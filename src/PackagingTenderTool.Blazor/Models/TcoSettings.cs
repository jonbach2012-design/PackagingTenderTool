namespace PackagingTenderTool.Blazor.Models;

public sealed class TcoSettings
{
    public Dictionary<string, decimal> EprMultipliers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public decimal GetMultiplier(string materialClass)
    {
        if (string.IsNullOrWhiteSpace(materialClass))
            return 0m;

        return EprMultipliers.TryGetValue(materialClass.Trim(), out var v) ? v : 0m;
    }
}

