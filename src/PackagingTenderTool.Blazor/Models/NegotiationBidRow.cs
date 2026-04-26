namespace PackagingTenderTool.Blazor.Models;

public sealed record NegotiationBidRow(
    string Supplier,
    decimal ActualTco,
    decimal DecisionScoreIndex,
    decimal VariancePercent,
    bool IsBestInScenario);

