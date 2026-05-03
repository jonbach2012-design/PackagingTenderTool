using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Import;

public sealed class LabelsExcelImportService
{
    private static readonly string[] RequiredColumns =
    [
        nameof(LabelLineItem.ItemNo),
        nameof(LabelLineItem.SupplierName)
    ];

    private static readonly IReadOnlyDictionary<string, string[]> ColumnAliases =
        new Dictionary<string, string[]>
        {
            [nameof(LabelLineItem.ItemNo)] = ["Item no", "Item no.", "Item number"],
            [nameof(LabelLineItem.ItemName)] = ["Item name", "Item"],
            [nameof(LabelLineItem.SupplierName)] = ["Supplier name", "Supplier"],
            [nameof(LabelLineItem.Site)] = ["Site", "DSH Site", "Location", "Plant"],
            [nameof(LabelLineItem.Quantity)] = ["Quantity", "Qty"],
            [nameof(LabelLineItem.Spend)] =
            [
                "Spend", "Spend (NOK)", "Spend (DKK)", "Spend (SEK)", "Spend (EUR)",
                "Spend NOK", "Spend DKK", "Spend SEK", "Spend EUR"
            ],
            [nameof(LabelLineItem.PricePerThousand)] = ["Price per 1,000", "Price per 1000", "Price/1000"],
            [nameof(LabelLineItem.Price)] =
            [
                "Price", "Price (DKK)", "Price (NOK)", "Price (SEK)", "Price (EUR)",
                "Price DKK", "Price NOK", "Price SEK", "Price EUR"
            ],
            [nameof(LabelLineItem.TheoreticalSpend)] =
            [
                "Theoretical spend", "Theoretical spend (NOK)", "Theoretical spend (DKK)", "Theoretical spend (SEK)",
                "Theoretical spend (EUR)", "Theoretical spend NOK", "Theoretical spend DKK", "Theoretical spend SEK",
                "Theoretical spend EUR"
            ],
            [nameof(LabelLineItem.TechnicalRating)] = ["TechnicalRating", "Technical rating", "MaterialQuality", "Material quality"],
            [nameof(LabelLineItem.LabelSize)] = ["Label size"],
            [nameof(LabelLineItem.WindingDirection)] = ["Winding direction"],
            [nameof(LabelLineItem.Material)] = ["Material"],
            [nameof(LabelLineItem.ReelDiameterOrPcsPerRoll)] =
                ["Reel diameter / pcs per roll", "Reel diameter/pcs per roll", "Reel diameter", "Pcs per roll"],
            [nameof(LabelLineItem.NumberOfColors)] = ["No. of colors", "No of colors", "Number of colors"],
            [nameof(LabelLineItem.LabelWeightGrams)] = ["Label weight", "Label weight grams", "Label weight (g)"],
            [nameof(LabelLineItem.IsMonoMaterial)] = ["Mono-material design", "Mono material", "Is mono material"],
            [nameof(LabelLineItem.IsEasyToSeparate)] = ["Easy separation", "Easy to separate", "Is easy to separate"],
            [nameof(LabelLineItem.IsReusableOrRecyclableMaterial)] =
                ["Reusable or recyclable material direction", "Reusable or recyclable", "Recyclable material"],
            [nameof(LabelLineItem.HasTraceability)] = ["Traceability", "Has traceability"],
            [nameof(LabelLineItem.Comment)] = ["Comment", "Comments"]
        };

    public Tender ImportTender(
        string filePath,
        string tenderName = "Imported Labels Tender",
        TenderSettings? settings = null)
    {
        return ImportTenderWithReport(filePath, tenderName, settings).Tender;
    }

    public Tender ImportTender(
        Stream excelStream,
        string tenderName = "Imported Labels Tender",
        TenderSettings? settings = null)
    {
        return ImportTenderWithReport(excelStream, tenderName, settings).Tender;
    }

    public LabelsTenderImportResult ImportTenderWithReport(
        string filePath,
        string tenderName = "Imported Labels Tender",
        TenderSettings? settings = null)
    {
        LabelTenderExcelImportGuard.EnsureXlsxExtension(filePath);

        using var stream = File.OpenRead(filePath);

        return ImportTenderWithReport(stream, tenderName, settings);
    }

