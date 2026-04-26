namespace PackagingTenderTool.Blazor.Models;

public sealed record LineTcoEntry(
    TenderLine Line,
    SupplierOffer Offer,
    TcoResult Result,
    decimal WeightedDecisionScore,
    decimal DecisionScoreIndex);

