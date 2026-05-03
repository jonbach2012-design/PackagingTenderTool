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

    private const string UnknownToken = "Unknown";

    public string UnknownDimensionValue => UnknownToken;

    public TcoEngineService(IRegulatoryService regulatory)
    {
        this.regulatory = regulatory ?? throw new ArgumentNullException(nameof(regulatory));
    }

    public string NormalizeTenderDimension(string? value) =>
        string.IsNullOrWhiteSpace(value) ? UnknownToken : value.Trim();

    public TenderFilterOptions GetTenderFilterOptions(
        IReadOnlyList<SupplierModel> allSuppliers,
        string? selectedCountryFilter)
    {
        ArgumentNullException.ThrowIfNull(allSuppliers);

        var countryFilter = string.IsNullOrWhiteSpace(selectedCountryFilter)
            ? string.Empty
            : selectedCountryFilter.Trim();

        var countrySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var siteSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var materialSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var adhesiveSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in allSuppliers)
        {
            foreach (var c in EnumerateCountryTokens(s))
                countrySet.Add(c);

            var sites = s.Sites ?? Array.Empty<string>();
            if (sites.Count == 0)
                siteSet.Add(UnknownToken);
            else
            {
                foreach (var site in sites)
                    siteSet.Add(NormalizeTenderDimension(site));
            }

            materialSet.Add(NormalizeTenderDimension(s.Material));
            adhesiveSet.Add(NormalizeTenderDimension(s.Adhesive));
        }

        var sitesForSelection = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(countryFilter))
        {
            foreach (var x in siteSet)
                sitesForSelection.Add(x);
        }
        else
        {
            foreach (var s in allSuppliers)
            {
                if (!SupplierMatchesCountryFilter(s, countryFilter))
                    continue;

                var sites = s.Sites ?? Array.Empty<string>();
                if (sites.Count == 0)
                    sitesForSelection.Add(UnknownToken);
                else
                {
                    foreach (var site in sites)
                        sitesForSelection.Add(NormalizeTenderDimension(site));
                }
            }
        }

        return new TenderFilterOptions
        {
            Countries = OrderFilterValues(countrySet),
            Sites = OrderFilterValues(sitesForSelection),
            Materials = OrderFilterValues(materialSet),
            Adhesives = OrderFilterValues(adhesiveSet)
        };
    }

    public bool SupplierMatchesTenderFilters(
        SupplierModel s,
        string countryFilter,
        string siteFilter,
        string materialFilter,
        string adhesiveFilter)
    {
        ArgumentNullException.ThrowIfNull(s);

        if (!string.IsNullOrWhiteSpace(countryFilter)
            && !EnumerateCountryTokens(s).Any(t => t.Equals(countryFilter.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(siteFilter))
        {
            var f = siteFilter.Trim();
            var sites = s.Sites ?? Array.Empty<string>();
            if (sites.Count == 0)
            {
                if (!f.Equals(UnknownToken, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else if (!sites.Any(x => NormalizeTenderDimension(x).Equals(f, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(materialFilter))
        {
            if (!NormalizeTenderDimension(s.Material).Equals(materialFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(adhesiveFilter))
        {
            if (!NormalizeTenderDimension(s.Adhesive).Equals(adhesiveFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    public LabelTenderDashboardDto CalculateResult(PackagingProfileSession session, SupplierModel supplier)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(supplier);

        return GetResults(session, [supplier]).First();
    }

    public IReadOnlyList<LabelTenderDashboardDto> GetResults(PackagingProfileSession session, IReadOnlyList<SupplierModel> suppliers) =>
        BuildRankedResults(session, suppliers, session.Commercial, session.Technical, session.Regulatory);

    public string? GetWeightedLeaderSupplierId(
        PackagingProfileSession session,
        IReadOnlyList<SupplierModel> suppliers,
        int commercialPct,
        int technicalPct,
        int regulatoryPct)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(suppliers);

        var ranked = BuildRankedResults(session, suppliers, commercialPct, technicalPct, regulatoryPct);
        return ranked.Count == 0 ? null : ranked[0].SupplierId;
    }

    private List<LabelTenderDashboardDto> BuildRankedResults(
        PackagingProfileSession session,
        IReadOnlyList<SupplierModel> suppliers,
        int commercialPct,
        int technicalPct,
        int regulatoryPct)
    {
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
                    missingWeightPenalty = decimal.Round(commercial * MissingWeightPenaltyPct, 2, MidpointRounding.AwayFromZero);
                }
            }

            var switching = supplierId.Equals(session.IncumbentSupplierId, StringComparison.OrdinalIgnoreCase)
                ? 0m
                : session.GetStartupCost(supplierId) + (session.GetMonthlySupportCost(supplierId) * 12m);

            var moq = commercial * (session.GetSupplierMoqPenaltyPct(supplierId) / 100m);

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

        var minTco = results.Min(r => r.Total);
        if (minTco <= 0m)
            minTco = 1m;

        for (var i = 0; i < results.Count; i++)
        {
            var r = results[i];
            var priceScore = r.Total <= 0m ? 0m : Math.Clamp((minTco / r.Total) * 100m, 0m, 100m);
            var final = ((priceScore * commercialPct) + (r.TechScore * technicalPct) + (r.RegScore * regulatoryPct)) / 100m;
            final = Math.Clamp(final, 0m, 100m);

            var supplier = suppliers[i];
            var supplierId = supplier.SupplierId ?? string.Empty;
            var supplierVolume = supplier.QuantityLabels;
            var breakdown = BuildBreakdown(
                r,
                priceScore,
                final,
                session,
                grade: string.IsNullOrWhiteSpace(supplierId) ? RecyclingGrade.C : session.GetSupplierRecyclabilityGrade(supplierId),
                volume: supplierVolume,
                commercialPct,
                technicalPct,
                regulatoryPct);

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
        decimal volume,
        int weightCommercialPct,
        int weightTechnicalPct,
        int weightRegulatoryPct)
    {
        var priceIndex = decimal.Round(priceScore, 0, MidpointRounding.AwayFromZero).ToString("0", InvariantCulture);
        var tco = r.Total.ToString("N0", DanishCulture);
        var weights = $"Weights: Price {weightCommercialPct:0}% / Tech {weightTechnicalPct:0}% / Reg {weightRegulatoryPct:0}%";

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

    public string GetRecommendationNarrative(
        PackagingProfileSession session,
        IReadOnlyList<LabelTenderDashboardDto> results)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(results);

        return BuildRecommendationCopy(session, results).narrative;
    }

    public TenderDecisionInsight GetDecisionInsight(
        PackagingProfileSession session,
        IReadOnlyList<LabelTenderDashboardDto> results,
        IReadOnlyList<SupplierModel> suppliersWeightedInResults)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(suppliersWeightedInResults);

        var copy = BuildRecommendationCopy(session, results);
        var rec = copy.recommended;
        var low = copy.lowestCommercial;
        var best = copy.bestTco;

        var confidence = AnalyzeWeightSensitivity(session, suppliersWeightedInResults);

        if (rec is null || results.Count == 0)
        {
            return new TenderDecisionInsight
            {
                Narrative = "No suppliers in scope — import data or adjust filters to compute a recommendation.",
                RecommendedSupplierId = string.Empty,
                RecommendedSupplierName = "—",
                RecommendedWeightedScore = 0m,
                WeightedShareCommercial = 0m,
                WeightedShareTechnical = 0m,
                WeightedShareRegulatory = 0m,
                LowestPriceSupplierId = string.Empty,
                LowestPriceSupplierName = "—",
                LowestCommercialSpend = 0m,
                BestTcoSupplierId = string.Empty,
                BestTcoSupplierName = "—",
                BestTcoTotal = 0m,
                RecommendedMatchesLowestPrice = false,
                RecommendedMatchesBestTco = false,
                PrimaryValueDriver = "—",
                ConfidenceLevel = DecisionConfidence.High,
                IsSensitiveDecision = false
            };
        }

        var final = rec.FinalCtrScore <= 0m ? 1m : rec.FinalCtrScore;
        var partC = rec.PriceScore * session.Commercial / 100m;
        var partT = rec.TechScore * session.Technical / 100m;
        var partR = rec.RegScore * session.Regulatory / 100m;

        return new TenderDecisionInsight
        {
            Narrative = copy.narrative,
            RecommendedSupplierId = rec.SupplierId,
            RecommendedSupplierName = rec.SupplierName,
            RecommendedWeightedScore = decimal.Round(rec.FinalCtrScore, 1, MidpointRounding.AwayFromZero),
            WeightedShareCommercial = decimal.Round(partC / final * 100m, 1, MidpointRounding.AwayFromZero),
            WeightedShareTechnical = decimal.Round(partT / final * 100m, 1, MidpointRounding.AwayFromZero),
            WeightedShareRegulatory = decimal.Round(partR / final * 100m, 1, MidpointRounding.AwayFromZero),
            LowestPriceSupplierId = low?.SupplierId ?? string.Empty,
            LowestPriceSupplierName = low?.SupplierName ?? "—",
            LowestCommercialSpend = low?.Commercial ?? 0m,
            BestTcoSupplierId = best?.SupplierId ?? string.Empty,
            BestTcoSupplierName = best?.SupplierName ?? "—",
            BestTcoTotal = best?.Total ?? 0m,
            RecommendedMatchesLowestPrice = low is not null
                && string.Equals(rec.SupplierId, low.SupplierId, StringComparison.OrdinalIgnoreCase),
            RecommendedMatchesBestTco = best is not null
                && string.Equals(rec.SupplierId, best.SupplierId, StringComparison.OrdinalIgnoreCase),
            PrimaryValueDriver = copy.primaryDriver,
            ConfidenceLevel = confidence.level,
            IsSensitiveDecision = confidence.isSensitive
        };
    }

    private (DecisionConfidence level, bool isSensitive) AnalyzeWeightSensitivity(
        PackagingProfileSession session,
        IReadOnlyList<SupplierModel> suppliers)
    {
        if (suppliers.Count < 2)
            return (DecisionConfidence.High, false);

        var baseId = GetWeightedLeaderSupplierId(session, suppliers, session.Commercial, session.Technical, session.Regulatory);
        if (string.IsNullOrWhiteSpace(baseId))
            return (DecisionConfidence.High, false);

        var c0 = session.Commercial;
        var t0 = session.Technical;
        var r0 = session.Regulatory;

        foreach (var (c, t, r) in EnumerateWeightPerturbations(c0, t0, r0))
        {
            var alt = GetWeightedLeaderSupplierId(session, suppliers, c, t, r);
            if (string.IsNullOrWhiteSpace(alt))
                continue;

            if (!alt.Equals(baseId, StringComparison.OrdinalIgnoreCase))
                return (DecisionConfidence.Sensitive, true);
        }

        return (DecisionConfidence.High, false);
    }

    private static IEnumerable<(int c, int t, int r)> EnumerateWeightPerturbations(int c0, int t0, int r0)
    {
        for (var pillar = 0; pillar < 3; pillar++)
        {
            foreach (var delta in new[] { 5, -5 })
            {
                var p = ApplyPillarDelta(c0, t0, r0, pillar, delta);
                if (p is { } v)
                    yield return v;
            }
        }
    }

    private static (int c, int t, int r)? ApplyPillarDelta(int c0, int t0, int r0, int pillar, int delta)
    {
        if (pillar == 0)
        {
            var c = c0 + delta;
            if (c < 0 || c > 100)
                return null;
            var rem = 100 - c;
            return SplitTwo(rem, t0, r0, out var t, out var r) ? (c, t, r) : null;
        }

        if (pillar == 1)
        {
            var t = t0 + delta;
            if (t < 0 || t > 100)
                return null;
            var rem = 100 - t;
            return SplitTwo(rem, c0, r0, out var c, out var r) ? (c, t, r) : null;
        }

        if (pillar == 2)
        {
            var r = r0 + delta;
            if (r < 0 || r > 100)
                return null;
            var rem = 100 - r;
            return SplitTwo(rem, c0, t0, out var c, out var t) ? (c, t, r) : null;
        }

        return null;
    }

    private static bool SplitTwo(int remainder, int w1, int w2, out int a, out int b)
    {
        a = 0;
        b = 0;
        if (remainder < 0 || remainder > 100)
            return false;

        var sumW = w1 + w2;
        if (sumW <= 0)
        {
            a = remainder / 2;
            b = remainder - a;
            return a >= 0 && b >= 0 && a <= 100 && b <= 100;
        }

        a = (int)Math.Round(remainder * (double)w1 / sumW, MidpointRounding.AwayFromZero);
        b = remainder - a;
        if (a < 0 || b < 0 || a > 100 || b > 100)
            return false;

        if (a + b != remainder)
            b = remainder - a;

        return true;
    }

    private static bool SupplierMatchesCountryFilter(SupplierModel s, string countryFilter)
    {
        var tokens = EnumerateCountryTokens(s).ToArray();
        return tokens.Any(t => t.Equals(countryFilter, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> EnumerateCountryTokens(SupplierModel s)
    {
        yield return NormalizeCountryField(s.Country);

        var sites = s.Sites ?? Array.Empty<string>();
        foreach (var site in sites)
        {
            var code = GetCountryCodeFromSiteToken(site);
            yield return string.IsNullOrEmpty(code) ? UnknownToken : code.ToUpperInvariant();
        }
    }

    private static string NormalizeCountryField(string? country) =>
        string.IsNullOrWhiteSpace(country) ? UnknownToken : country.Trim();

    private static IReadOnlyList<string> OrderFilterValues(HashSet<string> values)
    {
        var u = UnknownToken;
        return values
            .OrderBy(x => string.Equals(x, u, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Matches Label Tender import/site conventions (e.g. DK-East → DK).</summary>
    private static string GetCountryCodeFromSiteToken(string? site)
    {
        if (string.IsNullOrWhiteSpace(site))
            return string.Empty;

        var s = site.Trim();
        var dash = s.IndexOf('-');
        var prefix = dash > 0
            ? s[..dash]
            : s.Length >= 2
                ? s[..2]
                : s;

        if (!string.IsNullOrEmpty(prefix))
            return prefix.ToUpperInvariant();

        return DeriveCountryFromSiteName(s);
    }

    private static string DeriveCountryFromSiteName(string site)
    {
        if (string.IsNullOrWhiteSpace(site))
            return string.Empty;

        var s = site.Trim();
        if (s.Contains("Jæren", StringComparison.OrdinalIgnoreCase))
            return "NO";
        if (s.Contains("Jaeren", StringComparison.OrdinalIgnoreCase))
            return "NO";
        if (s.Contains("Norway", StringComparison.OrdinalIgnoreCase) || s.Contains("Norge", StringComparison.OrdinalIgnoreCase))
            return "NO";
        if (s.Contains("Denmark", StringComparison.OrdinalIgnoreCase) || s.Contains("Danmark", StringComparison.OrdinalIgnoreCase))
            return "DK";
        if (s.Contains("Sweden", StringComparison.OrdinalIgnoreCase) || s.Contains("Sverige", StringComparison.OrdinalIgnoreCase))
            return "SE";
        if (s.Contains("Finland", StringComparison.OrdinalIgnoreCase))
            return "FI";

        return string.Empty;
    }

    private static (LabelTenderDashboardDto? recommended, LabelTenderDashboardDto? lowestCommercial, LabelTenderDashboardDto? bestTco, string narrative, string primaryDriver)
        BuildRecommendationCopy(PackagingProfileSession session, IReadOnlyList<LabelTenderDashboardDto> results)
    {
        if (results.Count == 0)
            return (null, null, null, string.Empty, "—");

        var recommended = results[0];

        var lowestCommercial = results.MinBy(r => r.Commercial);
        var bestTco = results.MinBy(r => r.Total);

        if (results.Count == 1)
        {
            var n = "Single supplier in view — weighted CTR score reflects current pillar weights and TCO inputs only.";
            return (recommended, lowestCommercial, bestTco, n, "Single option");
        }

        if (lowestCommercial is null || bestTco is null)
            return (recommended, lowestCommercial, bestTco, string.Empty, "—");

        var narrative = DescribeTradeOff(session, recommended, lowestCommercial);
        var driver = ClassifyValueDriver(session, recommended, lowestCommercial);

        return (recommended, lowestCommercial, bestTco, narrative, driver);
    }

    private static string ClassifyValueDriver(
        PackagingProfileSession session,
        LabelTenderDashboardDto recommended,
        LabelTenderDashboardDto lowestCommercial)
    {
        if (string.Equals(recommended.SupplierId, lowestCommercial.SupplierId, StringComparison.OrdinalIgnoreCase))
            return "Commercial alignment";

        var wTech =
            (recommended.TechScore - lowestCommercial.TechScore) * session.Technical / 100m;
        var wReg =
            (recommended.RegScore - lowestCommercial.RegScore) * session.Regulatory / 100m;
        var wPrice =
            (recommended.PriceScore - lowestCommercial.PriceScore) * session.Commercial / 100m;

        var absTech = Math.Abs(wTech);
        var absReg = Math.Abs(wReg);
        var absPrice = Math.Abs(wPrice);

        if (absReg >= absTech && absReg >= absPrice && wReg > 0.05m)
            return "Regulatory";

        if (absTech >= absReg && absTech >= absPrice && wTech > 0.05m)
            return "Technical";

        if (absPrice >= absTech && absPrice >= absReg && absPrice > 0.05m)
            return "Commercial index";

        return "Balanced mix";
    }

    private static string DescribeTradeOff(
        PackagingProfileSession session,
        LabelTenderDashboardDto recommended,
        LabelTenderDashboardDto lowestCommercial)
    {
        if (string.Equals(recommended.SupplierId, lowestCommercial.SupplierId, StringComparison.OrdinalIgnoreCase))
        {
            return "Recommended supplier matches the lowest commercial anchor — strategic weighted score aligns with unit economics.";
        }

        var wTech =
            (recommended.TechScore - lowestCommercial.TechScore) * session.Technical / 100m;
        var wReg =
            (recommended.RegScore - lowestCommercial.RegScore) * session.Regulatory / 100m;
        var wPrice =
            (recommended.PriceScore - lowestCommercial.PriceScore) * session.Commercial / 100m;

        if (recommended.Commercial == lowestCommercial.Commercial)
        {
            return "Commercial spend ties the anchor bid — recommendation is driven by technical/regulatory pillars under the active weights.";
        }

        var moreSpend = recommended.Commercial > lowestCommercial.Commercial;

        if (!moreSpend)
        {
            return "Recommended supplier is not the lowest commercial bid, but pillar scores under current weights still favor this option — review regulatory and technical deltas vs. the commercial anchor.";
        }

        if (wReg >= wTech && wReg > 0.05m)
            return "Regulatory advantage offsets higher commercial spend versus the lowest-price bid.";

        if (wTech > wReg && wTech > 0.05m)
            return "Technical strength offsets higher unit economics versus the lowest-price bid.";

        if (wPrice < -0.05m)
            return "Lower price index is outweighed by technical/regulatory performance under the active weight profile.";

        return "Weighted pillar mix favors the recommendation over the lowest commercial anchor — no single pillar dominates.";
    }
}