    public LabelsTenderImportResult ImportTenderWithReport(
        Stream excelStream,
        string tenderName = "Imported Labels Tender",
        TenderSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(excelStream);

        var importResult = ImportLineItemsWithReport(excelStream);
        var tender = new Tender
        {
            Name = tenderName,
            Settings = settings ?? new TenderSettings(),
            LabelLineItems = importResult.Tender.LabelLineItems
        };
        importResult.Tender = tender;

        return importResult;
    }

    public IReadOnlyList<LabelLineItem> ImportLineItems(Stream excelStream)
    {
        return ImportLineItemsWithReport(excelStream).Tender.LabelLineItems;
    }

    public const string WorkbookOpenFailedMarker = "WORKBOOK_OPEN_FAILED";
    public const string NoWorksheetMarker = "NO_WORKSHEET";
    public const string HeaderNotRecognizedMarker = "HEADER_NOT_RECOGNIZED";
    public const string MissingRequiredColumnMarker = "MISSING_REQUIRED_COLUMN";

    public LabelsTenderImportResult ImportLineItemsWithReport(Stream excelStream)
    {
        ArgumentNullException.ThrowIfNull(excelStream);

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(excelStream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(WorkbookOpenFailedMarker, ex);
        }

        try
        {
            if (!workbook.Worksheets.Any())
            {
                throw new InvalidOperationException(NoWorksheetMarker);
            }

            IXLWorksheet? worksheet = null;
            IXLRow? headerRow = null;
            foreach (var candidateSheet in workbook.Worksheets)
            {
                var candidateHeader = FindHeaderRow(candidateSheet);
                if (candidateHeader is not null)
                {
                    worksheet = candidateSheet;
                    headerRow = candidateHeader;
                    break;
                }
            }

            if (worksheet is null || headerRow is null)
            {
                throw new InvalidOperationException(HeaderNotRecognizedMarker);
            }

            var columnMap = BuildColumnMap(headerRow);
            ValidateRequiredColumns(columnMap);
            var lineItems = new List<LabelLineItem>();
            var rawRows = new List<RawLabelTenderRow>();
            var issues = new List<LabelsImportIssue>();
            var categoryMapper = new CategoryMapper();
            var scannedRows = 0;
            var skippedRows = 0;

            foreach (var row in worksheet.RowsUsed().Where(row => row.RowNumber() > headerRow.RowNumber()))
            {
                if (!RowHasAnyContent(row))
                {
                    continue;
                }

                scannedRows++;
                var rawRow = MapRawRow(row, columnMap);
                if (ShouldSkipRow(rawRow))
                {
                    skippedRows++;
                    issues.Add(new LabelsImportIssue
                    {
                        RowNumber = row.RowNumber(),
                        FieldName = "Row",
                        Message = "Skipped row because it did not look like a detailed tender item row.",
                        Severity = LabelsImportIssueSeverity.Info
                    });
                    continue;
                }

                var lineItem = MapRow(row, columnMap, categoryMapper);
                rawRows.Add(rawRow);
                AddRowIssues(row.RowNumber(), lineItem, issues);
                lineItems.Add(lineItem);
            }

            var cleanedRows = new PackagingTenderTool.Core.Services.LabelDataCleaningService().CleanMany(lineItems).ToList();
            var invalidRows = lineItems.Count(lineItem => lineItem.SourceManualReviewFlags.Count > 0);
            var rowsWithSpend = lineItems.Where(lineItem => lineItem.Spend is > 0).ToList();

            return new LabelsTenderImportResult
            {
                Tender = new Tender { LabelLineItems = lineItems },
                RawRows = rawRows,
                CleanedRows = cleanedRows,
                Issues = issues,
                Summary = new LabelsImportSummary
                {
                    WorksheetName = worksheet.Name,
                    HeaderRowNumber = headerRow.RowNumber(),
                    TotalRowsScanned = scannedRows,
                    ImportedRows = lineItems.Count,
                    ValidRows = lineItems.Count - invalidRows,
                    InvalidRows = invalidRows,
                    SkippedRows = skippedRows,
                    ManualReviewFlagCount = lineItems.Sum(lineItem => lineItem.SourceManualReviewFlags.Count),
                    SupplierCount = DistinctCount(lineItems.Select(lineItem => lineItem.SupplierName)),
                    SiteCount = DistinctCount(lineItems.Select(lineItem => lineItem.Site)),
                    SizeCount = DistinctCount(cleanedRows.Select(row => row.NormalizedLabelSize)),
                    MaterialCount = DistinctCount(cleanedRows.Select(row => row.NormalizedMaterial)),
                    TotalSpend = rowsWithSpend.Sum(lineItem => lineItem.Spend!.Value)
                }
            };
        }
        finally
        {
            workbook.Dispose();
        }
    }

