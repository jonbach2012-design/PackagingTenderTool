using ClosedXML.Excel;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Import;

/// <summary>
/// Imports the consolidated "Tender Price Analyze" Labels format.
/// One row per label format per site — expands to 4 LabelLineItem rows (one per supplier).
/// Detected by column header structure — sheet name is irrelevant.
/// See ADR-008 in docs/decisions/decisions.md.
/// </summary>
public sealed class TenderPriceAnalyzeImportService
{
    // Column indices (1-based, hardcoded per ADR-008)
    private const int ColSite = 1;
    private const int ColLabelFormat = 2;
    private const int ColMaterial = 3;
    private const int ColNumberOfColors = 4;
    private const int ColSurfaceFinish = 5;
    private const int ColLabelsPerRoll = 6;
    private const int ColHistoricalVolume = 7;
    private const int ColNumberOfDesigns = 8;
    private const int ColSuggestedVolume = 9;
    private const int ColStockArticle = 10;

    // Flexoprint (DKK price = CurrentContractPrice)
    private const int ColFlexoprintPriceDkk = 11;
    private const int ColFlexoprintSpendNok = 12;

    // Norsk Etikett (NOK)
    private const int ColNorskEtikettPriceNok = 13;
    private const int ColNorskEtikettMoq = 14;
    private const int ColNorskEtikettComment = 15;
    private const int ColNorskEtikettSpendNok = 16;

    // Grafiket (DKK)
    private const int ColGrafiketPriceDkk = 17;
    private const int ColGrafiketMoq = 18;
    private const int ColGrafiketComment = 19;
    private const int ColGrafiketSpendNok = 20;

    // Ettiketto (NOK)
    private const int ColEttikettoPriceNok = 21;
    private const int ColEttikettoMoq = 22;
    private const int ColEttikettoComment = 23;
    private const int ColEttikettoSpendNok = 24;

    private static readonly (string SupplierName, int PriceCol, string PriceCurrency,
        int SpendCol, int MoqCol, int CommentCol, bool IsCurrentSupplier)[] SupplierBlocks =
    [
        ("Flexoprint",    ColFlexoprintPriceDkk,    "DKK", ColFlexoprintSpendNok,    0,                    0,                      true),
        ("Norsk Etikett", ColNorskEtikettPriceNok,  "NOK", ColNorskEtikettSpendNok,  ColNorskEtikettMoq,   ColNorskEtikettComment, false),
        ("Grafiket",      ColGrafiketPriceDkk,      "DKK", ColGrafiketSpendNok,      ColGrafiketMoq,       ColGrafiketComment,     false),
        ("Ettiketto",     ColEttikettoPriceNok,     "NOK", ColEttikettoSpendNok,     ColEttikettoMoq,      ColEttikettoComment,    false),
    ];

    public const string FormatNotRecognizedMarker = "TENDER_PRICE_ANALYZE_NOT_RECOGNIZED";

