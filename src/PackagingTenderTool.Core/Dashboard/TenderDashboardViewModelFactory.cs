using System.Globalization;
using PackagingTenderTool.Core.Analytics;
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Dashboard;

public sealed class TenderDashboardViewModelFactory
{
    private static int DashboardSeverityRank(string severity) =>
        severity switch
        {
            nameof(ImportValidationSeverity.Fatal) => 4,
            nameof(ImportValidationSeverity.Error) => 3,
            nameof(ImportValidationSeverity.Warning) => 2,
            nameof(ImportValidationSeverity.Info) => 1,
            _ => 0
        };

    public TenderDashboardViewModel Create(TenderEvaluationResult result)
    {
        return Create(result, new TenderDashboardQuery());
    }

    public TenderDashboardViewModel Create(TenderEvaluationResult result, TenderDashboardQuery? query)
    {
        ArgumentNullException.ThrowIfNull(result);

        query ??= new TenderDashboardQuery();
        var analytics = result.Analytics;
        var importSummary = result.ImportSummary;
        var outlierKeys = BuildOutlierKeys(analytics?.PriceOutlierCandidates ?? []);
        var cleanedRows = ApplyQuery(result.CleanedLineItems, query, outlierKeys).ToList();
        var filteredAnalytics = new TenderAnalyticsService().Analyze(cleanedRows);
        var countries = BuildOptions(result.CleanedLineItems, row => row.Country);

        return new TenderDashboardViewModel
        {
            Query = query,
            ImportSummary = new DashboardImportSummary
            {
                WorksheetName = importSummary?.WorksheetName ?? string.Empty,
                HeaderRowNumber = importSummary?.HeaderRowNumber ?? 0,
                RowsImported = importSummary?.ImportedRows ?? result.CleanedLineItems.Count,
                ValidRows = importSummary?.ValidRows ?? result.CleanedLineItems.Count(row => row.HasRequiredBusinessData),
                InvalidRows = importSummary?.InvalidRows ?? result.CleanedLineItems.Count(row => !row.HasRequiredBusinessData || row.Source.SourceManualReviewFlags.Count > 0),
                SkippedRows = importSummary?.SkippedRows ?? 0,
                SupplierCount = importSummary?.SupplierCount ?? DistinctCount(result.CleanedLineItems.Select(row => row.Source.SupplierName)),
                CountryCount = countries.Count,
                SiteCount = importSummary?.SiteCount ?? DistinctCount(result.CleanedLineItems.Select(row => row.Source.Site)),
                SizeCount = importSummary?.SizeCount ?? DistinctCount(result.CleanedLineItems.Select(row => row.NormalizedLabelSize)),
                MaterialCount = importSummary?.MaterialCount ?? DistinctCount(result.CleanedLineItems.Select(row => row.NormalizedMaterial)),
                ManualReviewFlagCount = importSummary?.ManualReviewFlagCount ?? result.CleanedLineItems.Sum(row => row.Source.SourceManualReviewFlags.Count),
                TotalSpend = importSummary?.TotalSpend ?? analytics?.TotalSpend ?? filteredAnalytics.TotalSpend
            },
            ImportMetrics =
            [
                Metric("Rows imported", importSummary?.ImportedRows),
                Metric("Valid rows", importSummary?.ValidRows),
                Metric("Invalid rows", importSummary?.InvalidRows),
                Metric("Skipped rows", importSummary?.SkippedRows),
                Metric("Suppliers", result.SupplierEvaluations.Count),
                Metric("Visible rows", cleanedRows.Count)
            ],
            AnalyticsMetrics =
            [
                Metric("Countries", filteredAnalytics.SpendByCountry.Count),
                Metric("Sites", filteredAnalytics.SpendBySite.Count),
                Metric("Label sizes", filteredAnalytics.SpendByLabelSize.Count),
                Metric("Materials", filteredAnalytics.SpendByMaterial.Count),
                Metric("Outliers", analytics?.PriceOutlierCandidates.Count),
                Metric("Consolidation candidates", analytics?.ConsolidationCandidates.Count),
                Metric("Visible spend", filteredAnalytics.TotalSpend, "N0"),
                Metric("Total spend", analytics?.TotalSpend, "N0")
            ],
            SupplierOverview = result.SupplierEvaluations
                .Select(supplier => new DashboardSupplierOverviewRow
                {
                    SupplierName = supplier.SupplierName,
                    TotalSpend = supplier.TotalSpend,
                    CommercialScore = supplier.ScoreBreakdown.Commercial,
                    TechnicalScore = supplier.ScoreBreakdown.Technical,
                    RegulatoryScore = supplier.ScoreBreakdown.Regulatory,
                    TotalScore = supplier.ScoreBreakdown.Total,
                    Classification = supplier.Classification?.ToString() ?? "Unclassified",
                    RequiresManualReview = supplier.RequiresManualReview,
                    ManualReviewFlagCount = supplier.ManualReviewFlags.Count
                })
                .OrderByDescending(row => row.TotalScore ?? -1)
                .ToList(),
            Suppliers = BuildOptions(result.CleanedLineItems, row => row.Source.SupplierName),
            Countries = countries,
            Sites = BuildOptions(result.CleanedLineItems, row => row.Source.Site),
            Materials = BuildOptions(result.CleanedLineItems, row => row.NormalizedMaterial),
            LabelSizes = BuildOptions(result.CleanedLineItems, row => row.NormalizedLabelSize),
            SpendByCountry = filteredAnalytics.SpendByCountry
                .Select(item => new DashboardSpendBreakdownRow
                {
                    Name = item.Name,
                    Spend = item.Spend,
                    ShareOfTotal = item.ShareOfTotal,
                    ItemCount = item.ItemCount
                })
                .ToList(),
            SpendBySite = filteredAnalytics.SpendBySite
                .Select(item => new DashboardSpendBreakdownRow
                {
                    Name = item.Name,
                    Spend = item.Spend,
                    ShareOfTotal = item.ShareOfTotal,
                    ItemCount = item.ItemCount
                })
                .ToList(),
            SpendByMaterial = filteredAnalytics.SpendByMaterial
                .Select(item => new DashboardSpendBreakdownRow
                {
                    Name = item.Name,
                    Spend = item.Spend,
                    ShareOfTotal = item.ShareOfTotal,
                    ItemCount = item.ItemCount
                })
                .ToList(),
            SpendByLabelSize = filteredAnalytics.SpendByLabelSize
                .Select(item => new DashboardSpendBreakdownRow
                {
                    Name = item.Name,
                    Spend = item.Spend,
                    ShareOfTotal = item.ShareOfTotal,
                    ItemCount = item.ItemCount
                })
                .ToList(),
            ItemRows = cleanedRows
                .OrderByDescending(row => row.Source.Spend ?? 0m)
                .Take(Math.Max(1, query.MaxRows))
                .Select(row => new DashboardTenderItemRow
                {
                    ItemNo = row.Source.ItemNo,
                    ItemName = row.Source.ItemName,
                    SupplierName = row.Source.SupplierName,
                    Country = row.Country,
                    Site = row.Source.Site,
                    LabelSize = row.NormalizedLabelSize,
                    Material = row.NormalizedMaterial,
                    ColorGroup = row.NormalizedColorGroup,
                    WindingDirection = row.NormalizedWindingDirection,
                    Quantity = row.Source.Quantity,
                    Spend = row.Source.Spend,
                    PricePerThousand = row.Source.PricePerThousand,
                    HasRequiredBusinessData = row.HasRequiredBusinessData,
                    HasFlags = row.Source.SourceManualReviewFlags.Count > 0,
                    IsOutlierCandidate = IsOutlier(row, outlierKeys)
                })
                .ToList(),
            TopSpendItems = filteredAnalytics.TopSpendItems
                .Select(item => new DashboardTopSpendRow
                {
                    ItemNo = item.ItemNo,
                    ItemName = item.ItemName,
                    SupplierName = item.SupplierName,
                    Site = item.Site,
                    LabelSize = item.LabelSize,
                    Spend = item.Spend
                })
                .ToList(),
            Outliers = analytics?.PriceOutlierCandidates
                .Select(item => new DashboardOutlierRow
                {
                    ItemNo = item.ItemNo,
                    ItemName = item.ItemName,
                    LabelSize = item.LabelSize,
                    Material = item.Material,
                    PricePerThousand = item.PricePerThousand,
                    MedianPricePerThousand = item.GroupMedianPricePerThousand,
                    PercentAboveMedian = item.PercentAboveMedian
                })
                .ToList() ?? [],
            ConsolidationCandidates = analytics?.ConsolidationCandidates
                .Select(item => new DashboardConsolidationRow
                {
                    LabelSize = item.LabelSize,
                    Material = item.Material,
                    Spend = item.Spend,
                    ItemCount = item.ItemCount,
                    SiteCount = item.SiteCount
                })
                .ToList() ?? [],
            Issues = result.ImportIssues
                .Select(issue => new DashboardIssueRow
                {
                    RowNumber = issue.RowNumber ?? 0,
                    FieldName = issue.ColumnName ?? string.Empty,
                    Message = issue.Message,
                    SourceValue = issue.RawValue,
                    Severity = issue.Severity.ToString()
                })
                .OrderByDescending(issue => DashboardSeverityRank(issue.Severity))
                .ThenBy(issue => issue.RowNumber)
                .ToList()
        };
    }

