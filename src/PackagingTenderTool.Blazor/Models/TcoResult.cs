namespace PackagingTenderTool.Blazor.Models;

public sealed record TcoResult(
    decimal Commercial,
    decimal Regulatory,
    decimal Technical,
    decimal Switching,
    decimal Total);

