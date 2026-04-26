using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class RegulatoryService
{
    private readonly IEprFeeService eprFeeService;

    public RegulatoryService(IEprFeeService eprFeeService)
    {
        this.eprFeeService = eprFeeService ?? throw new ArgumentNullException(nameof(eprFeeService));
    }

    public decimal CalculateLabelsRegulatoryCost(
        string countryCode,
        decimal labelWeightGramsPerUnit,
        decimal quantityUnits,
        RecyclingGrade? recyclingGrade,
        decimal? recycledContentPercent,
        bool applyPpwr2030Scenario)
    {
        if (quantityUnits <= 0m || labelWeightGramsPerUnit <= 0m)
            return 0m;

        // Weight for the tender volume in kg.
        var weightKg = (labelWeightGramsPerUnit / 1000m) * quantityUnits;

        // Category “Labels” is already supported by the existing EprFeeService placeholder set.
        var fee = eprFeeService.TryCalculateFee(countryCode, "Labels", weightKg, out var baseFee, out _)
            ? baseFee
            : 0m;

        if (!applyPpwr2030Scenario)
            return fee;

        // 2030 PPWR what-if: penalize low recycled content or poor grade.
        var lowRecycled = recycledContentPercent is not null && recycledContentPercent.Value < 30m;
        var poorGrade = recyclingGrade is RecyclingGrade.D or RecyclingGrade.E;

        if (lowRecycled || poorGrade)
        {
            fee *= 1.5m;
        }

        return decimal.Round(fee, 2, MidpointRounding.AwayFromZero);
    }

    public decimal CalculateEpr2026ForecastFee(
        string countryCode,
        decimal labelWeightGramsPerUnit,
        decimal quantityUnits)
    {
        if (quantityUnits <= 0m || labelWeightGramsPerUnit <= 0m)
            return 0m;

        var weightKg = (labelWeightGramsPerUnit / 1000m) * quantityUnits;
        return eprFeeService.TryCalculateFee(countryCode, "Labels", weightKg, out var baseFee, out _)
            ? decimal.Round(baseFee, 2, MidpointRounding.AwayFromZero)
            : 0m;
    }

    public decimal CalculateRegulatoryRiskDelta(
        string countryCode,
        decimal labelWeightGramsPerUnit,
        decimal quantityUnits,
        RecyclingGrade? selectedGrade,
        bool apply2030Scenario)
    {
        var baseFee = CalculateEpr2026ForecastFee(countryCode, labelWeightGramsPerUnit, quantityUnits);
        if (!apply2030Scenario)
            return 0m;

        var m = selectedGrade switch
        {
            RecyclingGrade.A => 1.0m,
            RecyclingGrade.B => 1.0m,
            RecyclingGrade.C => 1.5m,
            RecyclingGrade.D => 2.0m,
            RecyclingGrade.E => 3.0m,
            _ => 1.5m
        };

        var adjusted = baseFee * m;
        return decimal.Round(Math.Max(0m, adjusted - baseFee), 2, MidpointRounding.AwayFromZero);
    }
}

