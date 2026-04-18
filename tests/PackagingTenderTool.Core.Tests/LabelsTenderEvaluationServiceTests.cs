using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Tests;

public sealed class LabelsTenderEvaluationServiceTests
{
    [Fact]
    public void EvaluateRunsLineSupplierAndClassificationFlow()
    {
        var tender = new Tender
        {
            Name = "Labels tender",
            Settings = new TenderSettings
            {
                ExpectedMaterial = "PP white",
                ExpectedWindingDirection = "Left",
                ExpectedLabelSize = "80x120",
                MaximumLabelWeightGrams = 2m,
                ExpectedMonoMaterial = true,
                ExpectedEasySeparation = true,
                ExpectedReusableOrRecyclableMaterial = true,
                ExpectedTraceability = true
            }
        };
        tender.LabelLineItems.Add(new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            PricePerThousand = 10m,
            Material = "PP white",
            WindingDirection = "Left",
            LabelSize = "80x120",
            LabelWeightGrams = 1.5m,
            IsMonoMaterial = true,
            IsEasyToSeparate = true,
            IsReusableOrRecyclableMaterial = true,
            HasTraceability = true
        });

        var result = new LabelsTenderEvaluationService().Evaluate(tender);
        var supplierEvaluation = result.SupplierEvaluations.Single();

        Assert.Same(tender, result.Tender);
        Assert.Single(result.LineEvaluations);
        Assert.Equal("Acme Labels", supplierEvaluation.SupplierName);
        Assert.Equal(100m, supplierEvaluation.ScoreBreakdown.Regulatory);
        Assert.Equal(100m, supplierEvaluation.ScoreBreakdown.Total);
        Assert.Equal(SupplierClassification.Recommended, supplierEvaluation.Classification);
        Assert.False(supplierEvaluation.RequiresManualReview);
    }
}
