namespace PackagingTenderTool.Blazor.Models;

public sealed record AuditGridRow(
    string LineItem,
    string Supplier,
    string Site,
    string Category,
    string MaterialClass,
    decimal BasePrice,
    decimal ActualTco,
    decimal WeightedDecisionScore,
    decimal DecisionScoreIndex,
    decimal DataQualityScore);

