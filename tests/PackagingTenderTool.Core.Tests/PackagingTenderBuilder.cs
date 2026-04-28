using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Tests;

public sealed class PackagingTenderBuilder
{
    private readonly Tender tender = new();
    private readonly LabelLineItem lineItem = new();

    public PackagingTenderBuilder()
    {
        tender.Name = "Test tender";
        tender.LabelLineItems.Add(lineItem);

        // Safe deterministic defaults
        lineItem.SupplierName = "Acme Labels";
        lineItem.Spend = 100m;
        lineItem.PricePerThousand = 10m;
        lineItem.Quantity = 1_000m;
        lineItem.LabelWeightGrams = 100m;

        WithEprScheme(countryCode: "DK", category: "Labels");
    }

    public PackagingTenderBuilder WithSupplier(string supplierName)
    {
        lineItem.SupplierName = supplierName;
        return this;
    }

    public PackagingTenderBuilder WithSpend(decimal? spend)
    {
        lineItem.Spend = spend;
        return this;
    }

    public PackagingTenderBuilder WithPricePerThousand(decimal? pricePerThousand)
    {
        lineItem.PricePerThousand = pricePerThousand;
        return this;
    }

    public PackagingTenderBuilder WithPrice(decimal? unitPrice)
    {
        lineItem.Price = unitPrice;
        return this;
    }

    public PackagingTenderBuilder WithQuantity(decimal? quantity)
    {
        lineItem.Quantity = quantity;
        return this;
    }

    public PackagingTenderBuilder WithEprScheme(string countryCode, string category)
    {
        lineItem.EprSchemes.Clear();
        lineItem.EprSchemes.Add(new EprSchemeInfo { CountryCode = countryCode, Category = category });
        return this;
    }

    public PackagingTenderBuilder WithMissingWeight()
    {
        lineItem.LabelWeightGrams = null;
        return this;
    }

    public PackagingTenderBuilder WithZeroVolume()
    {
        lineItem.Quantity = 0m;
        return this;
    }

    public PackagingTenderBuilder WithExtremePrice()
    {
        lineItem.PricePerThousand = 99_999m;
        return this;
    }

    public PackagingTenderBuilder WithMissingCo2Data()
    {
        // CO2 is not a first-class field on LabelLineItem in Core v1; model it as a manual-review flag.
        lineItem.SourceManualReviewFlags.Add(new ManualReviewFlag
        {
            FieldName = "Co2Impact",
            Reason = "CO2 impact data is missing.",
            Severity = ManualReviewSeverity.Warning
        });
        return this;
    }

    public Tender Build() => tender;

    public LabelLineItem BuildSingleLineItem() => lineItem;
}

