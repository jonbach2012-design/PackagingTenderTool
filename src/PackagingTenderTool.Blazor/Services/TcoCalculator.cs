using Microsoft.Extensions.Options;
using PackagingTenderTool.Blazor.Models;

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

        var total = commercial + technical + switching + regulatory;
        return new TcoResult(
            Commercial: decimal.Round(commercial, 2, MidpointRounding.AwayFromZero),
            Regulatory: decimal.Round(regulatory, 2, MidpointRounding.AwayFromZero),
            Technical: decimal.Round(technical, 2, MidpointRounding.AwayFromZero),
            Switching: decimal.Round(switching, 2, MidpointRounding.AwayFromZero),
            Total: decimal.Round(total, 2, MidpointRounding.AwayFromZero));
    }

    public TcoDecisionOutput CalculateDecision(SupplierOffer offer)
    {
        var actual = Calculate(offer);
        var w = scenarioState.StrategicWeights;

        var weightedTotal =
            (actual.Commercial * w.Commercial) +
            (actual.Technical * w.Technical) +
            (actual.Switching * w.Switching) +
            (actual.Regulatory * w.Regulatory);

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

