using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Tests;

public sealed class EvaluationServiceTests
{
    private static LineEvaluationService CreateLineService()
    {
        return new LineEvaluationService(new LabelsEvaluationStrategy(new EprFeeService()));
    }

    [Fact]
    public void LineEvaluationServiceCreatesEvaluationForLabelLineItem()
    {
        var lineItem = new LabelLineItem
        {
            ItemNo = "LBL-001",
            SupplierName = "Acme Labels",
            Spend = 125m,
            PricePerThousand = 10m
        };

        var evaluation = CreateLineService().Evaluate(lineItem);

        Assert.Equal(lineItem.Id, evaluation.LineItemId);
        Assert.Same(lineItem, evaluation.LineItem);
        Assert.False(evaluation.RequiresManualReview);
        Assert.Empty(evaluation.ManualReviewFlags);
        Assert.Equal(100m, evaluation.ScoreBreakdown.Commercial);
        Assert.Equal(0m, evaluation.ScoreBreakdown.Technical);
        Assert.Equal(0m, evaluation.ScoreBreakdown.Regulatory);
        Assert.Equal(30m, evaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void LineEvaluationServiceFlagsMissingAndInvalidDataForManualReview()
    {
        var lineItem = new LabelLineItem
        {
            SupplierName = " ",
            Spend = -10m,
            Quantity = -1m,
            PricePerThousand = -2m,
            Price = -3m,
            TheoreticalSpend = -4m,
            NumberOfColors = -1
        };

        var evaluation = CreateLineService().Evaluate(lineItem);

        Assert.True(evaluation.RequiresManualReview);
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.SupplierName));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.Spend) && flag.Severity == ManualReviewSeverity.Error);
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.Quantity));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.PricePerThousand));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.Price));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.TheoreticalSpend));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.NumberOfColors));
        Assert.Equal(8, evaluation.ManualReviewFlags.Count);
        Assert.Null(evaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void LineEvaluationServiceFlagsMissingSpendWithoutExcludingLine()
    {
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = null
        };

        var evaluation = CreateLineService().Evaluate(lineItem);

        Assert.Same(lineItem, evaluation.LineItem);
        Assert.Contains(evaluation.ManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.Spend)
            && flag.Severity == ManualReviewSeverity.Warning);
        Assert.Contains(evaluation.ManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.PricePerThousand)
            && flag.Severity == ManualReviewSeverity.Warning);
    }

    [Fact]
    public void SupplierAggregationServiceGroupsLineEvaluationsBySupplierName()
    {
        var lineService = CreateLineService();
        var lineEvaluations = new[]
        {
            lineService.Evaluate(new LabelLineItem { SupplierName = "Acme Labels", Spend = 100m, PricePerThousand = 10m }),
            lineService.Evaluate(new LabelLineItem { SupplierName = "Beta Labels", Spend = 50m, PricePerThousand = 12m }),
            lineService.Evaluate(new LabelLineItem { SupplierName = "Acme Labels", Spend = 25m, PricePerThousand = 11m })
        };

        var supplierEvaluations = new SupplierAggregationService().AggregateBySupplierName(lineEvaluations);

        Assert.Equal(2, supplierEvaluations.Count);
        var acmeEvaluation = supplierEvaluations.Single(evaluation => evaluation.SupplierName == "Acme Labels");
        Assert.Equal(2, acmeEvaluation.LineEvaluations.Count);
        Assert.Equal(125m, acmeEvaluation.TotalSpend);
        Assert.All(acmeEvaluation.LineEvaluations, evaluation => Assert.Equal("Acme Labels", evaluation.LineItem.SupplierName));
    }

    [Fact]
    public void SupplierAggregationServiceUsesSpendWeightedScoreInputs()
    {
        var lowSpendLine = new LineEvaluation
        {
            LineItem = new LabelLineItem { SupplierName = "Acme Labels", Spend = 25m },
            ScoreBreakdown = new ScoreBreakdown { Commercial = 20m, Technical = 40m, Regulatory = 60m, Total = 80m }
        };
        lowSpendLine.LineItemId = lowSpendLine.LineItem.Id;

        var highSpendLine = new LineEvaluation
        {
            LineItem = new LabelLineItem { SupplierName = "Acme Labels", Spend = 75m },
            ScoreBreakdown = new ScoreBreakdown { Commercial = 60m, Technical = 80m, Regulatory = 100m, Total = 40m }
        };
        highSpendLine.LineItemId = highSpendLine.LineItem.Id;

        var supplierEvaluation = new SupplierAggregationService()
            .AggregateBySupplierName([lowSpendLine, highSpendLine])
            .Single();

        Assert.Equal(100m, supplierEvaluation.TotalSpend);
        Assert.Equal(50m, supplierEvaluation.ScoreBreakdown.Commercial);
        Assert.Equal(70m, supplierEvaluation.ScoreBreakdown.Technical);
        Assert.Equal(90m, supplierEvaluation.ScoreBreakdown.Regulatory);
        // Spec 13.2: supplier total is spend-weighted by line total (LineScore).
        // (80*25 + 40*75) / (25+75) = 50
        Assert.Equal(50m, supplierEvaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void SupplierAggregationServicePropagatesManualReviewFlagsFromLines()
    {
        var lineService = CreateLineService();
        var flaggedLine = lineService.Evaluate(new LabelLineItem
        {
            SupplierName = null,
            Spend = null
        });

        var supplierEvaluation = new SupplierAggregationService()
            .AggregateBySupplierName([flaggedLine])
            .Single();

        Assert.Equal(string.Empty, supplierEvaluation.SupplierName);
        Assert.Same(flaggedLine, supplierEvaluation.LineEvaluations.Single());
        Assert.True(supplierEvaluation.RequiresManualReview);
        Assert.Equal(flaggedLine.ManualReviewFlags.Count, supplierEvaluation.ManualReviewFlags.Count);
        Assert.Contains(supplierEvaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.SupplierName));
        Assert.Contains(supplierEvaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.Spend));
        Assert.Contains(supplierEvaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.PricePerThousand));
        Assert.Equal(0m, supplierEvaluation.TotalSpend);
        Assert.Null(supplierEvaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void LineEvaluationServiceScoresLowestComparablePriceHighest()
    {
        var lineItems = new[]
        {
            new LabelLineItem { SupplierName = "Acme Labels", Spend = 100m, PricePerThousand = 10m },
            new LabelLineItem { SupplierName = "Beta Packaging", Spend = 100m, PricePerThousand = 20m },
            new LabelLineItem { SupplierName = "Gamma Labels", Spend = 100m, PricePerThousand = 25m }
        };

        var evaluations = CreateLineService().EvaluateMany(lineItems);

        Assert.Equal(100m, evaluations[0].ScoreBreakdown.Commercial);
        Assert.Equal(50m, evaluations[1].ScoreBreakdown.Commercial);
        Assert.Equal(40m, evaluations[2].ScoreBreakdown.Commercial);
        Assert.Equal(30m, evaluations[0].ScoreBreakdown.Total);
        Assert.Equal(15m, evaluations[1].ScoreBreakdown.Total);
        Assert.Equal(12m, evaluations[2].ScoreBreakdown.Total);
    }

    [Fact]
    public void LineEvaluationServiceCanUseFallbackValuesForCommercialPriceBasis()
    {
        var lineItems = new[]
        {
            new LabelLineItem
            {
                SupplierName = "Acme Labels",
                Spend = 100m,
                Quantity = 10_000m,
                TheoreticalSpend = 100m
            },
            new LabelLineItem
            {
                SupplierName = "Beta Packaging",
                Spend = 150m,
                Quantity = 10_000m
            },
            new LabelLineItem
            {
                SupplierName = "Gamma Labels",
                Spend = 200m,
                Price = 20m
            }
        };

        var evaluations = CreateLineService().EvaluateMany(lineItems);

        Assert.Equal(100m, evaluations[0].ScoreBreakdown.Commercial);
        Assert.Equal(66.67m, evaluations[1].ScoreBreakdown.Commercial);
        Assert.Equal(50m, evaluations[2].ScoreBreakdown.Commercial);
    }

    [Fact]
    public void SupplierAggregationServiceAggregatesCommercialScoreBySpend()
    {
        var lineItems = new[]
        {
            new LabelLineItem { SupplierName = "Acme Labels", Spend = 25m, PricePerThousand = 10m },
            new LabelLineItem { SupplierName = "Acme Labels", Spend = 75m, PricePerThousand = 20m }
        };
        var lineEvaluations = CreateLineService().EvaluateMany(lineItems);

        var supplierEvaluation = new SupplierAggregationService()
            .AggregateBySupplierName(lineEvaluations)
            .Single();

        Assert.Equal(62.5m, supplierEvaluation.ScoreBreakdown.Commercial);
        Assert.Equal(18.75m, supplierEvaluation.ScoreBreakdown.Total);
        Assert.Equal(0m, supplierEvaluation.ScoreBreakdown.Technical);
        Assert.Equal(0m, supplierEvaluation.ScoreBreakdown.Regulatory);
    }

    [Fact]
    public void LineEvaluationServiceAppliesEprMalusToRegulatoryAndTotalScore()
    {
        var settings = new TenderSettings
        {
            ExpectedMonoMaterial = true,
            ExpectedEasySeparation = true,
            ExpectedReusableOrRecyclableMaterial = true,
            ExpectedTraceability = true
        };

        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            PricePerThousand = 10m,
            LabelWeightGrams = 1000m,
            IsMonoMaterial = true,
            IsEasyToSeparate = true,
            IsReusableOrRecyclableMaterial = true,
            HasTraceability = true,
            EprSchemes =
            [
                new EprSchemeInfo { CountryCode = "DK", Category = "Flexibles" }
            ]
        };

        var evaluation = CreateLineService().Evaluate(lineItem, settings);

        Assert.Equal(90m, evaluation.ScoreBreakdown.Regulatory);
        Assert.Equal(66m, evaluation.ScoreBreakdown.Total);
        Assert.True(evaluation.Explanations.Count > 0);
    }

    [Fact]
    public void LineEvaluationServiceScoresTechnicalExactMatches()
    {
        var tenderSettings = new TenderSettings
        {
            ExpectedMaterial = "PP white",
            ExpectedWindingDirection = "Left",
            ExpectedLabelSize = "80x120"
        };
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            PricePerThousand = 10m,
            Material = "PP white",
            WindingDirection = "Left",
            LabelSize = "80x120"
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem, tenderSettings);

        Assert.Equal(100m, evaluation.ScoreBreakdown.Technical);
        Assert.False(evaluation.RequiresManualReview);
    }

    [Fact]
    public void LineEvaluationServiceReducesTechnicalScoreForMismatches()
    {
        var tenderSettings = new TenderSettings
        {
            ExpectedMaterial = "PP white",
            ExpectedWindingDirection = "Left",
            ExpectedLabelSize = "80x120"
        };
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            PricePerThousand = 10m,
            Material = "Paper",
            WindingDirection = "Left",
            LabelSize = "80x120"
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem, tenderSettings);

        Assert.Equal(66.67m, evaluation.ScoreBreakdown.Technical);
        Assert.False(evaluation.RequiresManualReview);
    }

    [Fact]
    public void LineEvaluationServiceFlagsMissingTechnicalValuesForManualReview()
    {
        var tenderSettings = new TenderSettings
        {
            ExpectedMaterial = "PP white",
            ExpectedWindingDirection = "Left",
            ExpectedLabelSize = "80x120"
        };
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            PricePerThousand = 10m,
            Material = null,
            WindingDirection = "Left",
            LabelSize = null
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem, tenderSettings);

        Assert.True(evaluation.RequiresManualReview);
        Assert.Equal(33.33m, evaluation.ScoreBreakdown.Technical);
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.Material));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.LabelSize));
    }

    [Fact]
    public void SupplierAggregationServiceAggregatesTechnicalScoreBySpend()
    {
        var tenderSettings = new TenderSettings
        {
            ExpectedMaterial = "PP white",
            ExpectedWindingDirection = "Left",
            ExpectedLabelSize = "80x120"
        };
        var lineItems = new[]
        {
            new LabelLineItem
            {
                SupplierName = "Acme Labels",
                Spend = 25m,
                PricePerThousand = 10m,
                Material = "PP white",
                WindingDirection = "Left",
                LabelSize = "80x120"
            },
            new LabelLineItem
            {
                SupplierName = "Acme Labels",
                Spend = 75m,
                PricePerThousand = 20m,
                Material = "Paper",
                WindingDirection = "Left",
                LabelSize = "80x120"
            }
        };
        var lineEvaluations = new LineEvaluationService().EvaluateMany(lineItems, tenderSettings);

        var supplierEvaluation = new SupplierAggregationService()
            .AggregateBySupplierName(lineEvaluations)
            .Single();

        Assert.Equal(75m, supplierEvaluation.ScoreBreakdown.Technical);
        Assert.Equal(62.5m, supplierEvaluation.ScoreBreakdown.Commercial);
        Assert.Equal(0m, supplierEvaluation.ScoreBreakdown.Regulatory);
        Assert.Equal(41.25m, supplierEvaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void LineEvaluationServiceCalculatesTotalFromWeightedScoreDimensions()
    {
        var tenderSettings = new TenderSettings
        {
            ExpectedMaterial = "PP white",
            ExpectedWindingDirection = "Left",
            ExpectedLabelSize = "80x120"
        };
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            PricePerThousand = 10m,
            Material = "PP white",
            WindingDirection = "Left",
            LabelSize = "80x120"
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem, tenderSettings);

        Assert.Equal(100m, evaluation.ScoreBreakdown.Commercial);
        Assert.Equal(100m, evaluation.ScoreBreakdown.Technical);
        Assert.Equal(0m, evaluation.ScoreBreakdown.Regulatory);
        Assert.Equal(60m, evaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void LineEvaluationServiceLeavesTotalNullWhenRequiredScoreDimensionIsMissing()
    {
        var tenderSettings = new TenderSettings
        {
            ExpectedMaterial = "PP white",
            ExpectedWindingDirection = "Left",
            ExpectedLabelSize = "80x120"
        };
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            Material = "PP white",
            WindingDirection = "Left",
            LabelSize = "80x120"
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem, tenderSettings);

        Assert.Null(evaluation.ScoreBreakdown.Commercial);
        Assert.Equal(100m, evaluation.ScoreBreakdown.Technical);
        Assert.Equal(0m, evaluation.ScoreBreakdown.Regulatory);
        Assert.Null(evaluation.ScoreBreakdown.Total);
        Assert.True(evaluation.RequiresManualReview);
    }

    [Fact]
    public void SupplierAggregationServiceCalculatesTotalFromAggregatedWeightedDimensions()
    {
        var lineEvaluations = new[]
        {
            new LineEvaluation
            {
                LineItem = new LabelLineItem { SupplierName = "Acme Labels", Spend = 25m },
                ScoreBreakdown = new ScoreBreakdown { Commercial = 100m, Technical = 100m, Regulatory = 0m, Total = 60m }
            },
            new LineEvaluation
            {
                LineItem = new LabelLineItem { SupplierName = "Acme Labels", Spend = 75m },
                ScoreBreakdown = new ScoreBreakdown { Commercial = 50m, Technical = 66.67m, Regulatory = 0m, Total = 35m }
            }
        };
        foreach (var lineEvaluation in lineEvaluations)
        {
            lineEvaluation.LineItemId = lineEvaluation.LineItem.Id;
        }

        var supplierEvaluation = new SupplierAggregationService()
            .AggregateBySupplierName(lineEvaluations)
            .Single();

        Assert.Equal(62.5m, supplierEvaluation.ScoreBreakdown.Commercial);
        Assert.Equal(75m, supplierEvaluation.ScoreBreakdown.Technical);
        Assert.Equal(0m, supplierEvaluation.ScoreBreakdown.Regulatory);
        Assert.Equal(41.25m, supplierEvaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void LineEvaluationServiceScoresRegulatoryExactMatches()
    {
        var tenderSettings = CreateRegulatoryTenderSettings();
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            PricePerThousand = 10m,
            LabelWeightGrams = 1.5m,
            IsMonoMaterial = true,
            IsEasyToSeparate = true,
            IsReusableOrRecyclableMaterial = true,
            HasTraceability = true
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem, tenderSettings);

        Assert.Equal(100m, evaluation.ScoreBreakdown.Regulatory);
        Assert.False(evaluation.RequiresManualReview);
        Assert.Equal(70m, evaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void LineEvaluationServiceReducesRegulatoryScoreForMismatches()
    {
        var tenderSettings = CreateRegulatoryTenderSettings();
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            PricePerThousand = 10m,
            LabelWeightGrams = 2.5m,
            IsMonoMaterial = false,
            IsEasyToSeparate = true,
            IsReusableOrRecyclableMaterial = false,
            HasTraceability = true
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem, tenderSettings);

        Assert.Equal(40m, evaluation.ScoreBreakdown.Regulatory);
        Assert.False(evaluation.RequiresManualReview);
        Assert.Equal(46m, evaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void LineEvaluationServiceFlagsMissingRegulatoryValuesForManualReview()
    {
        var tenderSettings = CreateRegulatoryTenderSettings();
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            PricePerThousand = 10m,
            LabelWeightGrams = null,
            IsMonoMaterial = true,
            IsEasyToSeparate = null,
            IsReusableOrRecyclableMaterial = null,
            HasTraceability = true
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem, tenderSettings);

        Assert.True(evaluation.RequiresManualReview);
        Assert.Equal(40m, evaluation.ScoreBreakdown.Regulatory);
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.LabelWeightGrams));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.IsEasyToSeparate));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.IsReusableOrRecyclableMaterial));
    }

    [Fact]
    public void LineEvaluationServiceFlagsInvalidRegulatoryWeightForManualReview()
    {
        var tenderSettings = CreateRegulatoryTenderSettings();
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = 100m,
            PricePerThousand = 10m,
            LabelWeightGrams = -1m,
            IsMonoMaterial = true,
            IsEasyToSeparate = true,
            IsReusableOrRecyclableMaterial = true,
            HasTraceability = true
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem, tenderSettings);

        Assert.True(evaluation.RequiresManualReview);
        Assert.Equal(80m, evaluation.ScoreBreakdown.Regulatory);
        Assert.Contains(evaluation.ManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.LabelWeightGrams)
            && flag.Severity == ManualReviewSeverity.Error);
    }

    [Fact]
    public void SupplierAggregationServiceAggregatesRegulatoryScoreBySpend()
    {
        var tenderSettings = CreateRegulatoryTenderSettings();
        var lineItems = new[]
        {
            new LabelLineItem
            {
                SupplierName = "Acme Labels",
                Spend = 25m,
                PricePerThousand = 10m,
                LabelWeightGrams = 1.5m,
                IsMonoMaterial = true,
                IsEasyToSeparate = true,
                IsReusableOrRecyclableMaterial = true,
                HasTraceability = true
            },
            new LabelLineItem
            {
                SupplierName = "Acme Labels",
                Spend = 75m,
                PricePerThousand = 20m,
                LabelWeightGrams = 2.5m,
                IsMonoMaterial = false,
                IsEasyToSeparate = true,
                IsReusableOrRecyclableMaterial = false,
                HasTraceability = true
            }
        };
        var lineEvaluations = new LineEvaluationService().EvaluateMany(lineItems, tenderSettings);

        var supplierEvaluation = new SupplierAggregationService()
            .AggregateBySupplierName(lineEvaluations)
            .Single();

        Assert.Equal(55m, supplierEvaluation.ScoreBreakdown.Regulatory);
        Assert.Equal(62.5m, supplierEvaluation.ScoreBreakdown.Commercial);
        Assert.Equal(40.75m, supplierEvaluation.ScoreBreakdown.Total);
    }

    private static TenderSettings CreateRegulatoryTenderSettings()
    {
        return new TenderSettings
        {
            MaximumLabelWeightGrams = 2m,
            ExpectedMonoMaterial = true,
            ExpectedEasySeparation = true,
            ExpectedReusableOrRecyclableMaterial = true,
            ExpectedTraceability = true
        };
    }
}
