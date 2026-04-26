namespace PackagingTenderTool.Core.Models;

/// <summary>
/// Supplier row for label-tender analysis: pillar scores plus drill-down inputs.
/// </summary>
public sealed class SupplierModel
{
    /// <summary>Stable unique identifier (used for UI keys and selection).</summary>
    public string SupplierId { get; init; } = string.Empty;

    public string SupplierName { get; init; } = string.Empty;

    // --- Label specification filters (table view) ---
    public string LabelType { get; init; } = string.Empty;

    public string Material { get; init; } = string.Empty;

    public string Adhesive { get; init; } = string.Empty;

    /// <summary>Plants / locations covered by the supplier.</summary>
    public IReadOnlyList<string> Sites { get; init; } = Array.Empty<string>();

    /// <summary>Requested quantity / annual volume (labels).</summary>
    public decimal QuantityLabels { get; init; }

    // --- Category manager inputs ---
    /// <summary>Minimum order quantity (units/labels).</summary>
    public decimal MOQUnits { get; init; }

    /// <summary>One-time implementation / startup cost (currency units) if changing supplier.</summary>
    public decimal StartupCost { get; init; }

    /// <summary>Monthly technical / audit / support cost (currency units).</summary>
    public decimal MonthlySupportCost { get; init; }

    /// <summary>MOQ penalty % used as inventory-binding risk (0–15 typical).</summary>
    public decimal MoqPenaltyPct { get; init; }

    /// <summary>
    /// Legacy alias for startup cost (kept for compatibility with older demo/import code paths).
    /// </summary>
    public decimal SwitchingCost => StartupCost;

    /// <summary>Lead time in weeks (used for technical penalty).</summary>
    public int LeadTimeWeeks { get; init; }

    // --- Regulatory what-if inputs (PPWR/EPR) ---
    public decimal LabelWeightGramsPerUnit { get; init; }

    public RecyclingGrade? RecyclingGrade { get; init; }

    public decimal? RecycledContentPercent { get; init; }

    // --- "Non-Excel" drill-down / technical insight ---
    public decimal PriceAt1k { get; init; }

    public decimal PriceAt5k { get; init; }

    public decimal PriceAt10k { get; init; }

    public string SupplierComments { get; init; } = string.Empty;

    public string LogisticsConstraints { get; init; } = string.Empty;

    /// <summary>Commercial sub-parameter (e.g. unit price).</summary>
    public decimal Price { get; init; }

    /// <summary>Technical / sustainability sub-parameter.</summary>
    public decimal Co2Impact { get; init; }

    /// <summary>Regulatory / logistics sub-parameter (calendar days).</summary>
    public decimal DeliveryTimeDays { get; init; }

    /// <summary>ISO country code or name for geographic cockpit KPIs.</summary>
    public string Country { get; init; } = string.Empty;

    /// <summary>Number of production / supply sites represented by this bid.</summary>
    public int SiteCount { get; init; }

    /// <summary>Pillar score 0–100 (commercial dimension).</summary>
    public decimal CommercialScore { get; init; }

    /// <summary>Pillar score 0–100 (technical dimension).</summary>
    public decimal TechnicalScore { get; init; }

    /// <summary>Pillar score 0–100 (regulatory dimension).</summary>
    public decimal RegulatoryScore { get; init; }

    /// <summary>Display helper for the drill-down table.</summary>
    public decimal PricePer1000 => Price * 1000m;

    /// <summary>
    /// Weighted final score when pillar scores are 0–100 and weights are percentage points summing to 100.
    /// Maximum is exactly 100.0 when all pillar scores are 100.
    /// </summary>
    public decimal ComputeFinalScore(decimal commercialWeightPct, decimal technicalWeightPct, decimal regulatoryWeightPct)
    {
        return (CommercialScore * (commercialWeightPct / 100m))
               + (TechnicalScore * (technicalWeightPct / 100m))
               + (RegulatoryScore * (regulatoryWeightPct / 100m));
    }
}

public enum SupplierOutlierKind
{
    None = 0,
    PotentialUnitError = 1
}

public sealed record SupplierOutlierFlag(
    string SupplierId,
    string SupplierName,
    string Field,
    SupplierOutlierKind Kind,
    string Message);

public sealed record SupplierOutlierReport(
    IReadOnlyDictionary<string, IReadOnlyList<SupplierOutlierFlag>> BySupplierId)
{
    public int TotalFlags => BySupplierId.Values.Sum(v => v.Count);

    public int SupplierCountWithFlags => BySupplierId.Count(kvp => kvp.Value.Count > 0);

    public bool HasFlags => TotalFlags > 0;
}

