using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class LineEvaluationService
{
    public LineEvaluation Evaluate(LabelLineItem lineItem)
    {
        ArgumentNullException.ThrowIfNull(lineItem);

        var evaluation = new LineEvaluation
        {
            LineItemId = lineItem.Id,
            LineItem = lineItem,
            ScoreBreakdown = CreatePlaceholderScoreBreakdown()
        };

        AddManualReviewFlags(lineItem, evaluation.ManualReviewFlags);

        return evaluation;
    }

    private static ScoreBreakdown CreatePlaceholderScoreBreakdown()
    {
        return new ScoreBreakdown
        {
            Commercial = 0m,
            Technical = 0m,
            Regulatory = 0m,
            Total = 0m
        };
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
    }
}
