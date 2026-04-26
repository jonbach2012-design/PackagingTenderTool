namespace PackagingTenderTool.Blazor.Models;

public sealed record TenderLine(
    string LineItem,
    string Supplier,
    string Site,
    string Country,
    string Category,
    decimal BasePrice);

