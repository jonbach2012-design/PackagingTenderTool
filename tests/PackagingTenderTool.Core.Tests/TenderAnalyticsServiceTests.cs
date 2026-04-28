using PackagingTenderTool.Core.Analytics;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;
using NSubstitute;

namespace PackagingTenderTool.Core.Tests;

public sealed class TenderAnalyticsServiceTests
{
    [Fact]
    public void AnalyzeCreatesSpendBreakdownsAndCandidates()
    {
        var cleaner = new LabelDataCleaningService();
        var rows = new[]
        {
            CreateLine("A", "Jæren", "90X219", "PP top white", 100m, 100m),
            CreateLine("B", "Jæren", "90x219", "PP top white", 200m, 105m),
            CreateLine("C", "Stokke", "90x219", "PP top white", 300m, 180m),
            CreateLine("D", "Stokke", "80X263", "PP top white", 400m, 120m)
        }.Select(cleaner.Clean).ToList();

        var epr = Substitute.For<IEprFeeService>();
        var summary = new TenderAnalyticsService(epr).Analyze(rows);

        Assert.Equal(1000m, summary.TotalSpend);
        Assert.Equal(4, summary.ItemCount);
        Assert.Contains(summary.SpendByCountry, item => item.Name == "Norway" && item.ItemCount == 4);
        Assert.Equal("Stokke", summary.SpendBySite[0].Name);
        Assert.Contains(summary.SpendByLabelSize, item => item.Name == "90x219" && item.ItemCount == 3);
        Assert.Contains(summary.ConsolidationCandidates, candidate =>
            candidate.LabelSize == "90x219"
            && candidate.ItemCount == 3
            && candidate.SiteCount == 2);
        Assert.Contains(summary.PriceOutlierCandidates, candidate =>
            candidate.ItemNo == "C"
            && candidate.PercentAboveMedian > 25m);
    }

    [Fact]
    public void CalculateTcoCalculatesNetSpendEprImpactTcoAndWeightedRegulatory()
    {
        var lines = new[]
        {
            new LineEvaluation
            {
                LineItem = new LabelLineItem
                {
                    SupplierName = "Acme",
                    Quantity = 1000m,
                    PricePerThousand = 100m
                },
                EprFee = 0.10m,
                ScoreBreakdown = new ScoreBreakdown { Regulatory = 80m }
            },
            new LineEvaluation
            {
                LineItem = new LabelLineItem
                {
                    SupplierName = "Acme",
                    Quantity = 500m,
                    PricePerThousand = 600m
                },
                EprFee = 0.20m,
                ScoreBreakdown = new ScoreBreakdown { Regulatory = 40m }
            }
        };
        foreach (var line in lines)
        {
            line.LineItemId = line.LineItem.Id;
        }

        var epr = Substitute.For<IEprFeeService>();
        var summary = new TenderAnalyticsService(epr).CalculateTco(lines);

        // Net spend = price * volume:
        // (100/1000)*1000 + (600/1000)*500 = 100 + 300 = 400
        Assert.Equal(400.00m, summary.TotalNetSpend);

        // EPR impact: (0.10*1000) + (0.20*500) = 100 + 100 = 200
        Assert.Equal(200.00m, summary.TotalEprImpact);

        Assert.Equal(600.00m, summary.AggregatedTco);

        // Weighted regulatory by spend uses Spend field as weighting input; none provided => null.
        Assert.Null(summary.WeightedRegulatoryScore);
    }

    [Fact]
    public void CalculateTcoAppliesVolumeMultiplierToQuantityBasedValues()
    {
        var line = new LineEvaluation
        {
            LineItem = new LabelLineItem
            {
                Quantity = 100m,
                PricePerThousand = 10m
            },
            EprFee = 0.10m,
            ScoreBreakdown = new ScoreBreakdown { Regulatory = 100m }
        };
        line.LineItemId = line.LineItem.Id;

        var epr = Substitute.For<IEprFeeService>();
        var summary = new TenderAnalyticsService(epr).CalculateTco([line], volumeMultiplier: 1.10m);

        // Derived net spend: (10/1000) * (100*1.1) = 1.1
        Assert.Equal(1.10m, summary.TotalNetSpend);

        // EPR impact: 0.10 * (100*1.1) = 11
        Assert.Equal(11.00m, summary.TotalEprImpact);
    }

    [Fact]
    public void CalculateTcoAppliesGlobalMultipliersToEprAndMaterialPrice()
    {
        var line = new LineEvaluation
        {
            LineItem = new LabelLineItem
            {
                Quantity = 100m,
                PricePerThousand = 10m
            },
            EprFee = 0.10m
        };
        line.LineItemId = line.LineItem.Id;

        var stress = new TenderStressParameters
        {
            EprInflationMultiplier = 1.10m,
            MaterialPriceMultiplier = 0.95m
        };

        var epr = Substitute.For<IEprFeeService>();
        var summary = new TenderAnalyticsService(epr).CalculateTco([line], volumeMultiplier: 1.00m, stress: stress);

        // Net spend: (10/1000)*100 = 1.0, then *0.95 = 0.95
        Assert.Equal(0.95m, summary.TotalNetSpend);

        // EPR impact: 0.10*100 = 10, then *1.10 = 11
        Assert.Equal(11.00m, summary.TotalEprImpact);
        Assert.Equal(11.95m, summary.AggregatedTco);
    }

    private static LabelLineItem CreateLine(
        string itemNo,
        string site,
        string labelSize,
        string material,
        decimal spend,
        decimal pricePerThousand)
    {
        return new LabelLineItem
        {
            ItemNo = itemNo,
            ItemName = $"Item {itemNo}",
            SupplierName = "Flexoprint AS",
            Site = site,
            LabelSize = labelSize,
            Material = material,
            Spend = spend,
            PricePerThousand = pricePerThousand
        };
    }
}