    private static int DistinctCount(IEnumerable<string?> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    private static IReadOnlyDictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var aliasLookup = ColumnAliases
            .SelectMany(pair => pair.Value.Select(alias => new
            {
                PropertyName = pair.Key,
                NormalizedAlias = NormalizeColumnName(alias)
            }))
            .GroupBy(alias => alias.NormalizedAlias)
            .ToDictionary(group => group.Key, group => group.First().PropertyName);

        var columnMap = new Dictionary<string, int>();
        foreach (var cell in headerRow.CellsUsed())
        {
            var normalizedHeader = NormalizeColumnName(cell.GetString());
            if (aliasLookup.TryGetValue(normalizedHeader, out var propertyName)
                && !columnMap.ContainsKey(propertyName))
            {
                columnMap[propertyName] = cell.Address.ColumnNumber;
            }
        }

        return columnMap;
    }

    private static string DisplayRequiredColumnName(string propertyName) =>
        propertyName switch
        {
            nameof(LabelLineItem.ItemNo) => "Item no.",
            nameof(LabelLineItem.SupplierName) => "Supplier name",
            _ => propertyName
        };

    /// <summary>
    /// Whether a row looks like a Labels tender <em>header</em> (grid identity). Supplier name is validated
    /// separately via <see cref="ValidateRequiredColumns"/> so a sheet missing that column still matches here
    /// and then fails with a missing-column message, not <see cref="HeaderNotRecognizedMarker"/>.
    /// Uses the same <see cref="BuildColumnMap"/> / <see cref="ColumnAliases"/> as mapping.
    /// </summary>
    private static bool IsLabelsTenderIdentityHeader(IReadOnlyDictionary<string, int> columnMap)
    {
        if (!columnMap.ContainsKey(nameof(LabelLineItem.ItemNo))
            || !columnMap.ContainsKey(nameof(LabelLineItem.ItemName))
            || !columnMap.ContainsKey(nameof(LabelLineItem.Quantity))
            || !columnMap.ContainsKey(nameof(LabelLineItem.Material)))
        {
            return false;
        }

        var hasPriceLike = columnMap.ContainsKey(nameof(LabelLineItem.Price))
            || columnMap.ContainsKey(nameof(LabelLineItem.PricePerThousand))
            || columnMap.ContainsKey(nameof(LabelLineItem.Spend))
            || columnMap.ContainsKey(nameof(LabelLineItem.TheoreticalSpend));

        return hasPriceLike;
    }

    private static IXLRow? FindHeaderRow(IXLWorksheet worksheet)
    {
        return worksheet.RowsUsed()
            .Select(row => new
            {
                Row = row,
                ColumnMap = BuildColumnMap(row)
            })
            .OrderByDescending(candidate => candidate.ColumnMap.Count)
            .FirstOrDefault(candidate => IsLabelsTenderIdentityHeader(candidate.ColumnMap))?
            .Row;
    }

    private static void ValidateRequiredColumns(IReadOnlyDictionary<string, int> columnMap)
    {
        var missingColumns = RequiredColumns
            .Where(column => !columnMap.ContainsKey(column))
            .ToList();
        if (missingColumns.Count > 0)
        {
            var display = missingColumns.Select(static c => DisplayRequiredColumnName(c)).ToArray();
            throw new InvalidOperationException($"{MissingRequiredColumnMarker}:{string.Join('|', display)}");
        }
    }

