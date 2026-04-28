using PackagingTenderTool.Blazor.Models;

namespace PackagingTenderTool.Blazor.Services;

public sealed class ScenarioStateService : IScenarioStateService
{
    private readonly Dictionary<string, decimal> baselineWeights = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> activeWeights = new(StringComparer.OrdinalIgnoreCase);
    private StrategicWeights strategicWeights = StrategicWeights.Default;

    public event Action? OnChange;

    private int updateNesting;

    public IReadOnlyDictionary<string, decimal> BaselineWeights => baselineWeights;
    public IReadOnlyDictionary<string, decimal> ActiveWeights => activeWeights;
    public StrategicWeights StrategicWeights => strategicWeights;

    public void InitializeBaseline(IReadOnlyDictionary<string, decimal> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);

        BeginUpdate();
        try
        {
            baselineWeights.Clear();
            foreach (var kvp in weights)
                baselineWeights[kvp.Key] = kvp.Value;

            // First init also seeds active.
            if (activeWeights.Count == 0)
            {
                foreach (var kvp in baselineWeights)
                    activeWeights[kvp.Key] = kvp.Value;
            }
        }
        finally
        {
            EndUpdate();
        }
    }

    public decimal GetActiveMultiplier(string materialClass)
    {
        if (string.IsNullOrWhiteSpace(materialClass))
            return 0m;

        return activeWeights.TryGetValue(materialClass.Trim(), out var v) ? v : 0m;
    }

    public decimal GetBaselineMultiplier(string materialClass)
    {
        if (string.IsNullOrWhiteSpace(materialClass))
            return 0m;

        return baselineWeights.TryGetValue(materialClass.Trim(), out var v) ? v : 0m;
    }

    public void SetActiveWeights(IReadOnlyDictionary<string, decimal> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);

        BeginUpdate();
        try
        {
            activeWeights.Clear();
            foreach (var kvp in weights)
                activeWeights[kvp.Key] = kvp.Value;
        }
        finally
        {
            EndUpdate();
        }
    }

    public void SetActiveWeight(string materialClass, decimal multiplier)
    {
        if (string.IsNullOrWhiteSpace(materialClass))
            return;

        activeWeights[materialClass.Trim()] = multiplier;
        NotifyChanged();
    }

    public void ResetToBaseline()
    {
        BeginUpdate();
        try
        {
            activeWeights.Clear();
            foreach (var kvp in baselineWeights)
                activeWeights[kvp.Key] = kvp.Value;

            strategicWeights = StrategicWeights.Default;
        }
        finally
        {
            EndUpdate();
        }
    }

    public void SetStrategicWeights(StrategicWeights weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        strategicWeights = weights;
        NotifyChanged();
    }

    public void SetStrategicWeight(string pillar, decimal value)
    {
        value = Math.Clamp(value, 0.5m, 2.0m);

        strategicWeights = pillar switch
        {
            "Commercial" => strategicWeights with { Commercial = value },
            "Technical" => strategicWeights with { Technical = value },
            "Switching" => strategicWeights with { Switching = value },
            "Regulatory" => strategicWeights with { Regulatory = value },
            _ => strategicWeights
        };

        NotifyChanged();
    }

    public void BeginUpdate() => updateNesting++;

    public void EndUpdate()
    {
        if (updateNesting <= 0)
        {
            updateNesting = 0;
            NotifyChanged();
            return;
        }

        updateNesting--;
        if (updateNesting == 0)
            NotifyChanged();
    }

    private void NotifyChanged()
    {
        if (updateNesting > 0)
            return;

        OnChange?.Invoke();
    }
}

