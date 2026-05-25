using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Tests;

public sealed class SupplierPriceCompareServiceTests
{
    private readonly SupplierPriceCompareService _service = new();

    [Fact]
    public void TwoSuppliersOneLine_CheapestSupplierHasLowestPriceCountOne_OtherZero()
    {
        const decimal quantity = 1000m;
        var lines = new[]
        {
            Line("ITEM-1", "Alpha", quantity, 100m),
            Line("ITEM-1", "Beta", quantity, 200m)
        };

        var result = _service.Compare(lines, ["Alpha", "Beta"]);

        var alpha = result.SupplierRows.Single(r => r.SupplierName == "Alpha");
        var beta = result.SupplierRows.Single(r => r.SupplierName == "Beta");

        Assert.Equal(1, alpha.LowestPriceCount);
        Assert.Equal(0, beta.LowestPriceCount);
        Assert.Equal(100m, alpha.TotalOfferedSpend);
        Assert.Equal(200m, beta.TotalOfferedSpend);
    }

    [Fact]
    public void ThreeSuppliersFiveLines_LowestPriceCountSumsToFiveAcrossSuppliers()
    {
        const decimal quantity = 1000m;
        var lines = new List<LabelLineItem>
        {
            Line("ITEM-1", "Alpha", quantity, 100m),
            Line("ITEM-1", "Beta", quantity, 110m),
            Line("ITEM-1", "Gamma", quantity, 120m),

            Line("ITEM-2", "Alpha", quantity, 130m),
            Line("ITEM-2", "Beta", quantity, 120m),
            Line("ITEM-2", "Gamma", quantity, 110m),

            Line("ITEM-3", "Alpha", quantity, 140m),
            Line("ITEM-3", "Beta", quantity, 130m),
            Line("ITEM-3", "Gamma", quantity, 120m),

            Line("ITEM-4", "Alpha", quantity, 150m),
            Line("ITEM-4", "Beta", quantity, 140m),
            Line("ITEM-4", "Gamma", quantity, 130m),

            Line("ITEM-5", "Alpha", quantity, 160m),
            Line("ITEM-5", "Beta", quantity, 150m),
            Line("ITEM-5", "Gamma", quantity, 140m)
        };

        var result = _service.Compare(lines, ["Alpha", "Beta", "Gamma"]);

        Assert.Equal(5, result.SupplierRows.Sum(r => r.LowestPriceCount));
        Assert.Equal(5, result.LineRows.Count);
    }

    [Fact]
    public void OneSupplierMissingBidOnOneLine_MissingLineCountIsOne()
    {
        const decimal quantity = 1000m;
        var lines = new[]
        {
            Line("ITEM-1", "Alpha", quantity, 100m),
            Line("ITEM-1", "Beta", quantity, 110m),
            Line("ITEM-2", "Alpha", quantity, 100m)
        };

        var result = _service.Compare(lines, ["Alpha", "Beta"]);

        var beta = result.SupplierRows.Single(r => r.SupplierName == "Beta");

        Assert.Equal(1, beta.QuotedLineCount);
        Assert.Equal(1, beta.MissingLineCount);
    }

    [Fact]
    public void AllSuppliersSamePrice_PctVsCheapestIsZeroForAll()
    {
        const decimal quantity = 1000m;
        const decimal price = 500m;
        var lines = new[]
        {
            Line("ITEM-1", "Alpha", quantity, price),
            Line("ITEM-1", "Beta", quantity, price),
            Line("ITEM-1", "Gamma", quantity, price),
            Line("ITEM-2", "Alpha", quantity, price),
            Line("ITEM-2", "Beta", quantity, price),
            Line("ITEM-2", "Gamma", quantity, price)
        };

        var result = _service.Compare(lines, ["Alpha", "Beta", "Gamma"]);

        Assert.All(result.SupplierRows, row => Assert.Equal(0m, row.PctVsCheapest));
        Assert.All(result.SupplierRows, row => Assert.Equal(0m, row.SpendVsCheapest));
    }

    [Fact]
    public void MostExpensiveSupplier_SpendVsCheapestIsCorrectKrAmount()
    {
        const decimal quantity = 10_000m;
        var lines = new[]
        {
            Line("ITEM-1", "Alpha", quantity, 100m),
            Line("ITEM-1", "Beta", quantity, 150m),
            Line("ITEM-1", "Gamma", quantity, 200m)
        };

        var result = _service.Compare(lines, ["Alpha", "Beta", "Gamma"]);

        var gamma = result.SupplierRows.Single(r => r.SupplierName == "Gamma");

        Assert.Equal(1000m, result.SupplierRows.Min(r => r.TotalOfferedSpend));
        Assert.Equal(2000m, gamma.TotalOfferedSpend);
        Assert.Equal(1000m, gamma.SpendVsCheapest);
    }

    private static LabelLineItem Line(
        string itemNo,
        string supplierName,
        decimal quantity,
        decimal pricePerThousand) =>
        new()
        {
            ItemNo = itemNo,
            SupplierName = supplierName,
            Quantity = quantity,
            PricePerThousand = pricePerThousand,
            LabelSize = "90X219",
            Site = "Jæren",
            Material = "PP"
        };
}