    private static RawLabelTenderRow MapRawRow(IXLRow row, IReadOnlyDictionary<string, int> columnMap)
    {
        return new RawLabelTenderRow
        {
            RowNumber = row.RowNumber(),
            ItemNo = GetString(row, columnMap, nameof(LabelLineItem.ItemNo)),
            ItemName = GetString(row, columnMap, nameof(LabelLineItem.ItemName)),
            SupplierName = GetString(row, columnMap, nameof(LabelLineItem.SupplierName)),
            Site = GetString(row, columnMap, nameof(LabelLineItem.Site)),
            Quantity = GetRawString(row, columnMap, nameof(LabelLineItem.Quantity)),
            Spend = GetRawString(row, columnMap, nameof(LabelLineItem.Spend)),
            PricePerThousand = GetRawString(row, columnMap, nameof(LabelLineItem.PricePerThousand)),
            Price = GetRawString(row, columnMap, nameof(LabelLineItem.Price)),
            TheoreticalSpend = GetRawString(row, columnMap, nameof(LabelLineItem.TheoreticalSpend)),
            LabelSize = GetString(row, columnMap, nameof(LabelLineItem.LabelSize)),
            WindingDirection = GetString(row, columnMap, nameof(LabelLineItem.WindingDirection)),
            Material = GetString(row, columnMap, nameof(LabelLineItem.Material)),
            ReelDiameterOrPcsPerRoll = GetString(row, columnMap, nameof(LabelLineItem.ReelDiameterOrPcsPerRoll)),
            NumberOfColors = GetRawString(row, columnMap, nameof(LabelLineItem.NumberOfColors)),
            Comment = GetString(row, columnMap, nameof(LabelLineItem.Comment))
        };
    }