    private static IEnumerable<CleanedLabelLineItem> ApplyQuery(
        IEnumerable<CleanedLabelLineItem> rows,
        TenderDashboardQuery query,
        ISet<string> outlierKeys)
    {
        var filtered = rows;
        if (!string.IsNullOrWhiteSpace(query.Supplier))
        {
            filtered = filtered.Where(row => Matches(row.Source.SupplierName, query.Supplier));
        }

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            filtered = filtered.Where(row => Matches(row.Country, query.Country));
        }

        if (!string.IsNullOrWhiteSpace(query.Site))
        {
            filtered = filtered.Where(row => Matches(row.Source.Site, query.Site));
        }

        if (!string.IsNullOrWhiteSpace(query.Material))
        {
            filtered = filtered.Where(row => Matches(row.NormalizedMaterial, query.Material));
        }

        if (!string.IsNullOrWhiteSpace(query.LabelSize))
        {
            filtered = filtered.Where(row => Matches(row.NormalizedLabelSize, query.LabelSize));
        }

        if (query.MinimumSpend is > 0m)
        {
            filtered = filtered.Where(row => row.Source.Spend >= query.MinimumSpend);
        }

        if (query.FlaggedOnly)
        {
            filtered = filtered.Where(row => row.Source.SourceManualReviewFlags.Count > 0 || !row.HasRequiredBusinessData);
        }

