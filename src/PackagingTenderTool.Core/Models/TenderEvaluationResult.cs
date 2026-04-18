namespace PackagingTenderTool.Core.Models;

public sealed class TenderEvaluationResult
{
    public Tender Tender { get; set; } = new();

    public List<LineEvaluation> LineEvaluations { get; set; } = [];

    public List<SupplierEvaluation> SupplierEvaluations { get; set; } = [];
}