    private static bool ShouldSkipRow(RawLabelTenderRow rawRow)
    {
        var hasItemIdentity = !string.IsNullOrWhiteSpace(rawRow.ItemNo)
            || !string.IsNullOrWhiteSpace(rawRow.ItemName);
        if (rawRow.ItemNo?.Contains("summary", StringComparison.OrdinalIgnoreCase) == true
            || rawRow.ItemNo?.Contains("total", StringComparison.OrdinalIgnoreCase) == true
            || rawRow.ItemName?.Contains("summary", StringComparison.OrdinalIgnoreCase) == true
            || rawRow.ItemName?.Contains("total", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return !hasItemIdentity;
    }

    private static void AddRowIssues(int rowNumber, LabelLineItem lineItem, ICollection<LabelsImportIssue> issues)
    {
        foreach (var flag in lineItem.SourceManualReviewFlags)
        {
            issues.Add(new LabelsImportIssue
            {
                RowNumber = rowNumber,
                FieldName = flag.FieldName ?? "Source",
                Message = flag.Reason,
                SourceValue = flag.SourceValue,
                Severity = flag.Severity == ManualReviewSeverity.Error
                    ? LabelsImportIssueSeverity.Error
                    : LabelsImportIssueSeverity.Warning
            });
        }

        if (string.IsNullOrWhiteSpace(lineItem.ItemNo))
        {
            issues.Add(MissingRequiredValue(rowNumber, nameof(LabelLineItem.ItemNo)));
        }

        if (string.IsNullOrWhiteSpace(lineItem.SupplierName))
        {
            issues.Add(MissingRequiredValue(rowNumber, nameof(LabelLineItem.SupplierName)));
        }

        if (lineItem.Spend is null)
        {
            issues.Add(MissingRequiredValue(rowNumber, nameof(LabelLineItem.Spend)));
        }
    }

    private static LabelsImportIssue MissingRequiredValue(int rowNumber, string fieldName)
    {
        return new LabelsImportIssue
        {
            RowNumber = rowNumber,
            FieldName = fieldName,
            Message = "Required value is missing from a detailed tender row.",
            Severity = LabelsImportIssueSeverity.Warning
        };
    }

    private static LabelLineItem MapRow(
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        CategoryMapper categoryMapper)
    {
        var lineItem = new LabelLineItem
        {
            ItemNo = GetString(row, columnMap, nameof(LabelLineItem.ItemNo)),
            ItemName = GetString(row, columnMap, nameof(LabelLineItem.ItemName)),
            SupplierName = GetString(row, columnMap, nameof(LabelLineItem.SupplierName)),
            Site = GetString(row, columnMap, nameof(LabelLineItem.Site)),
            LabelSize = GetString(row, columnMap, nameof(LabelLineItem.LabelSize)),
            WindingDirection = GetString(row, columnMap, nameof(LabelLineItem.WindingDirection)),
            Material = GetString(row, columnMap, nameof(LabelLineItem.Material)),
            ReelDiameterOrPcsPerRoll = GetString(row, columnMap, nameof(LabelLineItem.ReelDiameterOrPcsPerRoll)),
            Comment = GetString(row, columnMap, nameof(LabelLineItem.Comment))
        };

        lineItem.Quantity = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.Quantity),
            nameof(LabelLineItem.Quantity),
            lineItem.SourceManualReviewFlags);
        lineItem.Spend = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.Spend),
            nameof(LabelLineItem.Spend),
            lineItem.SourceManualReviewFlags);
        lineItem.PricePerThousand = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.PricePerThousand),
            nameof(LabelLineItem.PricePerThousand),
            lineItem.SourceManualReviewFlags);
        lineItem.Price = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.Price),
            nameof(LabelLineItem.Price),
            lineItem.SourceManualReviewFlags);
        lineItem.TheoreticalSpend = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.TheoreticalSpend),
            nameof(LabelLineItem.TheoreticalSpend),
            lineItem.SourceManualReviewFlags);
        lineItem.TechnicalRating = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.TechnicalRating),
            nameof(LabelLineItem.TechnicalRating),
            lineItem.SourceManualReviewFlags);
        lineItem.NumberOfColors = GetInteger(
            row,
            columnMap,
            nameof(LabelLineItem.NumberOfColors),
            nameof(LabelLineItem.NumberOfColors),
            lineItem.SourceManualReviewFlags);
        lineItem.LabelWeightGrams = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.LabelWeightGrams),
            nameof(LabelLineItem.LabelWeightGrams),
            lineItem.SourceManualReviewFlags);
        lineItem.IsMonoMaterial = GetBoolean(
            row,
            columnMap,
            nameof(LabelLineItem.IsMonoMaterial),
            nameof(LabelLineItem.IsMonoMaterial),
            lineItem.SourceManualReviewFlags);
        lineItem.IsEasyToSeparate = GetBoolean(
            row,
            columnMap,
            nameof(LabelLineItem.IsEasyToSeparate),
            nameof(LabelLineItem.IsEasyToSeparate),
            lineItem.SourceManualReviewFlags);
        lineItem.IsReusableOrRecyclableMaterial = GetBoolean(
            row,
            columnMap,
            nameof(LabelLineItem.IsReusableOrRecyclableMaterial),
            nameof(LabelLineItem.IsReusableOrRecyclableMaterial),
            lineItem.SourceManualReviewFlags);
        lineItem.HasTraceability = GetBoolean(
            row,
            columnMap,
            nameof(LabelLineItem.HasTraceability),
            nameof(LabelLineItem.HasTraceability),
            lineItem.SourceManualReviewFlags);

        AddEprSchemeIfPossible(lineItem, categoryMapper);

        return lineItem;
    }

    private static void AddEprSchemeIfPossible(LabelLineItem lineItem, CategoryMapper categoryMapper)
    {
        if (lineItem.EprSchemes.Count > 0)
        {
            return;
        }

        var countryCode = DeriveCountryCodeFromSite(lineItem.Site);
        var category = categoryMapper.MapToSystemCategory(lineItem.Material);
        if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        lineItem.EprSchemes.Add(new EprSchemeInfo
        {
            CountryCode = countryCode,
            Category = category
        });
    }

    private static string? DeriveCountryCodeFromSite(string? site)
    {
        if (string.IsNullOrWhiteSpace(site) || site.Length < 2)
        {
            return null;
        }

        var prefix = site.Trim().Substring(0, 2);
        return prefix.All(char.IsLetter) ? prefix.ToUpperInvariant() : null;
    }

    private static string? GetString(
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        string propertyName)
    {
        if (!columnMap.TryGetValue(propertyName, out var columnNumber))
        {
            return null;
        }

        var value = row.Cell(columnNumber).GetFormattedString().Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? GetRawString(
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        string propertyName)
    {
        if (!columnMap.TryGetValue(propertyName, out var columnNumber))
        {
            return null;
        }

        var value = row.Cell(columnNumber).GetFormattedString().Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static decimal? GetDecimal(
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        string propertyName,
        string fieldName,
        ICollection<ManualReviewFlag> manualReviewFlags)
    {
        if (!columnMap.TryGetValue(propertyName, out var columnNumber))
        {
            return null;
        }

        var cell = row.Cell(columnNumber);
        if (cell.IsEmpty())
        {
            return null;
        }

        var sourceValue = cell.GetFormattedString().Trim();
        if (TryParseDecimal(sourceValue, out var parsedValue))
        {
            return parsedValue;
        }

        if (cell.DataType == XLDataType.Number
            && cell.TryGetValue<decimal>(out var numericValue))
        {
            return numericValue;
        }

        AddInvalidNumericFlag(manualReviewFlags, fieldName, sourceValue, "Imported numeric value could not be parsed.");

        return null;
    }

    private static bool? GetBoolean(
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        string propertyName,
        string fieldName,
        ICollection<ManualReviewFlag> manualReviewFlags)
    {
        if (!columnMap.TryGetValue(propertyName, out var columnNumber))
        {
            return null;
        }

        var cell = row.Cell(columnNumber);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.Boolean
            && cell.TryGetValue<bool>(out var booleanValue))
        {
            return booleanValue;
        }

        var sourceValue = cell.GetFormattedString().Trim();
        if (TryParseBoolean(sourceValue, out var parsedValue))
        {
            return parsedValue;
        }

        AddInvalidNumericFlag(manualReviewFlags, fieldName, sourceValue, "Imported boolean value could not be parsed.");

        return null;
    }

    private static int? GetInteger(
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        string propertyName,
        string fieldName,
        ICollection<ManualReviewFlag> manualReviewFlags)
    {
        var decimalValue = GetDecimal(row, columnMap, propertyName, fieldName, manualReviewFlags);
        if (decimalValue is null)
        {
            return null;
        }

        if (decimalValue.Value % 1m == 0m)
        {
            return decimal.ToInt32(decimalValue.Value);
        }

        AddInvalidNumericFlag(
            manualReviewFlags,
            fieldName,
            decimalValue.Value.ToString("G", CultureInfo.InvariantCulture),
            "Imported integer value had a decimal component.");

        return null;
    }

    private static void AddInvalidNumericFlag(
        ICollection<ManualReviewFlag> manualReviewFlags,
        string fieldName,
        string sourceValue,
        string reason)
    {
        manualReviewFlags.Add(new ManualReviewFlag
        {
            FieldName = fieldName,
            SourceValue = sourceValue,
            Reason = reason,
            Severity = ManualReviewSeverity.Error
        });
    }

    private static bool TryParseDecimal(string sourceValue, out decimal value)
    {
        var normalizedValue = NormalizeDecimalValue(sourceValue);
        if (normalizedValue is not null
            && decimal.TryParse(
                normalizedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value))
        {
            return true;
        }

        return decimal.TryParse(
                sourceValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value)
            || decimal.TryParse(
                sourceValue,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out value);
    }

    private static bool TryParseBoolean(string sourceValue, out bool value)
    {
        switch (sourceValue.Trim().ToLowerInvariant())
        {
            case "true":
            case "yes":
            case "y":
            case "1":
            case "ja":
            case "j":
                value = true;
                return true;
            case "false":
            case "no":
            case "n":
            case "0":
            case "nej":
                value = false;
                return true;
            default:
                value = false;
                return false;
        }
    }


    private static string? NormalizeDecimalValue(string sourceValue)
    {
        var value = sourceValue
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);
        value = new string(value
            .Where(character => char.IsDigit(character)
                || character == ','
                || character == '.'
                || character == '-')
            .ToArray());
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var lastCommaIndex = value.LastIndexOf(',');
        var lastDotIndex = value.LastIndexOf('.');

        if (lastCommaIndex >= 0 && lastDotIndex >= 0)
        {
            return lastCommaIndex > lastDotIndex
                ? value.Replace(".", string.Empty).Replace(',', '.')
                : value.Replace(",", string.Empty);
        }

        if (lastCommaIndex >= 0)
        {
            if (value.Count(character => character == ',') > 1
                || value.Length - lastCommaIndex - 1 == 3)
            {
                return value.Replace(",", string.Empty);
            }

            return value.Replace(',', '.');
        }

        if (lastDotIndex >= 0
            && (value.Count(character => character == '.') > 1
                || value.Length - lastDotIndex - 1 == 3))
        {
            return value.Replace(".", string.Empty);
        }

        return value;
    }

    private static bool RowHasAnyContent(IXLRow row)
    {
        return row.CellsUsed().Any(cell => !string.IsNullOrWhiteSpace(cell.GetFormattedString()));
    }

    private static string NormalizeColumnName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}
