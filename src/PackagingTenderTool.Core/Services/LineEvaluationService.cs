using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class LineEvaluationService
{
    public LineEvaluation Evaluate(LabelLineItem lineItem)
    {
        return Evaluate(lineItem, [lineItem]);
    }

    public IReadOnlyList<LineEvaluation> EvaluateMany(IEnumerable<LabelLineItem> lineItems)
    {
        ArgumentNullException.ThrowIfNull(lineItems);

        var lineItemList = lineItems.ToList();

        return lineItemList
            .Select(lineItem => Evaluate(lineItem, lineItemList))
            .ToList();
    }

    private static LineEvaluation Evaluate(
        LabelLineItem lineItem,
        IReadOnlyCollection<LabelLineItem> comparisonLines)
    {
        ArgumentNullException.ThrowIfNull(lineItem);

        var evaluation = new LineEvaluation
        {
            LineItemId = lineItem.Id,
            LineItem = lineItem,
            ScoreBreakdown = CreateScoreBreakdown(lineItem, comparisonLines)
        };

        AddManualReviewFlags(lineItem, evaluation.ManualReviewFlags);

        return evaluation;
    }

    private static ScoreBreakdown CreateScoreBreakdown(
        LabelLineItem lineItem,
        IEnumerable<LabelLineItem> comparisonLines)
    {
        var commercialScore = CalculateCommercialScore(lineItem, comparisonLines);

        return new ScoreBreakdown
        {
            Commercial = commercialScore,
            Technical = 0m,
            Regulatory = 0m,
            Total = commercialScore
        };
    }

    private static decimal? CalculateCommercialScore(
        LabelLineItem lineItem,
        IEnumerable<LabelLineItem> comparisonLines)
    {
        var linePrice = GetComparablePrice(lineItem);
        if (linePrice is null)
        {
            return null;
        }

        var lowestPrice = comparisonLines
            .Select(GetComparablePrice)
            .Where(price => price is > 0m)
            .Select(price => price.GetValueOrDefault())
            .Min();

        return decimal.Round(lowestPrice / linePrice.Value * 100m, 2);
    }

    private static decimal? GetComparablePrice(LabelLineItem lineItem)
    {
        if (lineItem.PricePerThousand is > 0m)
        {
            return lineItem.PricePerThousand.Value;
        }

        if (lineItem.Price is > 0m)
        {
            return lineItem.Price.Value;
        }

        if (lineItem.TheoreticalSpend is > 0m && lineItem.Quantity is > 0m)
        {
            return lineItem.TheoreticalSpend.Value / lineItem.Quantity.Value * 1_000m;
        }

        if (lineItem.Spend is > 0m && lineItem.Quantity is > 0m)
        {
            return lineItem.Spend.Value / lineItem.Quantity.Value * 1_000m;
        }

        return null;
    }

    private static void AddManualReviewFlags(
        LabelLineItem lineItem,
        ICollection<ManualReviewFlag> manualReviewFlags)
    {
        if (string.IsNullOrWhiteSpace(lineItem.SupplierName))
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.SupplierName),
                SourceValue = lineItem.SupplierName,
                Reason = "Supplier name is missing.",
                Severity = ManualReviewSeverity.Warning
            });
        }

        if (lineItem.Spend is null)
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.Spend),
                Reason = "Spend is missing and cannot be used for supplier weighting.",
                Severity = ManualReviewSeverity.Warning
            });
        }
        else if (lineItem.Spend < 0m)
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.Spend),
                SourceValue = lineItem.Spend.Value.ToString("G"),
                Reason = "Spend cannot be negative.",
                Severity = ManualReviewSeverity.Error
            });
        }

        if (lineItem.Quantity < 0m)
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.Quantity),
                SourceValue = lineItem.Quantity.Value.ToString("G"),
                Reason = "Quantity cannot be negative.",
                Severity = ManualReviewSeverity.Error
            });
        }

        if (lineItem.PricePerThousand < 0m)
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.PricePerThousand),
                SourceValue = lineItem.PricePerThousand.Value.ToString("G"),
                Reason = "Price per thousand cannot be negative.",
                Severity = ManualReviewSeverity.Error
            });
        }

        if (lineItem.Price < 0m)
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.Price),
                SourceValue = lineItem.Price.Value.ToString("G"),
                Reason = "Price cannot be negative.",
                Severity = ManualReviewSeverity.Error
            });
        }

        if (lineItem.TheoreticalSpend < 0m)
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.TheoreticalSpend),
                SourceValue = lineItem.TheoreticalSpend.Value.ToString("G"),
                Reason = "Theoretical spend cannot be negative.",
                Severity = ManualReviewSeverity.Error
            });
        }

        if (lineItem.NumberOfColors < 0)
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.NumberOfColors),
                SourceValue = lineItem.NumberOfColors.Value.ToString("G"),
                Reason = "Number of colors cannot be negative.",
                Severity = ManualReviewSeverity.Error
            });
        }

        if (GetComparablePrice(lineItem) is null)
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.PricePerThousand),
                Reason = "A positive comparable price is missing for commercial scoring.",
                Severity = ManualReviewSeverity.Warning
            });
        }
    }
}
