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

        return new TenderEvaluationResult
        {
            Tender = tender,
            LineEvaluations = lineEvaluations,
            SupplierEvaluations = supplierEvaluations,
            CleanedLineItems = cleanedRows,
            Analytics = analyticsService.Analyze(cleanedRows)
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
}