public static class SupplierOutlierDetection
{
    public static SupplierOutlierReport Detect(IReadOnlyList<SupplierModel> suppliers)
    {
        ArgumentNullException.ThrowIfNull(suppliers);

        // Never throw on duplicate supplier names — key by SupplierId and aggregate.
        var bySupplier = new Dictionary<string, List<SupplierOutlierFlag>>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in suppliers)
        {
            if (string.IsNullOrWhiteSpace(s.SupplierId) || string.IsNullOrWhiteSpace(s.SupplierName))
            {
                continue;
            }

            if (!bySupplier.TryAdd(s.SupplierId, []))
            {
                // already present
            }
        }

        if (suppliers.Count < 2)
        {
            var empty = bySupplier.ToDictionary(
                kvp => kvp.Key,
                _ => (IReadOnlyList<SupplierOutlierFlag>)Array.Empty<SupplierOutlierFlag>(),
                StringComparer.OrdinalIgnoreCase);
            return new SupplierOutlierReport(empty);
        }

        FlagRelativeToOthersAverage(
            suppliers,
            field: "Price",
            selector: s => s.Price,
            message: "Check units (100 vs 10,000?)",
            bySupplier);

        FlagRelativeToOthersAverage(
            suppliers,
            field: "Quantity",
            selector: s => s.QuantityLabels,
            message: "Check units (100 vs 10,000?)",
            bySupplier);

        var frozen = bySupplier.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<SupplierOutlierFlag>)kvp.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

        return new SupplierOutlierReport(frozen);
    }

    private static void FlagRelativeToOthersAverage(
        IReadOnlyList<SupplierModel> suppliers,
        string field,
        Func<SupplierModel, decimal> selector,
        string message,
        Dictionary<string, List<SupplierOutlierFlag>> bySupplier)
    {
        foreach (var s in suppliers)
        {
            var v = selector(s);
            if (v <= 0m)
                continue;

            if (string.IsNullOrWhiteSpace(s.SupplierId) || string.IsNullOrWhiteSpace(s.SupplierName))
                continue;

            var others = suppliers
                .Where(o => !string.Equals(o.SupplierId, s.SupplierId, StringComparison.OrdinalIgnoreCase))
                .Select(selector)
                .Where(x => x > 0m)
                .ToArray();

            if (others.Length == 0)
                continue;

            var avgOthers = others.Average();
            if (avgOthers <= 0.0000001m)
                continue;

            var ratio = v / avgOthers;
            // DSH logic: flag if deviates >50% from peer average OR looks like unit mismatch (10×).
            var unitMismatch = ratio >= 10m || ratio <= 0.1m;
            if (ratio > 1.5m || ratio < 0.5m || unitMismatch)
            {
                if (!bySupplier.TryGetValue(s.SupplierId, out var list))
                {
                    list = [];
                    bySupplier[s.SupplierId] = list;
                }

                list.Add(new SupplierOutlierFlag(
                    s.SupplierId,
                    s.SupplierName,
                    field,
                    SupplierOutlierKind.PotentialUnitError,
                    message));
            }
        }
    }
}

/// <summary>One grid row: supplier plus weighted total for approved pillar weights.</summary>
public sealed record SupplierPillarAnalysisRow(SupplierModel Supplier, decimal TotalScore);

/// <summary>
/// Weighted pillar aggregation. Weights are whole-number percentages that sum to 100.
/// </summary>
public static class SupplierPillarAnalysis
{
    /// <summary>Delegates to <see cref="SupplierModel.ComputeFinalScore"/>.</summary>
    public static decimal ComputeWeightedTotal(
        SupplierModel supplier,
        decimal commercialWeight,
        decimal technicalWeight,
        decimal regulatoryWeight)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        return supplier.ComputeFinalScore(commercialWeight, technicalWeight, regulatoryWeight);
    }

    public static IReadOnlyList<SupplierPillarAnalysisRow> BuildOrdered(
        IReadOnlyList<SupplierModel> suppliers,
        decimal commercialWeight,
        decimal technicalWeight,
        decimal regulatoryWeight)
    {
        ArgumentNullException.ThrowIfNull(suppliers);
        return suppliers
            .Select(s => new SupplierPillarAnalysisRow(
                s,
                decimal.Round(ComputeWeightedTotal(s, commercialWeight, technicalWeight, regulatoryWeight), 1, MidpointRounding.AwayFromZero)))
            .OrderByDescending(r => r.TotalScore)
            .ThenBy(r => r.Supplier.SupplierName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
