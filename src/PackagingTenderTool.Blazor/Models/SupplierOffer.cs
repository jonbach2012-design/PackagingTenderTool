namespace PackagingTenderTool.Blazor.Models;

public sealed record SupplierOffer(
    TenderLine TenderLine,
    decimal BasePrice,
    string MaterialClass,
    decimal TechnicalFit,
    decimal SwitchingCost,
    decimal DataQualityScore);

