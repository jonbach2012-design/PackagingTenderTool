using System.Globalization;
using PackagingTenderTool.Blazor.Models.LabelTender;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Blazor.Services;

public sealed class TcoEngineService : ITcoEngineService
{
    private readonly IRegulatoryService regulatory;
    private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private const decimal MissingWeightPenaltyPct = 0.15m;

    public TcoEngineService(IRegulatoryService regulatory)
    {
        this.regulatory = regulatory ?? throw new ArgumentNullException(nameof(regulatory));
    }

    public LabelTenderDashboardDto CalculateResult(PackagingProfileSession session, SupplierModel supplier)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(supplier);

        return GetResults(session, [supplier]).First();
    }

    public IReadOnlyList<LabelTenderDashboardDto> GetResults(PackagingProfileSession session, IReadOnlyList<SupplierModel> suppliers)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(suppliers);

        var results = new List<LabelTenderDashboardDto>(suppliers.Count);
        if (suppliers.Count == 0)
            return results;

        foreach (var s in suppliers)
        {
            var supplierId = s.SupplierId ?? string.Empty;
            var volume = s.QuantityLabels <= 0 ? 0m : s.QuantityLabels;
            var commercial = volume * s.Price;

            var country = string.IsNullOrWhiteSpace(s.Country) ? "DK" : s.Country;
            var grade = session.GetSupplierRecyclabilityGrade(supplierId);

            var epr = 0m;
            var missingWeightPenalty = 0m;
            var hasWeight = s.LabelWeightGramsPerUnit > 0m;

            if (session.ApplyPpwr2030Scenario)
            {
                if (hasWeight)
                {
                    var eprBase = regulatory.CalculateEpr2026ForecastFee(
                        countryCode: country,
                        labelWeightGramsPerUnit: s.LabelWeightGramsPerUnit,
                        quantityUnits: volume);

                    var gradeFactor = grade switch
                    {
                        RecyclingGrade.A => 1.0m,
                        RecyclingGrade.B => 1.3m,
                        RecyclingGrade.C => 1.8m,
                        RecyclingGrade.D => 2.4m,
                        RecyclingGrade.E => 3.0m,
                        _ => 1.8m
                    };

                    epr = eprBase * gradeFactor;
                }
                else if (commercial > 0m)
                {
                    // Golden-case robustness: missing weight implies unknown EPR exposure.
                    // Treat it as a penalty to reflect compliance risk deterministically.
                    missingWeightPenalty = decimal.Round(commercial * MissingWeightPenaltyPct, 2, MidpointRounding.AwayFromZero);
                }
            }

            var switching = supplierId.Equals(session.IncumbentSupplierId, StringComparison.OrdinalIgnoreCase)
                ? 0m
                : session.GetStartupCost(supplierId) + (session.GetMonthlySupportCost(supplierId) * 12m);

            var moq = commercial * (session.GetSupplierMoqPenaltyPct(supplierId) / 100m);

            // Technical score adjustment (lead time penalty).
            var tech = s.TechnicalScore;
            if (s.LeadTimeWeeks > 4)
                tech = Math.Max(0m, tech - ((s.LeadTimeWeeks - 4) * 5m));

            var regScore = s.RegulatoryScore;
            var total = commercial + epr + missingWeightPenalty + switching + moq;

            var isCompliant = hasWeight;

            results.Add(new LabelTenderDashboardDto
            {
                SupplierId = supplierId,
                SupplierName = s.SupplierName,
                Commercial = decimal.Round(commercial, 0, MidpointRounding.AwayFromZero),
                Epr = decimal.Round(epr + missingWeightPenalty, 0, MidpointRounding.AwayFromZero),
                Switching = decimal.Round(switching, 0, MidpointRounding.AwayFromZero),
                Moq = decimal.Round(moq, 0, MidpointRounding.AwayFromZero),
                Total = decimal.Round(total, 0, MidpointRounding.AwayFromZero),
                PriceScore = 0m,
                TechScore = decimal.Round(tech, 1, MidpointRounding.AwayFromZero),
                RegScore = decimal.Round(regScore, 1, MidpointRounding.AwayFromZero),
                FinalCtrScore = 0m,
                CommercialWidth = 0,
                RegulatoryWidth = 0,
                SwitchingWidth = 0,
                MoqWidth = 0,
                TotalWidth = 0,
                CalculationBreakdown = string.Empty,
                TechnicalSummary = SummarizeTechnical(s.SupplierComments),
                IsCompliant = isCompliant
            });
        }

        // Price score: min TCO gets 100; others scale by (min/current)*100.
        var minTco = results.Min(r => r.Total);
        if (minTco <= 0m)
            minTco = 1m;

        for (var i = 0; i < results.Count; i++)
        {
            var r = results[i];
            var priceScore = r.Total <= 0m ? 0m : Math.Clamp((minTco / r.Total) * 100m, 0m, 100m);
            var final = ((priceScore * session.Commercial) + (r.TechScore * session.Technical) + (r.RegScore * session.Regulatory)) / 100m;
            final = Math.Clamp(final, 0m, 100m);

            // results are built in the same order as suppliers; keep O(n) and avoid name-based lookups.
            var supplier = suppliers[i];
            var supplierId = supplier.SupplierId ?? string.Empty;
            var supplierVolume = supplier.QuantityLabels;
            var breakdown = BuildBreakdown(r, priceScore, final, session,
                grade: string.IsNullOrWhiteSpace(supplierId) ? RecyclingGrade.C : session.GetSupplierRecyclabilityGrade(supplierId),
                volume: supplierVolume);

            results[i] = new LabelTenderDashboardDto
            {
                SupplierId = r.SupplierId,
                SupplierName = r.SupplierName,
                Commercial = r.Commercial,
                Epr = r.Epr,
                Switching = r.Switching,
                Moq = r.Moq,
                Total = r.Total,
                PriceScore = decimal.Round(priceScore, 1, MidpointRounding.AwayFromZero),
                TechScore = r.TechScore,
                RegScore = r.RegScore,
                FinalCtrScore = decimal.Round(final, 1, MidpointRounding.AwayFromZero),
                CommercialWidth = r.CommercialWidth,
                RegulatoryWidth = r.RegulatoryWidth,
                SwitchingWidth = r.SwitchingWidth,
                MoqWidth = r.MoqWidth,
                TotalWidth = r.TotalWidth,
                CalculationBreakdown = breakdown,
                TechnicalSummary = r.TechnicalSummary,
                IsCompliant = r.IsCompliant
            };
        }

        // Sort by final CTR score descending (most relevant first).
        results = results
            .OrderByDescending(r => r.FinalCtrScore)
            .ThenBy(r => r.SupplierName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var maxTotal = results.Max(r => r.Total);
        if (maxTotal <= 0m)
            maxTotal = 1m;

        var scale = 600.0 / (double)maxTotal;

        for (var i = 0; i < results.Count; i++)
        {
            var r = results[i];
            results[i] = new LabelTenderDashboardDto
            {
                SupplierId = r.SupplierId,
                SupplierName = r.SupplierName,
                Commercial = r.Commercial,
                Epr = r.Epr,
                Switching = r.Switching,
                Moq = r.Moq,
                Total = r.Total,
                PriceScore = r.PriceScore,
                TechScore = r.TechScore,
                RegScore = r.RegScore,
                FinalCtrScore = r.FinalCtrScore,
                CommercialWidth = (double)r.Commercial * scale,
                RegulatoryWidth = (double)r.Epr * scale,
                SwitchingWidth = (double)r.Switching * scale,
                MoqWidth = (double)r.Moq * scale,
                TotalWidth = (double)r.Total * scale,
                CalculationBreakdown = r.CalculationBreakdown,
                TechnicalSummary = r.TechnicalSummary,
                IsCompliant = r.IsCompliant
            };
        }

        return results;
    }

    public static string SummarizeTechnical(string comments)
    {
        if (string.IsNullOrWhiteSpace(comments))
            return "No technical deviations reported.";

        // Lightweight summary heuristic (kept out of UI per ADR 001).
        var c = comments.Trim();
        return c.Length <= 140 ? c : c[..140] + "…";
    }

    public decimal ComputeTenderValueWeighted(
        IReadOnlyList<SupplierPillarAnalysisRow> rows,
        decimal tenderVolumeUnits)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count == 0 || tenderVolumeUnits <= 0m)
            return 0m;

        var denom = rows.Sum(r => r.TotalScore);
        if (denom <= 0.0001m)
            return 0m;

        var weightedUnitPrice = rows.Sum(r => r.Supplier.Price * r.TotalScore) / denom;
        return weightedUnitPrice * tenderVolumeUnits;
    }

    public int CountCompliancePassed(
        IReadOnlyList<SupplierModel> suppliers,
        decimal maxCo2Impact,
        decimal maxLeadTimeDays)
    {
        ArgumentNullException.ThrowIfNull(suppliers);

        if (suppliers.Count == 0)
            return 0;

        return suppliers.Count(s =>
            HasCo2Data(s) && s.Co2Impact <= maxCo2Impact
            && HasLeadTimeData(s) && s.DeliveryTimeDays <= maxLeadTimeDays);
    }

    public bool HasCo2Data(SupplierModel supplier) => supplier.Co2Impact > 0m;

    public bool HasLeadTimeData(SupplierModel supplier) => supplier.DeliveryTimeDays > 0m;

    private static string BuildBreakdown(
        LabelTenderDashboardDto r,
        decimal priceScore,
        decimal finalCtrScore,
        PackagingProfileSession session,
        RecyclingGrade grade,
        decimal volume)
    {
        var priceIndex = decimal.Round(priceScore, 0, MidpointRounding.AwayFromZero).ToString("0", InvariantCulture);
        var tco = r.Total.ToString("N0", DanishCulture);
        var weights = $"Weights: Price {session.Commercial:0}% / Tech {session.Technical:0}% / Reg {session.Regulatory:0}%";

        var ppwrPenalty = session.ApplyPpwr2030Scenario
            ? $"+{r.Epr.ToString("N0", DanishCulture)} regulatory (PPWR ON, grade {grade})"
            : "+0 regulatory (PPWR OFF)";

        var switchingPenalty = r.Switching == 0m
            ? "+0 switching (incumbent)"
            : $"+{r.Switching.ToString("N0", DanishCulture)} switching";

        var moqPenalty = r.Moq == 0m
            ? "+0 MOQ risk"
            : $"+{r.Moq.ToString("N0", DanishCulture)} MOQ risk";

        var compliancePenalty = r.IsCompliant
            ? "Compliance: OK"
            : $"Penalty: missing weight (+{(MissingWeightPenaltyPct * 100m).ToString("0", InvariantCulture)}% of commercial)";

        var penaltyText = $"PPWR/EPR: {ppwrPenalty}; {compliancePenalty}; Switching: {switchingPenalty}; MOQ: {moqPenalty}";

        var assumptionText = volume <= 0m
            ? "0 volume detected => fixed costs dominate total TCO."
            : "Volume > 0 => per-unit spend drives total.";

        return
            $"What penalty? {penaltyText}. " +
            $"What assumption? {assumptionText}. " +
            $"{weights}. " +
            $"Price Index: {priceIndex}. " +
            $"Pillar scores: Tech {r.TechScore.ToString("0.0", InvariantCulture)}, Reg {r.RegScore.ToString("0.0", InvariantCulture)}. " +
            $"CTR (weighted): {decimal.Round(finalCtrScore, 0, MidpointRounding.AwayFromZero).ToString("0", InvariantCulture)}. " +
            $"TCO: {tco} kr.";
    }
}

