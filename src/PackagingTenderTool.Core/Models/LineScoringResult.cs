namespace PackagingTenderTool.Core.Models;

public sealed class LineScoringResult
{
    public ScoreBreakdown ScoreBreakdown { get; set; } = new();

    public decimal? EprFee { get; set; }

    public List<ScoreExplanation> Explanations { get; set; } = [];

    public List<ManualReviewFlag> ManualReviewFlags { get; set; } = [];
}

public sealed class ScoreExplanation
{
    public string Dimension { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}

