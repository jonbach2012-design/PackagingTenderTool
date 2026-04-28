using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Analytics;

public sealed class TenderAnalyticsService
{
    private readonly IEprFeeService eprFeeService;

    public TenderAnalyticsService(IEprFeeService eprFeeService)
    {
        this.eprFeeService = eprFeeService ?? throw new ArgumentNullException(nameof(eprFeeService));
    }

    public TenderAnalyticsService()
        : this(new EprFeeService())
    {
    }

    public TenderAnalyticsSummary Analyze(IEnumerable<CleanedLabelLineItem> cleanedRows)
    {
        ArgumentNullException.ThrowIfNull(cleanedRows);

        var rows = cleanedRows
            .Where(row => row.Source.Spend is > 0)
            .ToList();
        var totalSpend = rows.Sum(row => row.Source.Spend!.Value);

        return new TenderAnalyticsSummary
        {
            TotalSpend = totalSpend,
            ItemCount = rows.Count,
            SpendByCountry = BuildBreakdown(rows, row => row.Country),
            SpendBySite = BuildBreakdown(rows, row => row.Source.Site),
            SpendByLabelSize = BuildBreakdown(rows, row => row.NormalizedLabelSize),
            SpendByMaterial = BuildBreakdown(rows, row => row.NormalizedMaterial),
            TopSpendItems = BuildTopSpendItems(rows),
            PriceOutlierCandidates = BuildPriceOutlierCandidates(rows),
            ConsolidationCandidates = BuildConsolidationCandidates(rows)
        };
    }

    private static List<SpendBreakdownItem> BuildBreakdown(
        IReadOnlyCollection<CleanedLabelLineItem> rows,
        Func<CleanedLabelLineItem, string?> keySelector)
    {
        var totalSpend = rows.Sum(row => row.Source.Spend!.Value);
        return rows
            .GroupBy(row => NormalizeGroupName(keySelector(row)))
            .Select(group =>
            {
                var spend = group.Sum(row => row.Source.Spend!.Value);
                return new SpendBreakdownItem
                {
                    Name = group.Key,
                    Spend = spend,
                    ShareOfTotal = totalSpend == 0m ? 0m : Math.Round(spend / totalSpend * 100m, 2),
                    ItemCount = group.Count()
                };
            })
            .OrderByDescending(item => item.Spend)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private static List<TopSpendItem> BuildTopSpendItems(IEnumerable<CleanedLabelLineItem> rows)
    {
        return rows
            .OrderByDescending(row => row.Source.Spend)
            .Take(10)
            .Select(row => new TopSpendItem
            {
                ItemNo = row.Source.ItemNo,
                ItemName = row.Source.ItemName,
                SupplierName = row.Source.SupplierName,
                Site = row.Source.Site,
                LabelSize = row.NormalizedLabelSize,
                Spend = row.Source.Spend!.Value
            })
            .ToList();
    }

    private static List<PriceOutlierCandidate> BuildPriceOutlierCandidates(IReadOnlyCollection<CleanedLabelLineItem> rows)
    {
        return rows
            .Where(row => row.Source.PricePerThousand is > 0
                && !string.IsNullOrWhiteSpace(row.NormalizedLabelSize))
            .GroupBy(row => $"{row.NormalizedLabelSize}|{row.NormalizedMaterial}")
            .Where(group => group.Count() >= 3)
            .SelectMany(group =>
            {
                var median = Median(group.Select(row => row.Source.PricePerThousand!.Value).Order().ToList());
                if (median <= 0m)
                {
                    return [];
                }

                return group
                    .Where(row => row.Source.PricePerThousand!.Value > median * 1.25m)
                    .Select(row => new PriceOutlierCandidate
                    {
                        ItemNo = row.Source.ItemNo,
                        ItemName = row.Source.ItemName,
                        LabelSize = row.NormalizedLabelSize,
                        Material = row.NormalizedMaterial,
                        PricePerThousand = row.Source.PricePerThousand!.Value,
                        GroupMedianPricePerThousand = median,
                        PercentAboveMedian = Math.Round((row.Source.PricePerThousand!.Value - median) / median * 100m, 2)
                    });
            })
            .OrderByDescending(candidate => candidate.PercentAboveMedian)
            .Take(10)
            .ToList();
    }

    private static List<ConsolidationCandidate> BuildConsolidationCandidates(IReadOnlyCollection<CleanedLabelLineItem> rows)
    {
        return rows
            .Where(row => !string.IsNullOrWhiteSpace(row.NormalizedLabelSize)
                && !string.IsNullOrWhiteSpace(row.NormalizedMaterial))
            .GroupBy(row => new
            {
                LabelSize = row.NormalizedLabelSize!,
                Material = row.NormalizedMaterial!
            })
            .Select(group => new ConsolidationCandidate
            {
                LabelSize = group.Key.LabelSize,
                Material = group.Key.Material,
                Spend = group.Sum(row => row.Source.Spend ?? 0m),
                ItemCount = group.Count(),
                SiteCount = group.Select(row => NormalizeGroupName(row.Source.Site)).Distinct().Count()
            })
            .Where(candidate => candidate.ItemCount >= 3)
            .OrderByDescending(candidate => candidate.Spend)
            .ThenByDescending(candidate => candidate.ItemCount)
            .Take(10)
            .ToList();
    }

    private static decimal Median(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return 0m;
        }

        var midpoint = values.Count / 2;
        return values.Count % 2 == 1
            ? values[midpoint]
            : (values[midpoint - 1] + values[midpoint]) / 2m;
    }

