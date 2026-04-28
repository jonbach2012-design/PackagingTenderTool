using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public interface IRegulatoryService
{
    decimal CalculateLabelsRegulatoryCost(
        string countryCode,
        decimal labelWeightGramsPerUnit,
        decimal quantityUnits,
        RecyclingGrade? recyclingGrade,
        decimal? recycledContentPercent,
        bool applyPpwr2030Scenario);

    decimal CalculateEpr2026ForecastFee(
        string countryCode,
        decimal labelWeightGramsPerUnit,
        decimal quantityUnits);

    decimal CalculateRegulatoryRiskDelta(
        string countryCode,
        decimal labelWeightGramsPerUnit,
        decimal quantityUnits,
        RecyclingGrade? selectedGrade,
        bool apply2030Scenario);
}

