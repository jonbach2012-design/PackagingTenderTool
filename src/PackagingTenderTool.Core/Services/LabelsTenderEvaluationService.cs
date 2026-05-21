using PackagingTenderTool.Core.Analytics;
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class LabelsTenderEvaluationService
{
    private readonly LabelsExcelImportService importService;
    private readonly LineEvaluationService lineEvaluationService;
    private readonly SupplierAggregationService supplierAggregationService;
    private readonly SupplierClassificationService supplierClassificationService;
    private readonly LabelDataCleaningService dataCleaningService;
    private readonly TenderAnalyticsService analyticsService;

    public LabelsTenderEvaluationService()
        : this(
            new LabelsExcelImportService(),
            new LineEvaluationService(new LabelsEvaluationStrategy(new EprFeeService())),
            new SupplierAggregationService(),
            new SupplierClassificationService(),
            new LabelDataCleaningService(),
            new TenderAnalyticsService())
    {
    }

    public LabelsTenderEvaluationService(
        LabelsExcelImportService importService,
        LineEvaluationService lineEvaluationService,
        SupplierAggregationService supplierAggregationService,
        SupplierClassificationService supplierClassificationService,
        LabelDataCleaningService? dataCleaningService = null,
        TenderAnalyticsService? analyticsService = null)
    {
        this.importService = importService;
        this.lineEvaluationService = lineEvaluationService;
        this.supplierAggregationService = supplierAggregationService;
        this.supplierClassificationService = supplierClassificationService;
        this.dataCleaningService = dataCleaningService ?? new LabelDataCleaningService();
        this.analyticsService = analyticsService ?? new TenderAnalyticsService();
    }

    public TenderEvaluationResult ImportAndEvaluate(
        string filePath,
        string tenderName,
        TenderSettings? tenderSettings = null)
    {
        var importResult = importService.ImportTenderWithReport(filePath, tenderName, tenderSettings);

        return Evaluate(importResult);
    }

    public TenderEvaluationResult ImportAndEvaluate(
        Stream excelStream,
        string tenderName,
        TenderSettings? tenderSettings = null)
    {
        var importResult = importService.ImportTenderWithReport(excelStream, tenderName, tenderSettings);

        return Evaluate(importResult);
    }

    public TenderEvaluationResult Evaluate(Tender tender)
    {
        ArgumentNullException.ThrowIfNull(tender);

        var lineEvaluations = lineEvaluationService
            .EvaluateMany(tender.LabelLineItems, tender.Settings)
            .ToList();
        var supplierEvaluations = supplierAggregationService
            .AggregateBySupplierName(lineEvaluations)
            .ToList();

        supplierClassificationService.ApplyClassifications(supplierEvaluations);

        var cleanedRows = dataCleaningService.CleanMany(tender.LabelLineItems).ToList();

        var bestBidBaseline = ComputeBestBidBaseline(lineEvaluations);
        var currentContractBaseline = ComputeCurrentContractBaseline(tender.LabelLineItems);

        return new TenderEvaluationResult
        {
            Tender = tender,
            LineEvaluations = lineEvaluations,
            SupplierEvaluations = supplierEvaluations,
            CleanedLineItems = cleanedRows,
            Analytics = analyticsService.Analyze(cleanedRows),
            BestBidBaseline = bestBidBaseline,
            CurrentContractPriceBaseline = currentContractBaseline
        };
    }

    public TenderEvaluationResult Evaluate(LabelsTenderImportResult importResult)
    {
        ArgumentNullException.ThrowIfNull(importResult);

        var result = Evaluate(importResult.Tender);
        result.ImportSummary = importResult.Summary;
        result.ImportIssues = importResult.Issues;
        result.CleanedLineItems = importResult.CleanedRows;
        result.Analytics = analyticsService.Analyze(importResult.CleanedRows);

        return result;
    }

    private static IReadOnlyDictionary<string, decimal> ComputeBestBidBaseline(
        IReadOnlyList<LineEvaluation> lineEvaluations)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var evaluation in lineEvaluations)
        {
            var itemNo = evaluation.LineItem.ItemNo?.Trim();
            if (string.IsNullOrWhiteSpace(itemNo))
            {
                continue;
            }

            var price = evaluation.LineItem.Price ?? evaluation.LineItem.PricePerThousand;
            if (price is null or <= 0m)
            {
                continue;
            }

            if (!result.TryGetValue(itemNo, out var existing) || price.Value < existing)
            {
                result[itemNo] = price.Value;
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<string, decimal> ComputeCurrentContractBaseline(
        IReadOnlyList<LabelLineItem> lineItems)
    {
        // current_price is a unit price — multiply by quantity to get line total.
        // Use first available current_price and quantity per ItemNo.
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var byItemNo = lineItems
            .Where(i => !string.IsNullOrWhiteSpace(i.ItemNo))
            .GroupBy(i => i.ItemNo!.Trim(), StringComparer.OrdinalIgnoreCase);

        foreach (var group in byItemNo)
        {
            var contractPrice = group
                .Select(i => i.CurrentContractPrice)
                .FirstOrDefault(p => p is > 0m);

            if (contractPrice is null or <= 0m)
            {
                continue;
            }

            var qty = group
                .Select(i => i.Quantity)
                .FirstOrDefault(q => q is > 0m);

            if (qty is null or <= 0m)
            {
                continue;
            }

            result[group.Key] = decimal.Round(
                (contractPrice.Value / 1000m) * qty.Value,
                0,
                MidpointRounding.AwayFromZero);
        }

        return result;
    }
}
