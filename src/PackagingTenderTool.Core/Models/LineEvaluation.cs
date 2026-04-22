namespace PackagingTenderTool.Core.Models;

public sealed class LineEvaluation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LineItemId { get; set; }

    public LabelLineItem LineItem { get; set; } = new();

    public ScoreBreakdown ScoreBreakdown { get; set; } = new();

    public List<ScoreExplanation> Explanations { get; set; } = [];

    public List<ManualReviewFlag> ManualReviewFlags { get; set; } = [];

    public bool RequiresManualReview => ManualReviewFlags.Count > 0;
}
