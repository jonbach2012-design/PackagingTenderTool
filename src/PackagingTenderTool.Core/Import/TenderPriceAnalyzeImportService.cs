using System.Text.RegularExpressions;
using ClosedXML.Excel;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Import;

/// <summary>
/// Imports the consolidated "Tender Price Analyze" Labels format.
/// One row per label format per site — expands to one LabelLineItem per detected supplier price column.
/// Detected by column header structure — sheet name is irrelevant.
/// See ADR-008 in docs/decisions/decisions.md.
/// </summary>
public sealed class TenderPriceAnalyzeImportService
{
    // Column indices (1-based anchor columns; offset applied at read time for cols 1–12)
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

    private sealed record SupplierBlock(
        string SupplierName,
        int PriceCol,
        string PriceCurrency,
        int SpendCol,
        int MoqCol,
        int CommentCol,
        bool IsCurrentSupplier);

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
            var supplierBlocks = DetectSupplierBlocks(headerRow, offset);

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

                foreach (var block in supplierBlocks)
                {
                    var rawPrice = GetDecimal(row, block.PriceCol);
                    if (rawPrice is null or <= 0m) continue;

                    var priceInTarget = converter.Convert(rawPrice.Value, block.PriceCurrency, target);

                    decimal? spendInTarget;
                    if (block.IsCurrentSupplier)
                    {
                        // Flexoprint: compute spend from tender price × historical volume
                        // for fair comparison with other suppliers' tender bids
                        spendInTarget = historicalVolume.HasValue && historicalVolume.Value > 0
                            ? priceInTarget * historicalVolume.Value / 1000m
                            : (decimal?)null;
                    }
                    else
                    {
                        var rawSpend = block.SpendCol > 0 ? GetDecimal(row, block.SpendCol) : null;
                        spendInTarget = rawSpend.HasValue
                            ? converter.Convert(rawSpend.Value, "NOK", target)
                            : (decimal?)null;
                    }

                    var moq = block.MoqCol > 0 ? GetString(row, block.MoqCol) : null;
                    var comment = block.CommentCol > 0 ? GetString(row, block.CommentCol) : null;

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
    /// Detects supplier price blocks dynamically from header row.
    /// Supports arbitrary number of suppliers and revision columns (e.g. "Grafiket price rev 2").
    /// Flexoprint anchor columns (11, 12) are always hardcoded — other suppliers are header-detected.
    /// Column numbers stored are absolute Excel column indices (offset already applied for Flexoprint).
    /// </summary>
    private static List<SupplierBlock> DetectSupplierBlocks(IXLRow headerRow, int offset)
    {
        var blocks = new List<SupplierBlock>();

        blocks.Add(new SupplierBlock(
            "Flexoprint", ColFlexoprintPriceDkk + offset, "DKK",
            ColFlexoprintSpendNok + offset, 0, 0, true));

        var headers = new Dictionary<int, string>();
        foreach (var cell in headerRow.CellsUsed())
        {
            var text = cell.GetString().Trim();
            if (!string.IsNullOrWhiteSpace(text))
                headers[cell.Address.ColumnNumber] = text;
        }

        var spendCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var moqCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var commentCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (col, h) in headers)
        {
            var hl = h.ToLowerInvariant();
            if (hl.StartsWith("spend") || hl.Contains("current spend"))
            {
                var sup = ExtractSupplierFromHeader(h);
                if (sup is not null) spendCols[sup] = col;
            }
            else if (hl.Contains("moq"))
            {
                var sup = ExtractSupplierFromHeader(h);
                if (sup is not null) moqCols[sup] = col;
            }
            else if (h.StartsWith("comment", StringComparison.OrdinalIgnoreCase))
            {
                var sup = ExtractSupplierFromHeader(h);
                if (sup is not null) commentCols[sup] = col;
            }
        }

        foreach (var (col, h) in headers.OrderBy(x => x.Key))
        {
            if (col <= ColFlexoprintSpendNok + offset) continue;
            var hl = h.ToLowerInvariant();
            if (!hl.Contains("price")) continue;

            var currency = hl.Contains("(dkk)") ? "DKK" : "NOK";

            var supplierName = ExtractSupplierNameFromPriceHeader(h);
            if (supplierName is null) continue;

            var baseName = Regex
                .Replace(supplierName, @"\s+Rev\d+$", "", RegexOptions.IgnoreCase)
                .Trim();

            spendCols.TryGetValue(baseName, out var spendCol);
            moqCols.TryGetValue(baseName, out var moqCol);
            commentCols.TryGetValue(baseName, out var commentCol);

            blocks.Add(new SupplierBlock(
                supplierName, col, currency,
                spendCol, moqCol, commentCol, false));
        }

        return blocks;
    }

    /// <summary>
    /// Extracts supplier name from a price header.
    /// "Grafiket price 1 (DKK)" → "Grafiket"
    /// "Grafiket price rev 2 (DKK)" → "Grafiket Rev2"
    /// "Norsk Etikett price 1 (NOK)" → "Norsk Etikett"
    /// </summary>
    private static string? ExtractSupplierNameFromPriceHeader(string header)
    {
        var h = header.Trim();
        h = Regex.Replace(
            h, @"\s*\(DKK\)|\s*\(NOK\)", "",
            RegexOptions.IgnoreCase).Trim();

        var revMatch = Regex.Match(
            h, @"^(.+?)\s*,?\s*price\s+rev\s*(\d+)",
            RegexOptions.IgnoreCase);
        if (revMatch.Success)
            return $"{revMatch.Groups[1].Value.Trim()} Rev{revMatch.Groups[2].Value}";

        var normalMatch = Regex.Match(
            h, @"^(.+?)\s*,?\s*(?:Q\d+\s+\d{4}\s+)?price\b",
            RegexOptions.IgnoreCase);
        if (normalMatch.Success)
            return normalMatch.Groups[1].Value.Trim();

        return null;
    }

    /// <summary>
    /// Extracts base supplier name from spend/MOQ/comment headers.
    /// "Spend (NOK), Grafiket" → "Grafiket"
    /// "MOQ Norsk Etikett" → "Norsk Etikett"
    /// "Comment, Ettiketto" → "Ettiketto"
    /// "Flexoprint, current spend (NOK)" → "Flexoprint"
    /// </summary>
    private static string? ExtractSupplierFromHeader(string header)
    {
        var h = header.Trim();
        var spendMatch = Regex.Match(
            h, @"^spend\s*(?:\([^)]+\))?\s*,\s*(.+)$",
            RegexOptions.IgnoreCase);
        if (spendMatch.Success) return spendMatch.Groups[1].Value.Trim();

        var currentMatch = Regex.Match(
            h, @"^(.+?)\s*,\s*current\s+spend",
            RegexOptions.IgnoreCase);
        if (currentMatch.Success) return currentMatch.Groups[1].Value.Trim();

        var moqMatch = Regex.Match(
            h, @"^MOQ\s+(.+)$",
            RegexOptions.IgnoreCase);
        if (moqMatch.Success) return moqMatch.Groups[1].Value.Trim();

        var commentMatch = Regex.Match(
            h, @"^comment\s*,\s*(.+)$",
            RegexOptions.IgnoreCase);
        if (commentMatch.Success) return commentMatch.Groups[1].Value.Trim();

        return null;
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
