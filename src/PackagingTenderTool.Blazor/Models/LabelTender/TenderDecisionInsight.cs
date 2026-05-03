namespace PackagingTenderTool.Blazor.Models.LabelTender;

/// <summary>
/// Strategic decision summary: recommended pick vs. commercial anchor and best TCO, plus narrative.
/// </summary>
public sealed record TenderDecisionInsight
{
    public required string Narrative { get; init; }

    public required string RecommendedSupplierId { get; init; }
    public required string RecommendedSupplierName { get; init; }
    public required decimal RecommendedWeightedScore { get; init; }

    /// <summary>Share of final weighted score from price index pillar (0–100).</summary>
    public required decimal WeightedShareCommercial { get; init; }

    /// <summary>Share of final weighted score from technical pillar.</summary>
    public required decimal WeightedShareTechnical { get; init; }

    /// <summary>Share of final weighted score from regulatory pillar.</summary>
    public required decimal WeightedShareRegulatory { get; init; }

    public required string LowestPriceSupplierId { get; init; }
    public required string LowestPriceSupplierName { get; init; }
    public required decimal LowestCommercialSpend { get; init; }

    public required string BestTcoSupplierId { get; init; }
    public required string BestTcoSupplierName { get; init; }
    public required decimal BestTcoTotal { get; init; }

    /// <summary>True when recommendation matches lowest commercial spend row.</summary>
    public required bool RecommendedMatchesLowestPrice { get; init; }

    /// <summary>True when recommendation matches lowest total TCO.</summary>
    public required bool RecommendedMatchesBestTco { get; init; }

    public required string PrimaryValueDriver { get; init; }

    /// <summary>High when the winner is stable under ±5% pillar-weight shifts; otherwise sensitive.</summary>
    public required DecisionConfidence ConfidenceLevel { get; init; }

    /// <summary>True when pillar-weight sensitivity analysis flips the recommended supplier.</summary>
    public required bool IsSensitiveDecision { get; init; }
}
