using PackagingTenderTool.Blazor.Models;

namespace PackagingTenderTool.Blazor.Services;

public interface IScenarioStateService
{
    event Action? OnChange;

    IReadOnlyDictionary<string, decimal> BaselineWeights { get; }
    IReadOnlyDictionary<string, decimal> ActiveWeights { get; }
    StrategicWeights StrategicWeights { get; }

    void InitializeBaseline(IReadOnlyDictionary<string, decimal> weights);
    decimal GetActiveMultiplier(string materialClass);
    decimal GetBaselineMultiplier(string materialClass);
    void SetActiveWeights(IReadOnlyDictionary<string, decimal> weights);
    void SetActiveWeight(string materialClass, decimal multiplier);
    void ResetToBaseline();
    void SetStrategicWeights(StrategicWeights weights);
    void SetStrategicWeight(string pillar, decimal value);
    void BeginUpdate();
    void EndUpdate();
}

