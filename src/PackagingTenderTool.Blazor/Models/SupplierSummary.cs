namespace PackagingTenderTool.Blazor.Models;

public sealed record SupplierSummary(
    string Site,
    string Supplier,
    int Lines,
    decimal DataQualityScore,
    decimal Commercial,
    decimal Regulatory,
    decimal Technical,
    decimal Switching,
    decimal Total);