    private static string NormalizeGroupName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(missing)" : value.Trim();
    }

    public TenderTcoSummary CalculateTco(
        IEnumerable<LineEvaluation> lineEvaluations,
        decimal volumeMultiplier = 1.0m,
        TenderStressParameters? stress = null)
    {
        ArgumentNullException.ThrowIfNull(lineEvaluations);

        if (volumeMultiplier < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(volumeMultiplier), "Volume multiplier cannot be negative.");
        }

        stress ??= new TenderStressParameters();
        if (stress.EprInflationMultiplier < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(stress.EprInflationMultiplier), "EPR inflation multiplier cannot be negative.");
        }

        if (stress.MaterialPriceMultiplier < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(stress.MaterialPriceMultiplier), "Material price multiplier cannot be negative.");
        }

        decimal totalNetSpend = 0m;
        decimal totalEprImpact = 0m;
        decimal weightedRegulatorySum = 0m;
        decimal weightingSpendSum = 0m;

        foreach (var line in lineEvaluations)
        {
            var quantity = (line.LineItem.Quantity ?? 0m) * volumeMultiplier;

            // Spec: Total Net Spend = Price * Volume.
            // Prefer explicit price inputs, then fall back to amount fields if price is unavailable.
            decimal? netSpend = (line.LineItem.PricePerThousand is > 0m
                    ? (line.LineItem.PricePerThousand.Value / 1000m) * quantity
                    : (decimal?)null)
                ?? (line.LineItem.Price is > 0m
                    ? line.LineItem.Price.Value * quantity
                    : (decimal?)null)
                ?? line.LineItem.TheoreticalSpend
                ?? line.LineItem.Spend;

            // Only apply material price multiplier when net spend is price-derived (first two branches).
            // If we fell back to imported amount fields, we treat it as already-realized spend.
            if (line.LineItem.PricePerThousand is > 0m || line.LineItem.Price is > 0m)
            {
                netSpend *= stress.MaterialPriceMultiplier;
            }

            totalNetSpend += netSpend ?? 0m;

            // EPR fee is treated as per-unit fee (derived from unit weight in the evaluation layer).
            var eprPerUnit = line.EprFee ?? 0m;
            totalEprImpact += (eprPerUnit * quantity) * stress.EprInflationMultiplier;

            if (line.LineItem.Spend is > 0m && line.ScoreBreakdown.Regulatory is not null)
            {
                weightedRegulatorySum += line.ScoreBreakdown.Regulatory.Value * line.LineItem.Spend.Value;
                weightingSpendSum += line.LineItem.Spend.Value;
            }
        }

        return new TenderTcoSummary
        {
            TotalNetSpend = decimal.Round(totalNetSpend, 2),
            TotalEprImpact = decimal.Round(totalEprImpact, 2),
            WeightedRegulatoryScore = weightingSpendSum <= 0m
                ? null
                : decimal.Round(weightedRegulatorySum / weightingSpendSum, 2)
        };
    }

    public IReadOnlyDictionary<string, decimal> CalculateSupplierRiskScores(
        IEnumerable<LineEvaluation> lineEvaluations,
        decimal volumeMultiplier = 1.0m,
        TenderStressParameters? stress = null)
    {
        ArgumentNullException.ThrowIfNull(lineEvaluations);
        stress ??= new TenderStressParameters();

        // Risk definition: share of TCO that is EPR-driven (0-100).
        // Higher EPR share => higher risk.
        var bySupplier = lineEvaluations
            .GroupBy(line => line.LineItem.SupplierName ?? string.Empty);

        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in bySupplier)
        {
            var tco = CalculateTco(group, volumeMultiplier, stress);
            var total = tco.AggregatedTco;
            var share = total <= 0m ? 0m : (tco.TotalEprImpact / total) * 100m;
            result[group.Key] = decimal.Round(Clamp(share, 0m, 100m), 2);
        }

        return result;
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public IReadOnlyList<CtrSupplierSummary> CalculateCtrDecisionScores(
        IEnumerable<LineEvaluation> lineEvaluations,
        CtrWeights weights,
        IReadOnlyDictionary<string, decimal>? manualTechnicalScores = null,
        decimal volumeMultiplier = 1.0m,
        TenderStressParameters? stress = null)
    {
        ArgumentNullException.ThrowIfNull(lineEvaluations);
        ArgumentNullException.ThrowIfNull(weights);
        stress ??= new TenderStressParameters();

        var w = Normalize(weights);
        var bySupplier = lineEvaluations.GroupBy(line => line.LineItem.SupplierName ?? string.Empty).ToList();

        // TCO per supplier (used for commercial normalization)
        var tcoBySupplier = bySupplier
            .ToDictionary(
                group => group.Key,
                group => CalculateTco(group, volumeMultiplier, stress).AggregatedTco,
                StringComparer.OrdinalIgnoreCase);

        var minTco = tcoBySupplier.Values.Count == 0 ? 0m : tcoBySupplier.Values.Min();
        if (minTco <= 0m)
        {
            minTco = 0m;
        }

        var summaries = new List<CtrSupplierSummary>();
        foreach (var group in bySupplier)
        {
            var supplierName = group.Key;
            var supplierTco = tcoBySupplier.TryGetValue(supplierName, out var tco) ? tco : 0m;

            // Commercial: lowest TCO => 100, else (minTco / supplierTco) * 100
            var commercial = supplierTco <= 0m || minTco <= 0m
                ? 0m
                : decimal.Round(Clamp(minTco / supplierTco * 100m, 0m, 100m), 2);

            decimal technical;
            if (manualTechnicalScores is not null
                && manualTechnicalScores.TryGetValue(supplierName, out var manualTechnical))
            {
                technical = Clamp(manualTechnical, 0m, 100m);
            }
            else
            {
                // Technical: spend-weighted average of TechnicalRating if present, else fall back to line Technical score
                technical = SpendWeightedAverage(group, line =>
                    line.LineItem.TechnicalRating
                    ?? line.ScoreBreakdown.Technical);
            }

            // Regulatory: spend-weighted average of regulatory
            var regulatory = SpendWeightedAverage(group, line => line.ScoreBreakdown.Regulatory);

            var decision = decimal.Round(
                commercial * w.CommercialWeight
                + technical * w.TechnicalWeight
                + regulatory * w.RegulatoryWeight,
                2);

            summaries.Add(new CtrSupplierSummary
            {
                SupplierName = supplierName,
                TotalTco = decimal.Round(supplierTco, 2),
                CommercialScore = commercial,
                TechnicalScore = technical,
                RegulatoryScore = regulatory,
                DecisionScore = decision
            });
        }

        return summaries
            .OrderByDescending(s => s.DecisionScore)
            .ThenBy(s => s.SupplierName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static CtrWeights Normalize(CtrWeights weights)
    {
        var sum = weights.CommercialWeight + weights.TechnicalWeight + weights.RegulatoryWeight;
        if (sum <= 0m)
        {
            return new CtrWeights { CommercialWeight = 0.60m, TechnicalWeight = 0.30m, RegulatoryWeight = 0.10m };
        }

        return new CtrWeights
        {
            CommercialWeight = weights.CommercialWeight / sum,
            TechnicalWeight = weights.TechnicalWeight / sum,
            RegulatoryWeight = weights.RegulatoryWeight / sum
        };
    }

    private static decimal SpendWeightedAverage(IEnumerable<LineEvaluation> lines, Func<LineEvaluation, decimal?> selector)
    {
        decimal weighted = 0m;
        decimal spendSum = 0m;

        foreach (var line in lines)
        {
            var spend = line.LineItem.Spend;
            if (spend is null || spend <= 0m)
            {
                continue;
            }

            var value = selector(line);
            if (value is null)
            {
                continue;
            }

            weighted += value.Value * spend.Value;
            spendSum += spend.Value;
        }

        return spendSum <= 0m ? 0m : decimal.Round(weighted / spendSum, 2);
    }
}
