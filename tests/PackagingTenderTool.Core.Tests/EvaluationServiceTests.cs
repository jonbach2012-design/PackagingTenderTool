using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Tests;

public sealed class EvaluationServiceTests
{
    [Fact]
    public void LineEvaluationServiceCreatesEvaluationForLabelLineItem()
    {
        var lineItem = new LabelLineItem
        {
            ItemNo = "LBL-001",
            SupplierName = "Acme Labels",
            Spend = 125m
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem);

        Assert.Equal(lineItem.Id, evaluation.LineItemId);
        Assert.Same(lineItem, evaluation.LineItem);
        Assert.False(evaluation.RequiresManualReview);
        Assert.Empty(evaluation.ManualReviewFlags);
        Assert.Equal(0m, evaluation.ScoreBreakdown.Commercial);
        Assert.Equal(0m, evaluation.ScoreBreakdown.Technical);
        Assert.Equal(0m, evaluation.ScoreBreakdown.Regulatory);
        Assert.Equal(0m, evaluation.ScoreBreakdown.Total);
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

        var evaluation = new LineEvaluationService().Evaluate(lineItem);

        Assert.True(evaluation.RequiresManualReview);
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.SupplierName));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.Spend) && flag.Severity == ManualReviewSeverity.Error);
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.Quantity));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.PricePerThousand));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.Price));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.TheoreticalSpend));
        Assert.Contains(evaluation.ManualReviewFlags, flag => flag.FieldName == nameof(LabelLineItem.NumberOfColors));
        Assert.Equal(7, evaluation.ManualReviewFlags.Count);
        Assert.Equal(0m, evaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void LineEvaluationServiceFlagsMissingSpendWithoutExcludingLine()
    {
        var lineItem = new LabelLineItem
        {
            SupplierName = "Acme Labels",
            Spend = null
        };

        var evaluation = new LineEvaluationService().Evaluate(lineItem);

        Assert.Same(lineItem, evaluation.LineItem);
        Assert.Contains(evaluation.ManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.Spend)
            && flag.Severity == ManualReviewSeverity.Warning);
    }

    [Fact]
    public void SupplierAggregationServiceGroupsLineEvaluationsBySupplierName()
    {
        var lineService = new LineEvaluationService();
        var lineEvaluations = new[]
        {
            lineService.Evaluate(new LabelLineItem { SupplierName = "Acme Labels", Spend = 100m }),
            lineService.Evaluate(new LabelLineItem { SupplierName = "Beta Labels", Spend = 50m }),
            lineService.Evaluate(new LabelLineItem { SupplierName = "Acme Labels", Spend = 25m })
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
        Assert.Equal(50m, supplierEvaluation.ScoreBreakdown.Total);
    }

    [Fact]
    public void SupplierAggregationServicePropagatesManualReviewFlagsFromLines()
    {
        var lineService = new LineEvaluationService();
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
        Assert.Equal(0m, supplierEvaluation.TotalSpend);
        Assert.Null(supplierEvaluation.ScoreBreakdown.Total);
    }
}
