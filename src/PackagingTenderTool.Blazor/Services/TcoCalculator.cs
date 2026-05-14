using Microsoft.Extensions.Options;
using PackagingTenderTool.Blazor.Models;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Blazor.Services;

public sealed class TcoCalculator : ITcoCalculator
{
    private readonly IOptionsSnapshot<TcoSettings> options;
    private readonly IScenarioStateService scenarioState;

    public TcoCalculator(IOptionsSnapshot<TcoSettings> options, IScenarioStateService scenarioState)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.scenarioState = scenarioState ?? throw new ArgumentNullException(nameof(scenarioState));
    }

    public TcoResult Calculate(SupplierOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);

        var settings = options.Value;

        // Baseline weights come from appsettings via TcoSettings; ScenarioStateService holds the active overrides.
        // If ScenarioStateService hasn't been initialized yet, fall back to appsettings.
        var multiplier = scenarioState.ActiveWeights.Count == 0
            ? settings.GetMultiplier(offer.MaterialClass)
            : scenarioState.GetActiveMultiplier(offer.MaterialClass);

        var commercial = Math.Max(0m, offer.BasePrice);
        var technical = Math.Max(0m, offer.TechnicalFit);
        var switching = Math.Max(0m, offer.SwitchingCost);
        var regulatory = commercial * multiplier;

        var commercialR = decimal.Round(commercial, 2, MidpointRounding.AwayFromZero);
        var technicalR = decimal.Round(technical, 2, MidpointRounding.AwayFromZero);
        var switchingR = decimal.Round(switching, 2, MidpointRounding.AwayFromZero);
        var regulatoryR = decimal.Round(regulatory, 2, MidpointRounding.AwayFromZero);

        var ppwr = PpwrRiskEvaluator.Evaluate(commercialR, offer.PpwrGrade);
        var total = commercialR + technicalR + switchingR + regulatoryR + ppwr.PenaltyAmount;
        var totalR = decimal.Round(total, 2, MidpointRounding.AwayFromZero);

        return new TcoResult(
            Commercial: commercialR,
            Regulatory: regulatoryR,
            Technical: technicalR,
            Switching: switchingR,
            Total: totalR)
        {
            PpwrRiskPenalty = ppwr.PenaltyAmount,
            PpwrGrade = ppwr.NormalizedGrade,
            MarketAccessRisk2030 = ppwr.MarketAccessRisk2030,
            MarketAccessRiskNow = ppwr.MarketAccessRiskNow,
            PpwrRiskBreakdown = ppwr.Breakdown
        };
    }

    public TcoDecisionOutput CalculateDecision(SupplierOffer offer)
    {
        var actual = Calculate(offer);
        var w = scenarioState.StrategicWeights;

        var weightedTotal =
            (actual.Commercial * w.Commercial) +
            (actual.Technical * w.Technical) +
            (actual.Switching * w.Switching) +
            (actual.Regulatory * w.Regulatory) +
            (actual.PpwrRiskPenalty * w.Regulatory);

        // Index to avoid presenting this as currency.
        // 100 = same as actual total; >100 means "worse" under strategic weights; <100 means "better".
        var decisionIndex = actual.Total == 0m
            ? 0m
            : (weightedTotal / actual.Total) * 100m;

        return new TcoDecisionOutput(
            Actual: actual,
            WeightedDecisionScore: decimal.Round(weightedTotal, 2, MidpointRounding.AwayFromZero),
            DecisionScoreIndex: decimal.Round(decisionIndex, 1, MidpointRounding.AwayFromZero));
    }
}

