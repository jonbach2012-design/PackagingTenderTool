namespace PackagingTenderTool.Blazor.Models;

public sealed record StrategicWeights(
    decimal Commercial,
    decimal Technical,
    decimal Switching,
    decimal Regulatory)
{
    public static StrategicWeights Default => new(1.0m, 1.0m, 1.0m, 1.0m);
}

