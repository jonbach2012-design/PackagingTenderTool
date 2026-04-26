using System.Globalization;
using PackagingTenderTool.Blazor.Models.LabelTender;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Blazor.Services;

public sealed class TcoEngineService
{
    private readonly RegulatoryService regulatory;
    private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public TcoEngineService(RegulatoryService regulatory)
    {
        this.regulatory = regulatory ?? throw new ArgumentNullException(nameof(regulatory));
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
            var volume = s.QuantityLabels <= 0 ? 0m : s.QuantityLabels;
            var commercial = volume * s.Price;

            var country = string.IsNullOrWhiteSpace(s.Country) ? "DK" : s.Country;
            var grade = session.GetSupplierRecyclabilityGrade(s.SupplierId);

            var epr = 0m;
            if (session.ApplyPpwr2030Scenario)
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

            var switching = s.SupplierId.Equals(session.IncumbentSupplierId, StringComparison.OrdinalIgnoreCase)
                ? 0m
                : session.GetStartupCost(s.SupplierId) + (session.GetMonthlySupportCost(s.SupplierId) * 12m);

            var moq = commercial * (session.GetSupplierMoqPenaltyPct(s.SupplierId) / 100m);

            // Technical score adjustment (lead time penalty).
            var tech = s.TechnicalScore;
            if (s.LeadTimeWeeks > 4)
                tech = Math.Max(0m, tech - ((s.LeadTimeWeeks - 4) * 5m));

            var regScore = s.RegulatoryScore;
            var total = commercial + epr + switching + moq;

            results.Add(new LabelTenderDashboardDto
            {
                SupplierName = s.SupplierName,
                Commercial = decimal.Round(commercial, 0, MidpointRounding.AwayFromZero),
                Epr = decimal.Round(epr, 0, MidpointRounding.AwayFromZero),
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
                CalculationBreakdown = string.Empty
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
                CalculationBreakdown = breakdown
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
                CalculationBreakdown = r.CalculationBreakdown
            };
        }

        return results;
    }

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
        var weights = $"W: Price {session.Commercial:0} / Tech {session.Technical:0} / Reg {session.Regulatory:0}";

        var ppwrText = session.ApplyPpwr2030Scenario
            ? $"+{r.Epr.ToString("N0", DanishCulture)} regulatory (PPWR ON, grade {grade})"
            : "+0 regulatory (PPWR OFF)";

        var switchingText = r.Switching == 0m
            ? "+0 switching (incumbent)"
            : $"+{r.Switching.ToString("N0", DanishCulture)} switching";

        var moqText = r.Moq == 0m
            ? "+0 MOQ risk"
            : $"+{r.Moq.ToString("N0", DanishCulture)} MOQ risk";

        var assumption = volume <= 0m
            ? "Assumption: 0 volume ⇒ commercial=0 (fixed costs dominate if non-incumbent)."
            : string.Empty;

        return
            $"Why this score? Price Index {priceIndex} with {weights}. " +
            $"TCO: {tco} kr. " +
            $"Penalties: {ppwrText}, {switchingText}, {moqText}. " +
            $"Pillar scores: Tech {r.TechScore.ToString("0.0", InvariantCulture)}, Reg {r.RegScore.ToString("0.0", InvariantCulture)}. " +
            $"{assumption} " +
            $"CTR (weighted): {decimal.Round(finalCtrScore, 0, MidpointRounding.AwayFromZero).ToString("0", InvariantCulture)}.";
    }
}

