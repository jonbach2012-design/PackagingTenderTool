using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class SupplierClassificationService
{
    public const decimal DefaultRecommendedThreshold = 70m;
    public const decimal DefaultConditionalThreshold = 50m;

    public SupplierClassificationService(
        decimal recommendedThreshold = DefaultRecommendedThreshold,
        decimal conditionalThreshold = DefaultConditionalThreshold)
    {
        if (recommendedThreshold < conditionalThreshold)
        {
            throw new ArgumentException("Recommended threshold must be greater than or equal to conditional threshold.");
        }

        RecommendedThreshold = recommendedThreshold;
        ConditionalThreshold = conditionalThreshold;
    }

    public decimal RecommendedThreshold { get; }

    public decimal ConditionalThreshold { get; }

    public SupplierClassification Classify(SupplierEvaluation supplierEvaluation)
    {
        ArgumentNullException.ThrowIfNull(supplierEvaluation);

        if (supplierEvaluation.RequiresManualReview)
        {
            return SupplierClassification.ManualReview;
        }

        if (supplierEvaluation.ScoreBreakdown.Total is null)
        {
            return SupplierClassification.ManualReview;
        }

        if (supplierEvaluation.ScoreBreakdown.Total >= RecommendedThreshold)
        {
            return SupplierClassification.Recommended;
        }

        if (supplierEvaluation.ScoreBreakdown.Total >= ConditionalThreshold)
        {
            return SupplierClassification.Conditional;
        }

        return SupplierClassification.NotRecommended;
    }

    public void ApplyClassification(SupplierEvaluation supplierEvaluation)
    {
        ArgumentNullException.ThrowIfNull(supplierEvaluation);

        supplierEvaluation.Classification = Classify(supplierEvaluation);
        supplierEvaluation.ClassificationReason = CreateReason(supplierEvaluation);
    }

    public IReadOnlyList<SupplierEvaluation> ApplyClassifications(IEnumerable<SupplierEvaluation> supplierEvaluations)
    {
        ArgumentNullException.ThrowIfNull(supplierEvaluations);

        var supplierEvaluationList = supplierEvaluations.ToList();
        foreach (var supplierEvaluation in supplierEvaluationList)
        {
            ApplyClassification(supplierEvaluation);
        }

        return supplierEvaluationList;
    }

    private string CreateReason(SupplierEvaluation supplierEvaluation)
    {
        if (supplierEvaluation.RequiresManualReview)
        {
            return "Manual review is required because one or more supplier or line evaluation flags are present.";
        }

        if (supplierEvaluation.ScoreBreakdown.Total is null)
        {
            return "Manual review is required because the total weighted score could not be calculated.";
        }

        return supplierEvaluation.Classification switch
        {
            SupplierClassification.Recommended =>
                $"Total weighted score is at or above the provisional {RecommendedThreshold:0.##} threshold.",
            SupplierClassification.Conditional =>
                $"Total weighted score is at or above the provisional {ConditionalThreshold:0.##} threshold.",
            SupplierClassification.NotRecommended =>
                $"Total weighted score is below the provisional {ConditionalThreshold:0.##} threshold.",
            _ => "Classification could not be determined."
        };
    }
}