    /// <summary>
    /// Detects whether the workbook matches the TenderPriceAnalyze format.
    /// Detection is based on column header structure — sheet name is irrelevant.
    /// </summary>
    public static bool IsTenderPriceAnalyzeFormat(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var position = stream.Position;
        try
        {
            stream.Position = 0;
            using var wb = new XLWorkbook(stream);
            foreach (var ws in wb.Worksheets)
            {
                var headerRow = ws.RowsUsed().FirstOrDefault();
                if (headerRow is null) continue;

                var headers = headerRow.CellsUsed()
                    .Select(c => c.GetString().Trim().ToLowerInvariant())
                    .ToHashSet();

                if (headers.Any(h => h.Contains("label format")) &&
                    headers.Any(h => h.Contains("flexoprint")))
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            stream.Position = position;
        }
    }

    public LabelsTenderImportResult ImportTenderWithReport(
        Stream stream,
        string tenderName,
        TenderSettings? settings = null,
        string? revisionSuffix = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        settings ??= new TenderSettings();
        var converter = CurrencyConverter.FromSettings(settings);
        var target = settings.TargetCurrency;

        XLWorkbook workbook;
        try
        {
            stream.Position = 0;
            workbook = new XLWorkbook(stream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(FormatNotRecognizedMarker, ex);
        }

        try
        {
            // Find the first worksheet with a recognizable header
            IXLWorksheet? worksheet = null;
            IXLRow? headerRow = null;

            foreach (var ws in workbook.Worksheets)
            {
                var candidate = ws.RowsUsed().FirstOrDefault();
                if (candidate is null) continue;

                var headers = candidate.CellsUsed()
                    .Select(c => c.GetString().Trim().ToLowerInvariant())
                    .ToList();

                if (headers.Any(h => h.Contains("label format")) &&
                    headers.Any(h => h.Contains("flexoprint")))
                {
                    worksheet = ws;
                    headerRow = candidate;
                    break;
                }
            }

            if (worksheet is null || headerRow is null)
                throw new InvalidOperationException(FormatNotRecognizedMarker);

            var offset = FindColumnOffset(headerRow);

            var lineItems = new List<LabelLineItem>();
            var issues = new List<ImportValidationIssue>();
            var scannedRows = 0;

            foreach (var row in worksheet.RowsUsed()
                .Where(r => r.RowNumber() > headerRow.RowNumber()))
            {
                var site = GetString(row, ColSite + offset);
                var labelFormat = GetString(row, ColLabelFormat + offset);

                // Skip empty rows, grand total rows, and analysis rows
                if (string.IsNullOrWhiteSpace(site) &&
                    string.IsNullOrWhiteSpace(labelFormat))
                    continue;

                if (labelFormat?.Contains("total", StringComparison.OrdinalIgnoreCase) == true ||
                    site?.Contains("total", StringComparison.OrdinalIgnoreCase) == true)
                    continue;

                // Skip rows that look like pivot/analysis sections
                // (no site AND no recognizable label format)
                if (string.IsNullOrWhiteSpace(labelFormat))
                    continue;

                scannedRows++;

                var material = GetString(row, ColMaterial + offset);
                var surfaceFinish = GetString(row, ColSurfaceFinish + offset);
                var labelsPerRoll = GetString(row, ColLabelsPerRoll + offset);
                var historicalVolume = GetDecimal(row, ColHistoricalVolume + offset);
                var numberOfDesigns = GetString(row, ColNumberOfDesigns + offset);
                var suggestedVolume = GetString(row, ColSuggestedVolume + offset);
                var stockArticle = GetString(row, ColStockArticle + offset);
                var numberOfColors = GetString(row, ColNumberOfColors + offset);

                // Surrogate key: "{Site}|{LabelFormat}"
                var surrogateKey = $"{site?.Trim()}|{labelFormat?.Trim()}";

                // Get Flexoprint DKK price for CurrentContractPrice
                var flexoprintDkkPrice = GetDecimal(row, ColFlexoprintPriceDkk + offset);
                var currentContractPrice = flexoprintDkkPrice.HasValue
                    ? converter.Convert(flexoprintDkkPrice.Value, "DKK", target)
                    : (decimal?)null;

                foreach (var block in SupplierBlocks)
                {
                    var rawPrice = GetDecimal(row, block.PriceCol + offset);
                    if (rawPrice is null or <= 0m) continue;

                    var priceInTarget = converter.Convert(rawPrice.Value, block.PriceCurrency, target);

                    var rawSpend = GetDecimal(row, block.SpendCol + offset);
                    var spendInTarget = rawSpend.HasValue
                        ? converter.Convert(rawSpend.Value, "NOK", target)
                        : (decimal?)null;

                    var moq = block.MoqCol > 0 ? GetString(row, block.MoqCol + offset) : null;
                    var comment = block.CommentCol > 0 ? GetString(row, block.CommentCol + offset) : null;

                    var commentParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(numberOfDesigns))
                        commentParts.Add($"Designs: {numberOfDesigns}");
                    if (!string.IsNullOrWhiteSpace(suggestedVolume))
                        commentParts.Add($"Suggested vol: {suggestedVolume}");
                    if (!string.IsNullOrWhiteSpace(stockArticle))
                        commentParts.Add($"Stock: {stockArticle}");
                    if (!string.IsNullOrWhiteSpace(moq))
                        commentParts.Add($"MOQ: {moq}");
                    if (!string.IsNullOrWhiteSpace(comment))
                        commentParts.Add(comment);

                    var supplierName = string.IsNullOrWhiteSpace(revisionSuffix)
                        ? block.SupplierName
                        : block.SupplierName + " " + revisionSuffix;

                    var lineItem = new LabelLineItem
                    {
                        ItemNo = surrogateKey,
                        ItemName = labelFormat,
                        SupplierName = supplierName,
                        Site = site,
                        LabelSize = labelFormat,
                        Material = material,
                        SurfaceFinish = surfaceFinish,
                        ReelDiameterOrPcsPerRoll = labelsPerRoll,
                        Quantity = historicalVolume,
                        PricePerThousand = priceInTarget,
                        Spend = spendInTarget,
                        CurrentContractPrice = block.IsCurrentSupplier ? currentContractPrice : null,
                        Comment = commentParts.Count > 0
                            ? string.Join(" | ", commentParts)
                            : null,
                    };

                    // Parse number of colors
                    if (!string.IsNullOrWhiteSpace(numberOfColors) &&
                        int.TryParse(numberOfColors.Split(' ')[0], out var colors))
                        lineItem.NumberOfColors = colors;

                    lineItems.Add(lineItem);
                }
            }

            var suppliers = lineItems
                .Where(i => !string.IsNullOrWhiteSpace(i.SupplierName))
                .Select(i => i.SupplierName!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var summary = new LabelsImportSummary
            {
                WorksheetName = worksheet.Name,
                HeaderRowNumber = headerRow.RowNumber(),
                TotalRowsScanned = scannedRows,
                ImportedRows = lineItems.Count,
                ValidRows = lineItems.Count,
                InvalidRows = 0,
                SkippedRows = 0,
                SupplierCount = suppliers,
                TotalSpend = lineItems.Where(i => i.Spend > 0).Sum(i => i.Spend!.Value)
            };

            var report = ImportValidationReport.Create(
                worksheet.Name,
                headerRow.RowNumber(),
                scannedRows,
                lineItems.Count,
                issues,
                importCommitted: true);

            return new LabelsTenderImportResult
            {
                Tender = new Tender { Name = tenderName, LabelLineItems = lineItems },
                Issues = issues,
                ValidationReport = report,
                ImportCommitted = true,
                Summary = summary,
                RawRows = [],
                CleanedRows = []
            };
        }
        finally
        {
            workbook.Dispose();
        }
    }

    /// <summary>
    /// Finds the column offset by locating "DSH Site" or "Label format" header.
    /// Returns 0 if data starts at column 1 (no offset needed).
    /// Returns 1 if data starts at column 2 (one empty column before data).
    /// </summary>
    private static int FindColumnOffset(IXLRow headerRow)
    {
        foreach (var cell in headerRow.CellsUsed())
        {
            var text = cell.GetString().Trim().ToLowerInvariant();
            if (text.Contains("dsh site") || text.Contains("label format"))
                return cell.Address.ColumnNumber - 1;
        }
        return 0;
    }

    private static string? GetString(IXLRow row, int col)
    {
        if (col <= 0) return null;
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;
        var value = cell.GetFormattedString().Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static decimal? GetDecimal(IXLRow row, int col)
    {
        if (col <= 0) return null;
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;

        if (cell.DataType == XLDataType.Number &&
            cell.TryGetValue<decimal>(out var numericValue))
            return numericValue;

        var text = cell.GetFormattedString().Trim();
        if (FlexibleNumberParser.TryParseFlexibleDecimal(text, out var parsed))
            return parsed;

        return null;
    }
}
