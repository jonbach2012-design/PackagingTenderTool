using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Tests;

internal static class TestDataFactory
{
    public static LabelLineItem CreateStandardLabelItem(
        string supplierName = "Acme Labels",
        decimal? spend = 100m,
        decimal? pricePerThousand = 10m)
    {
        return new PackagingTenderBuilder()
            .WithSupplier(supplierName)
            .WithSpend(spend)
            .WithPricePerThousand(pricePerThousand)
            .BuildSingleLineItem();
    }

    public static LabelLineItem CreateValidLabelLineItem(
        string supplierName = "Acme Labels",
        decimal? spend = 100m,
        decimal? pricePerThousand = 10m,
        string countryCode = "DK",
        string category = "Labels",
        decimal labelWeightGrams = 100m)
    {
        var b = new PackagingTenderBuilder()
            .WithSupplier(supplierName)
            .WithSpend(spend)
            .WithPricePerThousand(pricePerThousand)
            .WithEprScheme(countryCode, category);

        if (labelWeightGrams <= 0m)
            return b.WithMissingWeight().BuildSingleLineItem();

        var item = b.BuildSingleLineItem();
        item.LabelWeightGrams = labelWeightGrams;
        return item;
    }
}