        if (query.OutliersOnly)
        {
            filtered = filtered.Where(row => IsOutlier(row, outlierKeys));
        }

        return filtered;
    }

    private static List<DashboardFilterOption> BuildOptions(
        IEnumerable<CleanedLabelLineItem> rows,
        Func<CleanedLabelLineItem, string?> selector)
    {
        return rows
            .GroupBy(row => NormalizeGroupName(selector(row)))
            .Select(group => new DashboardFilterOption
            {
                Value = group.Key,
                ItemCount = group.Count(),
                Spend = group.Sum(row => row.Source.Spend ?? 0m)
            })
            .OrderByDescending(option => option.Spend)
            .ThenBy(option => option.Value)
            .ToList();
    }

    private static HashSet<string> BuildOutlierKeys(IEnumerable<PriceOutlierCandidate> outliers)
    {
        return outliers
            .Select(outlier => OutlierKey(outlier.ItemNo, outlier.LabelSize, outlier.Material))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsOutlier(CleanedLabelLineItem row, ISet<string> outlierKeys)
    {
        return outlierKeys.Contains(OutlierKey(row.Source.ItemNo, row.NormalizedLabelSize, row.NormalizedMaterial));
    }

    private static string OutlierKey(string? itemNo, string? labelSize, string? material)
    {
        return $"{NormalizeGroupName(itemNo)}|{NormalizeGroupName(labelSize)}|{NormalizeGroupName(material)}";
    }

    private static DashboardMetric Metric(string name, int? value)
    {
        return new DashboardMetric
        {
            Name = name,
            Value = value?.ToString("N0", CultureInfo.CurrentCulture) ?? "-"
        };
    }

    private static DashboardMetric Metric(string name, decimal? value, string format)
    {
        return new DashboardMetric
        {
            Name = name,
            Value = value?.ToString(format, CultureInfo.CurrentCulture) ?? "-"
        };
    }

    private static bool Matches(string? actual, string expected)
    {
        return string.Equals(NormalizeGroupName(actual), expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeGroupName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(missing)" : value.Trim();
    }

    private static int DistinctCount(IEnumerable<string?> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }
}
