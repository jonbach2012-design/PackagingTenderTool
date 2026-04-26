namespace PackagingTenderTool.Blazor.Models;

public sealed record TcoDecisionOutput(
    TcoResult Actual,
    decimal WeightedDecisionScore,
    decimal DecisionScoreIndex);

