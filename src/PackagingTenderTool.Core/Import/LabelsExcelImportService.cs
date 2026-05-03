using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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

    private static readonly Regex ColorsRangePattern = new(
        @"^\s*(\d+)\s*(?:to|-|/)\s*(\d+)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Decimal-like columns where parse failures are written to <see cref="ImportValidationIssue"/> during import.</summary>
    private static readonly HashSet<string> NumericImportValidationFieldNames = new(StringComparer.Ordinal)
    {
        nameof(LabelLineItem.Quantity),
        nameof(LabelLineItem.Spend),
        nameof(LabelLineItem.PricePerThousand),
        nameof(LabelLineItem.Price),
        nameof(LabelLineItem.TheoreticalSpend),
        nameof(LabelLineItem.TechnicalRating),
        nameof(LabelLineItem.LabelWeightGrams),
        nameof(LabelLineItem.NumberOfColors)
    };

    private static bool IsNumericImportValidationField(string? fieldName) =>
        fieldName is not null && NumericImportValidationFieldNames.Contains(fieldName);

    private static string ColumnDisplayName(string propertyName) =>
        propertyName switch
        {
            nameof(LabelLineItem.ItemNo) => "Item no.",
            nameof(LabelLineItem.ItemName) => "Item name",
            nameof(LabelLineItem.SupplierName) => "Supplier name",
            nameof(LabelLineItem.Site) => "Site",
            nameof(LabelLineItem.Quantity) => "Quantity",
            nameof(LabelLineItem.Spend) => "Spend",
            nameof(LabelLineItem.PricePerThousand) => "Price per 1,000",
            nameof(LabelLineItem.Price) => "Price",
            nameof(LabelLineItem.TheoreticalSpend) => "Theoretical spend",
            nameof(LabelLineItem.TechnicalRating) => "Technical rating",
            nameof(LabelLineItem.LabelSize) => "Label size",
            nameof(LabelLineItem.WindingDirection) => "Winding direction",
            nameof(LabelLineItem.Material) => "Material",
            nameof(LabelLineItem.ReelDiameterOrPcsPerRoll) => "Reel diameter / pcs per roll",
            nameof(LabelLineItem.NumberOfColors) => "No. of colors",
            nameof(LabelLineItem.LabelWeightGrams) => "Label weight (g)",
            nameof(LabelLineItem.IsMonoMaterial) => "Mono-material design",
            nameof(LabelLineItem.IsEasyToSeparate) => "Easy separation",
            nameof(LabelLineItem.IsReusableOrRecyclableMaterial) => "Reusable or recyclable material",
            nameof(LabelLineItem.HasTraceability) => "Traceability",
            nameof(LabelLineItem.Comment) => "Comment",
            _ => propertyName
        };

    private static string FormatRowColumnMessage(int rowNumber, string columnDisplay, string rawValue, string problemPhrase)
    {
        var raw = string.IsNullOrEmpty(rawValue) ? "(empty)" : $"'{rawValue}'";
        return $"Row {rowNumber}, {columnDisplay}: Value {raw} {problemPhrase}";
    }

    /// <summary>Keeps <see cref="LabelLineItem.SourceManualReviewFlags"/> for evaluation and mirrors the same finding on <see cref="ImportValidationIssue"/>.</summary>
    private static void RecordNumericParseFailure(
        int rowNumber,
        string fieldName,
        string? sourceValue,
        string reason,
        string? suggestedAction,
        LabelLineItem lineItem,
        ICollection<ImportValidationIssue> issues)
    {
        AddInvalidNumericFlag(
            lineItem.SourceManualReviewFlags,
            fieldName,
            sourceValue ?? string.Empty,
            reason,
            suggestedAction);
        issues.Add(new ImportValidationIssue
        {
            RowNumber = rowNumber,
            ColumnName = ColumnDisplayName(fieldName),
            RawValue = sourceValue,
            Message = reason,
            SuggestedAction = suggestedAction,
            IssueType = ImportValidationIssueType.InvalidCellValue,
            Severity = ImportValidationSeverity.Error,
            BlocksImport = false
        });
    }

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
            var issues = new List<ImportValidationIssue>();
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
                    issues.Add(new ImportValidationIssue
                    {
                        RowNumber = row.RowNumber(),
                        ColumnName = "—",
                        Message = "Skipped row because it did not look like a detailed tender item row.",
                        IssueType = ImportValidationIssueType.RowSkipped,
                        Severity = ImportValidationSeverity.Info
                    });
                    continue;
                }

                var lineItem = MapRow(row.RowNumber(), row, columnMap, categoryMapper, issues);
                rawRows.Add(rawRow);
                AddRowIssues(row.RowNumber(), lineItem, issues);
                if (columnMap.ContainsKey(nameof(LabelLineItem.Site))
                    && string.IsNullOrWhiteSpace(lineItem.Site))
                {
                    issues.Add(new ImportValidationIssue
                    {
                        RowNumber = row.RowNumber(),
                        ColumnName = ColumnDisplayName(nameof(LabelLineItem.Site)),
                        Message = $"Row {row.RowNumber()}, {ColumnDisplayName(nameof(LabelLineItem.Site))}: Site is blank.",
                        IssueType = ImportValidationIssueType.ManualReviewRequired,
                        Severity = ImportValidationSeverity.Warning,
                        SuggestedAction = "Enter site / plant (e.g. DSH site code) when available for spend breakdown."
                    });
                }

                lineItems.Add(lineItem);
            }

            var blocking = issues.Any(issue => issue.BlocksImport || issue.Severity == ImportValidationSeverity.Fatal);
            List<LabelLineItem> committedLineItems = lineItems;
            List<RawLabelTenderRow> committedRawRows = rawRows;
            List<CleanedLabelLineItem> cleanedRows;
            if (blocking)
            {
                committedLineItems = [];
                committedRawRows = [];
                cleanedRows = [];
            }
            else
            {
                cleanedRows = new LabelDataCleaningService().CleanMany(committedLineItems).ToList();
            }

            var invalidRows = committedLineItems.Count(lineItem => lineItem.SourceManualReviewFlags.Count > 0);
            var rowsWithSpend = committedLineItems.Where(lineItem => lineItem.Spend is > 0).ToList();

            var summary = new LabelsImportSummary
            {
                WorksheetName = worksheet.Name,
                HeaderRowNumber = headerRow.RowNumber(),
                TotalRowsScanned = scannedRows,
                ImportedRows = committedLineItems.Count,
                ValidRows = committedLineItems.Count - invalidRows,
                InvalidRows = invalidRows,
                SkippedRows = skippedRows,
                ManualReviewFlagCount = committedLineItems.Sum(lineItem => lineItem.SourceManualReviewFlags.Count),
                SupplierCount = DistinctCount(committedLineItems.Select(lineItem => lineItem.SupplierName)),
                SiteCount = DistinctCount(committedLineItems.Select(lineItem => lineItem.Site)),
                SizeCount = DistinctCount(cleanedRows.Select(row => row.NormalizedLabelSize)),
                MaterialCount = DistinctCount(cleanedRows.Select(row => row.NormalizedMaterial)),
                TotalSpend = rowsWithSpend.Sum(lineItem => lineItem.Spend!.Value)
            };

            var validationReport = ImportValidationReport.Create(
                worksheet.Name,
                headerRow.RowNumber(),
                scannedRows,
                committedLineItems.Count,
                issues,
                importCommitted: !blocking);

            return new LabelsTenderImportResult
            {
                Tender = new Tender { LabelLineItems = committedLineItems },
                RawRows = committedRawRows,
                CleanedRows = cleanedRows,
                Issues = issues,
                ValidationReport = validationReport,
                ImportCommitted = !blocking,
                Summary = summary
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

    private static void AddRowIssues(int rowNumber, LabelLineItem lineItem, ICollection<ImportValidationIssue> issues)
    {
        foreach (var flag in lineItem.SourceManualReviewFlags)
        {
            var field = flag.FieldName ?? "Source";
            if (IsNumericImportValidationField(field)
                && flag.Severity == ManualReviewSeverity.Error)
            {
                // Mirror row already added in RecordNumericParseFailure (ImportValidationReport).
                continue;
            }

            var issueType = flag.Reason.Contains("converted to", StringComparison.OrdinalIgnoreCase)
                            || flag.Reason.Contains("Please confirm", StringComparison.OrdinalIgnoreCase)
                ? ImportValidationIssueType.ManualReviewRequired
                : ImportValidationIssueType.InvalidCellValue;

            issues.Add(new ImportValidationIssue
            {
                RowNumber = rowNumber,
                ColumnName = ColumnDisplayName(field),
                RawValue = flag.SourceValue,
                Message = flag.Reason,
                SuggestedAction = flag.SuggestedAction,
                IssueType = issueType,
                Severity = flag.Severity == ManualReviewSeverity.Error
                    ? ImportValidationSeverity.Error
                    : ImportValidationSeverity.Warning,
                BlocksImport = false
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
            issues.Add(MissingOptionalSpend(rowNumber));
        }
    }

    private static ImportValidationIssue MissingRequiredValue(int rowNumber, string fieldName)
    {
        var col = ColumnDisplayName(fieldName);
        return new ImportValidationIssue
        {
            RowNumber = rowNumber,
            ColumnName = col,
            RawValue = null,
            Message = $"Row {rowNumber}, {col}: Required value is missing.",
            IssueType = ImportValidationIssueType.EmptyRequiredCell,
            Severity = ImportValidationSeverity.Error,
            BlocksImport = true,
            SuggestedAction = $"Enter {col} for this row before importing."
        };
    }

    private static ImportValidationIssue MissingOptionalSpend(int rowNumber)
    {
        var col = ColumnDisplayName(nameof(LabelLineItem.Spend));
        return new ImportValidationIssue
        {
            RowNumber = rowNumber,
            ColumnName = col,
            Message = $"Row {rowNumber}, {col}: Spend is empty — commercial totals may be incomplete.",
            IssueType = ImportValidationIssueType.ManualReviewRequired,
            Severity = ImportValidationSeverity.Warning,
            SuggestedAction = "Enter spend for this line when available."
        };
    }

    private static LabelLineItem MapRow(
        int excelRowNumber,
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        CategoryMapper categoryMapper,
        ICollection<ImportValidationIssue> issues)
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
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.Quantity),
            nameof(LabelLineItem.Quantity),
            lineItem,
            issues);
        lineItem.Spend = GetDecimal(
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.Spend),
            nameof(LabelLineItem.Spend),
            lineItem,
            issues);
        lineItem.PricePerThousand = GetDecimal(
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.PricePerThousand),
            nameof(LabelLineItem.PricePerThousand),
            lineItem,
            issues);
        lineItem.Price = GetDecimal(
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.Price),
            nameof(LabelLineItem.Price),
            lineItem,
            issues);
        lineItem.TheoreticalSpend = GetDecimal(
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.TheoreticalSpend),
            nameof(LabelLineItem.TheoreticalSpend),
            lineItem,
            issues);
        lineItem.TechnicalRating = GetDecimal(
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.TechnicalRating),
            nameof(LabelLineItem.TechnicalRating),
            lineItem,
            issues);
        lineItem.NumberOfColors = ParseNumberOfColors(
            excelRowNumber,
            row,
            columnMap,
            lineItem,
            issues);
        lineItem.LabelWeightGrams = GetDecimal(
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.LabelWeightGrams),
            nameof(LabelLineItem.LabelWeightGrams),
            lineItem,
            issues);
        lineItem.IsMonoMaterial = GetBoolean(
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.IsMonoMaterial),
            nameof(LabelLineItem.IsMonoMaterial),
            lineItem.SourceManualReviewFlags);
        lineItem.IsEasyToSeparate = GetBoolean(
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.IsEasyToSeparate),
            nameof(LabelLineItem.IsEasyToSeparate),
            lineItem.SourceManualReviewFlags);
        lineItem.IsReusableOrRecyclableMaterial = GetBoolean(
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.IsReusableOrRecyclableMaterial),
            nameof(LabelLineItem.IsReusableOrRecyclableMaterial),
            lineItem.SourceManualReviewFlags);
        lineItem.HasTraceability = GetBoolean(
            excelRowNumber,
            row,
            columnMap,
            nameof(LabelLineItem.HasTraceability),
            nameof(LabelLineItem.HasTraceability),
            lineItem.SourceManualReviewFlags);

        AddEprSchemeIfPossible(lineItem, categoryMapper);

        return lineItem;
    }

    private static int? ParseNumberOfColors(
        int excelRowNumber,
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        LabelLineItem lineItem,
        ICollection<ImportValidationIssue> issues)
    {
        const string fieldName = nameof(LabelLineItem.NumberOfColors);
        if (!columnMap.TryGetValue(fieldName, out var columnNumber))
        {
            return null;
        }

        var cell = row.Cell(columnNumber);
        if (cell.IsEmpty())
        {
            return null;
        }

        var col = ColumnDisplayName(fieldName);
        var sourceValue = cell.GetFormattedString().Trim();

        if (cell.DataType == XLDataType.Number
            && cell.TryGetValue<decimal>(out var numericValue))
        {
            if (numericValue % 1m == 0m)
            {
                return decimal.ToInt32(numericValue);
            }

            RecordNumericParseFailure(
                excelRowNumber,
                fieldName,
                sourceValue,
                FormatRowColumnMessage(excelRowNumber, col, sourceValue, "is not a valid whole number."),
                "Enter a whole number (no decimals) for this column.",
                lineItem,
                issues);
            return null;
        }

        var rangeMatch = ColorsRangePattern.Match(sourceValue);
        if (rangeMatch.Success)
        {
            var lo = int.Parse(rangeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var hi = int.Parse(rangeMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            var upper = Math.Max(lo, hi);
            lineItem.OriginalColorsValue = sourceValue;
            var msg =
                $"Row {excelRowNumber}, {col}: Value '{sourceValue}' was converted to {upper}. Please confirm.";
            lineItem.SourceManualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = fieldName,
                SourceValue = sourceValue,
                Reason = msg,
                Severity = ManualReviewSeverity.Warning,
                SuggestedAction = "Confirm the effective colour count in manual review before relying on scores."
            });
            return upper;
        }

        if (!FlexibleNumberParser.TryParseFlexibleDecimal(sourceValue, out var d))
        {
            RecordNumericParseFailure(
                excelRowNumber,
                fieldName,
                sourceValue,
                FormatRowColumnMessage(excelRowNumber, col, sourceValue, "is not a valid whole number."),
                "Enter a single whole number (e.g. 4), or a compact range like 5-6.",
                lineItem,
                issues);
            return null;
        }

        if (d % 1m != 0m)
        {
            RecordNumericParseFailure(
                excelRowNumber,
                fieldName,
                sourceValue,
                FormatRowColumnMessage(excelRowNumber, col, sourceValue, "is not a valid whole number."),
                "Enter a whole number without decimals.",
                lineItem,
                issues);
            return null;
        }

        return decimal.ToInt32(d);
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
        int excelRowNumber,
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        string propertyName,
        string fieldName,
        LabelLineItem lineItem,
        ICollection<ImportValidationIssue> issues)
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

        if (cell.DataType == XLDataType.Number
            && cell.TryGetValue<decimal>(out var numericValue))
        {
            return numericValue;
        }

        var sourceValue = cell.GetFormattedString().Trim();
        if (FlexibleNumberParser.TryParseFlexibleDecimal(sourceValue, out var parsedValue))
        {
            return parsedValue;
        }

        var colName = ColumnDisplayName(fieldName);
        RecordNumericParseFailure(
            excelRowNumber,
            fieldName,
            sourceValue,
            FormatRowColumnMessage(excelRowNumber, colName, sourceValue, "is not a valid number."),
            "Replace the cell with a numeric value using digits and standard grouping (e.g. 1.200,50 or 1,200.50).",
            lineItem,
            issues);

        return null;
    }

    private static bool? GetBoolean(
        int excelRowNumber,
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

        var col = ColumnDisplayName(fieldName);
        AddInvalidNumericFlag(
            manualReviewFlags,
            fieldName,
            sourceValue,
            FormatRowColumnMessage(excelRowNumber, col, sourceValue, "is not a valid yes/no value."),
            "Use true/false, yes/no, 1/0, or a boolean cell.");

        return null;
    }

    private static void AddInvalidNumericFlag(
        ICollection<ManualReviewFlag> manualReviewFlags,
        string fieldName,
        string sourceValue,
        string reason,
        string? suggestedAction = null)
    {
        manualReviewFlags.Add(new ManualReviewFlag
        {
            FieldName = fieldName,
            SourceValue = sourceValue,
            Reason = reason,
            Severity = ManualReviewSeverity.Error,
            SuggestedAction = suggestedAction
        });
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
