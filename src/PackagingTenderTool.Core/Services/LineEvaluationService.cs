using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class LineEvaluationService
{
    private readonly IEvaluationStrategy evaluationStrategy;

    public LineEvaluationService(IEvaluationStrategy evaluationStrategy)
    {
        this.evaluationStrategy = evaluationStrategy;
    }

    public LineEvaluation Evaluate(LabelLineItem lineItem, TenderSettings? tenderSettings = null)
    {
        return Evaluate(lineItem, [lineItem], tenderSettings);
    }

    public IReadOnlyList<LineEvaluation> EvaluateMany(
        IEnumerable<LabelLineItem> lineItems,
        TenderSettings? tenderSettings = null)
    {
        ArgumentNullException.ThrowIfNull(lineItems);

        var lineItemList = lineItems.ToList();

        return lineItemList
            .Select(lineItem => Evaluate(lineItem, lineItemList, tenderSettings))
            .ToList();
    }

    private LineEvaluation Evaluate(
        LabelLineItem lineItem,
        IReadOnlyCollection<LabelLineItem> comparisonLines,
        TenderSettings? tenderSettings)
    {
        ArgumentNullException.ThrowIfNull(lineItem);
        tenderSettings ??= new TenderSettings();

        var scoringResult = evaluationStrategy.EvaluateLine(lineItem, comparisonLines, tenderSettings);

        var evaluation = new LineEvaluation
        {
            LineItemId = lineItem.Id,
            LineItem = lineItem,
            ScoreBreakdown = scoringResult.ScoreBreakdown,
            EprFee = scoringResult.EprFee,
            Explanations = scoringResult.Explanations
        };

        foreach (var sourceManualReviewFlag in lineItem.SourceManualReviewFlags)
        {
            evaluation.ManualReviewFlags.Add(sourceManualReviewFlag);
        }

        foreach (var flag in scoringResult.ManualReviewFlags)
        {
            evaluation.ManualReviewFlags.Add(flag);
        }

        AddManualReviewFlags(lineItem, tenderSettings, evaluation.ManualReviewFlags);

        return evaluation;
    }

    private static void AddManualReviewFlags(
        LabelLineItem lineItem,
        TenderSettings? tenderSettings,
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

        AddMissingTechnicalReferenceFlag(
            manualReviewFlags,
            nameof(LabelLineItem.Material),
            lineItem.Material,
            tenderSettings?.ExpectedMaterial);
        AddMissingTechnicalReferenceFlag(
            manualReviewFlags,
            nameof(LabelLineItem.WindingDirection),
            lineItem.WindingDirection,
            tenderSettings?.ExpectedWindingDirection);
        AddMissingTechnicalReferenceFlag(
            manualReviewFlags,
            nameof(LabelLineItem.LabelSize),
            lineItem.LabelSize,
            tenderSettings?.ExpectedLabelSize);

        AddRegulatoryManualReviewFlags(lineItem, tenderSettings, manualReviewFlags);
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

    private static void AddMissingTechnicalReferenceFlag(
        ICollection<ManualReviewFlag> manualReviewFlags,
        string fieldName,
        string? actualValue,
        string? expectedValue)
    {
        if (string.IsNullOrWhiteSpace(expectedValue) || !string.IsNullOrWhiteSpace(actualValue))
        {
            return;
        }

        manualReviewFlags.Add(new ManualReviewFlag
        {
            FieldName = fieldName,
            SourceValue = actualValue,
            Reason = $"A value is missing for technical comparison against expected {fieldName}.",
            Severity = ManualReviewSeverity.Warning
        });
    }

    private static void AddRegulatoryManualReviewFlags(
        LabelLineItem lineItem,
        TenderSettings? tenderSettings,
        ICollection<ManualReviewFlag> manualReviewFlags)
    {
        if (tenderSettings is null)
        {
            return;
        }

        if (lineItem.LabelWeightGrams < 0m)
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.LabelWeightGrams),
                SourceValue = lineItem.LabelWeightGrams.Value.ToString("G"),
                Reason = "Label weight cannot be negative.",
                Severity = ManualReviewSeverity.Error
            });
        }
        else if (tenderSettings.MaximumLabelWeightGrams is not null
            && lineItem.LabelWeightGrams is null)
        {
            manualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.LabelWeightGrams),
                Reason = "Label weight is missing for regulatory comparison.",
                Severity = ManualReviewSeverity.Warning
            });
        }

        AddMissingRegulatoryFlag(
            manualReviewFlags,
            nameof(LabelLineItem.IsMonoMaterial),
            lineItem.IsMonoMaterial,
            tenderSettings.ExpectedMonoMaterial);
        AddMissingRegulatoryFlag(
            manualReviewFlags,
            nameof(LabelLineItem.IsEasyToSeparate),
            lineItem.IsEasyToSeparate,
            tenderSettings.ExpectedEasySeparation);
        AddMissingRegulatoryFlag(
            manualReviewFlags,
            nameof(LabelLineItem.IsReusableOrRecyclableMaterial),
            lineItem.IsReusableOrRecyclableMaterial,
            tenderSettings.ExpectedReusableOrRecyclableMaterial);
        AddMissingRegulatoryFlag(
            manualReviewFlags,
            nameof(LabelLineItem.HasTraceability),
            lineItem.HasTraceability,
            tenderSettings.ExpectedTraceability);
    }

    private static void AddMissingRegulatoryFlag(
        ICollection<ManualReviewFlag> manualReviewFlags,
        string fieldName,
        bool? actualValue,
        bool? expectedValue)
    {
        if (expectedValue is null || actualValue is not null)
        {
            return;
        }

        manualReviewFlags.Add(new ManualReviewFlag
        {
            FieldName = fieldName,
            Reason = $"A value is missing for regulatory comparison against expected {fieldName}.",
            Severity = ManualReviewSeverity.Warning
        });
    }
}
