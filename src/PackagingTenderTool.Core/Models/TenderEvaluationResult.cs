using PackagingTenderTool.Core.Analytics;
using PackagingTenderTool.Core.Import;

namespace PackagingTenderTool.Core.Models;

public sealed class TenderEvaluationResult
{
    public Tender Tender { get; set; } = new();

    public List<LineEvaluation> LineEvaluations { get; set; } = [];

    public List<SupplierEvaluation> SupplierEvaluations { get; set; } = [];

    public LabelsImportSummary? ImportSummary { get; set; }

    public List<ImportValidationIssue> ImportIssues { get; set; } = [];

    public List<CleanedLabelLineItem> CleanedLineItems { get; set; } = [];

    public TenderAnalyticsSummary? Analytics { get; set; }
}
