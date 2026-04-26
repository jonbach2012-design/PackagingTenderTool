using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Blazor;

/// <summary>
/// Packaging profile, linked pillar weights (always sum 100), and advanced constraint limits.
/// </summary>
public sealed class PackagingProfileSession
{
    public const string ProfileLabels = "Labels";

    public string? SelectedProfile { get; private set; }

    public int Commercial { get; private set; } = 30;

    public int Technical { get; private set; } = 30;

    public int Regulatory { get; private set; } = 40;

    public decimal MaxCo2Impact { get; private set; } = 2.25m;

    public decimal MaxLeadTimeDays { get; private set; } = 30m;

    public bool ApplyPpwr2030Scenario { get; private set; }

    public string IncumbentSupplierId { get; private set; } = "incumbent";

    public decimal GetStartupCost(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return 0m;

        if (string.Equals(supplierId, IncumbentSupplierId, StringComparison.OrdinalIgnoreCase))
            return 0m;

        return _startupCosts.TryGetValue(supplierId.Trim(), out var v) ? v : 0m;
    }

    public void SetStartupCost(string supplierId, decimal value)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return;

        if (string.Equals(supplierId, IncumbentSupplierId, StringComparison.OrdinalIgnoreCase))
            value = 0m;

        _startupCosts[supplierId.Trim()] = Math.Max(0m, value);
        Notify();
    }

    public decimal GetMonthlySupportCost(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return 0m;

        if (string.Equals(supplierId, IncumbentSupplierId, StringComparison.OrdinalIgnoreCase))
            return 0m;

        return _monthlySupportCosts.TryGetValue(supplierId.Trim(), out var v) ? v : 0m;
    }

    public void SetMonthlySupportCost(string supplierId, decimal value)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return;

        if (string.Equals(supplierId, IncumbentSupplierId, StringComparison.OrdinalIgnoreCase))
            value = 0m;

        _monthlySupportCosts[supplierId.Trim()] = Math.Max(0m, value);
        Notify();
    }

    public string ActiveSupplierId { get; private set; } = string.Empty;

    public void SetActiveSupplier(string supplierId)
    {
        ActiveSupplierId = supplierId?.Trim() ?? string.Empty;
        Notify();
    }

    public RecyclingGrade GetSupplierRecyclabilityGrade(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return RecyclingGrade.C;

        return _supplierGrades.TryGetValue(supplierId.Trim(), out var g) ? g : RecyclingGrade.C;
    }

    public void SetSupplierRecyclabilityGrade(string supplierId, RecyclingGrade grade)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return;

        _supplierGrades[supplierId.Trim()] = grade;
        Notify();
    }

    public decimal GetSupplierMoqPenaltyPct(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return 5m;

        return _supplierMoqPenaltyPct.TryGetValue(supplierId.Trim(), out var v) ? v : 5m;
    }

    public void SetSupplierMoqPenaltyPct(string supplierId, decimal pct)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return;

        pct = Math.Clamp(pct, 0m, 25m);
        _supplierMoqPenaltyPct[supplierId.Trim()] = pct;
        Notify();
    }

    public decimal GetSwitchingCost(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return 0m;

        if (string.Equals(supplierId, IncumbentSupplierId, StringComparison.OrdinalIgnoreCase))
            return 0m;

        return _switchingCosts.TryGetValue(supplierId.Trim(), out var v) ? v : 0m;
    }

    public void SetSwitchingCost(string supplierId, decimal cost)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return;

        // Incumbent is always 0 (category manager rule).
        if (string.Equals(supplierId, IncumbentSupplierId, StringComparison.OrdinalIgnoreCase))
            cost = 0m;

        cost = Math.Max(0m, cost);
        _switchingCosts[supplierId.Trim()] = cost;
        Notify();
    }

    public void SeedSupplierDefaults(
        string supplierId,
        RecyclingGrade grade,
        decimal startupCost,
        decimal monthlySupportCost,
        decimal moqPenaltyPct)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            return;

        var id = supplierId.Trim();

        if (!_supplierGrades.ContainsKey(id))
            _supplierGrades[id] = grade;

        if (!_startupCosts.ContainsKey(id))
            _startupCosts[id] = string.Equals(id, IncumbentSupplierId, StringComparison.OrdinalIgnoreCase) ? 0m : Math.Max(0m, startupCost);

        if (!_monthlySupportCosts.ContainsKey(id))
            _monthlySupportCosts[id] = string.Equals(id, IncumbentSupplierId, StringComparison.OrdinalIgnoreCase) ? 0m : Math.Max(0m, monthlySupportCost);

        if (!_supplierMoqPenaltyPct.ContainsKey(id))
            _supplierMoqPenaltyPct[id] = Math.Clamp(moqPenaltyPct, 0m, 25m);
    }

    /// <summary>
    /// Selected supplier ids (used to filter cockpit KPIs and the grid).
    /// Defaults to "all" for the active profile.
    /// </summary>
    public IReadOnlySet<string> SelectedSuppliers => _selectedSupplierIds;

    /// <summary>Always 100 when weights are maintained via linked sliders.</summary>
    public int PillarSum => Commercial + Technical + Regulatory;

    /// <summary>
    /// Pending/clarification count surfaced in the sidebar badge.
    /// Set by the cockpit page after running the Digital Auditor + workflow filters.
    /// </summary>
    public int PendingClarificationsCount { get; private set; }

    public event Action? Changed;

    private readonly HashSet<string> _selectedSupplierIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> _switchingCosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> _startupCosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> _monthlySupportCosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RecyclingGrade> _supplierGrades = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> _supplierMoqPenaltyPct = new(StringComparer.OrdinalIgnoreCase);

    public void SelectLabels()
    {
        if (SelectedProfile is not null)
        {
            return;
        }

        SelectedProfile = ProfileLabels;
        _selectedSupplierIds.Clear();
        Notify();
    }

    public void ReplaceSelectedSuppliers(IEnumerable<string> supplierIds)
    {
        ArgumentNullException.ThrowIfNull(supplierIds);
        _selectedSupplierIds.Clear();
        foreach (var n in supplierIds.Where(static s => !string.IsNullOrWhiteSpace(s)))
        {
            _selectedSupplierIds.Add(n.Trim());
        }
        Notify();
    }

    public void SetSupplierSelected(string supplierId, bool isSelected)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return;
        }

        var changed = isSelected
            ? _selectedSupplierIds.Add(supplierId.Trim())
            : _selectedSupplierIds.Remove(supplierId.Trim());

        if (changed)
        {
            Notify();
        }
    }

    public void SetCommercial(int value) => ApplyPrimaryWeight(value, which: 0);

    public void SetTechnical(int value) => ApplyPrimaryWeight(value, which: 1);

    public void SetRegulatory(int value) => ApplyPrimaryWeight(value, which: 2);

    /// <summary>Sets one pillar to <paramref name="value"/> and splits the remainder across the other two by prior ratio.</summary>
    private void ApplyPrimaryWeight(int value, int which)
    {
        value = Math.Clamp(value, 0, 100);
        var remainder = 100 - value;

        switch (which)
        {
            case 0:
                Commercial = value;
                (Technical, Regulatory) = DistributeRemaining(remainder, Technical, Regulatory);
                break;
            case 1:
                Technical = value;
                (Commercial, Regulatory) = DistributeRemaining(remainder, Commercial, Regulatory);
                break;
            default:
                Regulatory = value;
                (Commercial, Technical) = DistributeRemaining(remainder, Commercial, Technical);
                break;
        }

        Notify();
    }

    /// <summary>Split <paramref name="remainder"/> across two pillars, proportional to their previous values.</summary>
    private static (int first, int second) DistributeRemaining(int remainder, int prevFirst, int prevSecond)
    {
        remainder = Math.Clamp(remainder, 0, 100);
        var denom = prevFirst + prevSecond;
        if (denom <= 0)
        {
            var half = remainder / 2;
            return (half, remainder - half);
        }

        var first = (int)Math.Round(remainder * (prevFirst / (decimal)denom));
        first = Math.Clamp(first, 0, remainder);
        var second = remainder - first;
        return (first, second);
    }

    public void SetMaxCo2Impact(decimal value)
    {
        MaxCo2Impact = Math.Clamp(
            value,
            LabelTenderAdvancedConstraints.Co2SliderMin,
            LabelTenderAdvancedConstraints.Co2SliderMax);
        Notify();
    }

    public void SetMaxLeadTimeDays(decimal value)
    {
        MaxLeadTimeDays = Math.Clamp(
            value,
            LabelTenderAdvancedConstraints.LeadSliderMin,
            LabelTenderAdvancedConstraints.LeadSliderMax);
        Notify();
    }

    public void SetApplyPpwr2030Scenario(bool value)
    {
        ApplyPpwr2030Scenario = value;
        Notify();
    }

    public void SetPendingClarificationsCount(int value)
    {
        PendingClarificationsCount = Math.Max(0, value);
        Notify();
    }

    private void Notify() => Changed?.Invoke();
}
