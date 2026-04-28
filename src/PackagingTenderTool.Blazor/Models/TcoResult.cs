namespace PackagingTenderTool.Blazor.Models;

public sealed record TcoResult(
    decimal Commercial,
    decimal Regulatory,
    decimal Technical,
    decimal Switching,
    decimal Total)
{
    public string TechnicalSummary { get; set; } = string.Empty;
}

