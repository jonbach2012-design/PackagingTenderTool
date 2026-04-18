using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class LabelsTenderEvaluationService
{
    private readonly LabelsExcelImportService importService;
    private readonly LineEvaluationService lineEvaluationService;
    private readonly SupplierAggregationService supplierAggregationService;
    private readonly SupplierClassificationService supplierClassificationService;

    public LabelsTenderEvaluationService()
        : this(
            new LabelsExcelImportService(),
            new LineEvaluationService(),
            new SupplierAggregationService(),
            new SupplierClassificationService())
    {
    }

    public LabelsTenderEvaluationService(
        LabelsExcelImportService importService,
        LineEvaluationService lineEvaluationService,
        SupplierAggregationService supplierAggregationService,
        SupplierClassificationService supplierClassificationService)
    {
        this.importService = importService;
        this.lineEvaluationService = lineEvaluationService;
        this.supplierAggregationService = supplierAggregationService;
        this.supplierClassificationService = supplierClassificationService;
    }

    public TenderEvaluationResult ImportAndEvaluate(
        string filePath,
        string tenderName,
        TenderSettings? tenderSettings = null)
    {
        var tender = importService.ImportTender(filePath, tenderName, tenderSettings);

        return Evaluate(tender);
    }

    public TenderEvaluationResult ImportAndEvaluate(
        Stream excelStream,
        string tenderName,
        TenderSettings? tenderSettings = null)
    {
        var tender = importService.ImportTender(excelStream, tenderName, tenderSettings);

        return Evaluate(tender);
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

        return new TenderEvaluationResult
        {
            Tender = tender,
            LineEvaluations = lineEvaluations,
            SupplierEvaluations = supplierEvaluations
        };
    }
}
