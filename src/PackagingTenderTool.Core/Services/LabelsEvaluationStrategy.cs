using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class LabelsEvaluationStrategy : IEvaluationStrategy
{
    private readonly IEprFeeService eprFeeService;

    public LabelsEvaluationStrategy(IEprFeeService eprFeeService)
    {
        this.eprFeeService = eprFeeService;
    }

    public string GetCategoryName() => "Labels";

    public LineScoringResult EvaluateLine(
        LabelLineItem lineItem,
        IReadOnlyCollection<LabelLineItem> comparisonLines,
        TenderSettings tenderSettings)
    {
        ArgumentNullException.ThrowIfNull(lineItem);
        ArgumentNullException.ThrowIfNull(comparisonLines);
        ArgumentNullException.ThrowIfNull(tenderSettings);

        var result = new LineScoringResult();

        var commercialScore = CalculateCommercialScore(lineItem, comparisonLines);
        var technicalScore = CalculateTechnicalScore(lineItem, tenderSettings);
        var regulatoryScore = CalculateRegulatoryScore(lineItem, tenderSettings);

        regulatoryScore = ApplyEprFeeAdjustment(lineItem, regulatoryScore, result);

        result.ScoreBreakdown = new ScoreBreakdown
        {
            Commercial = commercialScore,
            Technical = technicalScore,
            Regulatory = regulatoryScore
        };

        result.ScoreBreakdown.Total = CalculateWeightedTotal(result.ScoreBreakdown, tenderSettings);

        var weights = NormalizeWeights(tenderSettings);
        result.Explanations.Add(new ScoreExplanation
        {
            Dimension = "Total",
            Text = $"Total = C*{weights.Commercial:0.##} + T*{weights.Technical:0.##} + R*{weights.Regulatory:0.##} (weights normalized)."
        });

        return result;
    }

    private decimal ApplyEprFeeAdjustment(LabelLineItem lineItem, decimal regulatoryScore, LineScoringResult result)
    {
        var baseScore = ClampScore(regulatoryScore);

        var scheme = lineItem.EprSchemes.FirstOrDefault();
        var country = scheme?.CountryCode;
        var category = scheme?.Category;

        if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(category))
        {
            result.ManualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = "EprRate",
                Reason = "EPR country/category is missing on the line item (EprSchemes).",
                Severity = ManualReviewSeverity.Warning
            });
            return baseScore;
        }

        if (lineItem.LabelWeightGrams is null)
        {
            result.ManualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.LabelWeightGrams),
                Reason = "Label weight is missing; cannot calculate EPR fee impact.",
                Severity = ManualReviewSeverity.Warning
            });
            return baseScore;
        }

        if (lineItem.LabelWeightGrams < 0m)
        {
            result.ManualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = nameof(LabelLineItem.LabelWeightGrams),
                SourceValue = lineItem.LabelWeightGrams.Value.ToString("G"),
                Reason = "Label weight cannot be negative; cannot calculate EPR fee impact.",
                Severity = ManualReviewSeverity.Error
            });
            return baseScore;
        }

        var weightKg = decimal.Round(lineItem.LabelWeightGrams.Value / 1000m, 6);
        if (weightKg <= 0m)
        {
            return baseScore;
        }

        if (!eprFeeService.TryCalculateFee(country, category, weightKg, out var fee, out var manualReviewFlag))
        {
            if (manualReviewFlag is not null)
            {
                result.ManualReviewFlags.Add(manualReviewFlag);
            }

            return baseScore;
        }

        var effectiveRatePerKg = fee / weightKg;

        decimal adjusted = baseScore;
        if (effectiveRatePerKg >= 1.00m)
        {
            adjusted = baseScore - 10m;
            result.Explanations.Add(new ScoreExplanation
            {
                Dimension = "Regulatory",
                Text = $"EPR malus applied (rate/kg={effectiveRatePerKg:0.##}). Regulatory -10."
            });
        }
        else if (effectiveRatePerKg <= 0.20m)
        {
            adjusted = baseScore + 5m;
            result.Explanations.Add(new ScoreExplanation
            {
                Dimension = "Regulatory",
                Text = $"EPR bonus applied (rate/kg={effectiveRatePerKg:0.##}). Regulatory +5."
            });
        }
        else
        {
            result.Explanations.Add(new ScoreExplanation
            {
                Dimension = "Regulatory",
                Text = $"EPR neutral (rate/kg={effectiveRatePerKg:0.##}). No adjustment."
            });
        }

        return ClampScore(adjusted);
    }

    private static decimal ClampScore(decimal score)
    {
        return score < 0m ? 0m : score > 100m ? 100m : score;
    }

    private static (decimal Commercial, decimal Technical, decimal Regulatory) NormalizeWeights(TenderSettings tenderSettings)
    {
        var c = tenderSettings.CommercialWeight;
        var t = tenderSettings.TechnicalWeight;
        var r = tenderSettings.RegulatoryWeight;
        var sum = c + t + r;

        if (sum <= 0m)
        {
            return (0.30m, 0.30m, 0.40m);
        }

        return (c / sum, t / sum, r / sum);
    }

    private static decimal? CalculateWeightedTotal(ScoreBreakdown scoreBreakdown, TenderSettings tenderSettings)
    {
        if (scoreBreakdown.Commercial is null || scoreBreakdown.Technical is null || scoreBreakdown.Regulatory is null)
        {
            return null;
        }

        var w = NormalizeWeights(tenderSettings);
        return decimal.Round(
            scoreBreakdown.Commercial.Value * w.Commercial
            + scoreBreakdown.Technical.Value * w.Technical
            + scoreBreakdown.Regulatory.Value * w.Regulatory,
            2);
    }

    private static decimal? CalculateCommercialScore(LabelLineItem lineItem, IEnumerable<LabelLineItem> comparisonLines)
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

    private static decimal CalculateTechnicalScore(LabelLineItem lineItem, TenderSettings tenderSettings)
    {
        var expectedFields = new[]
        {
            (Expected: tenderSettings.ExpectedMaterial, Actual: lineItem.Material),
            (Expected: tenderSettings.ExpectedWindingDirection, Actual: lineItem.WindingDirection),
            (Expected: tenderSettings.ExpectedLabelSize, Actual: lineItem.LabelSize)
        }.Where(field => !string.IsNullOrWhiteSpace(field.Expected)).ToList();

        if (expectedFields.Count == 0)
        {
            return 0m;
        }

        var matches = expectedFields.Count(field =>
            string.Equals(field.Expected, field.Actual, StringComparison.OrdinalIgnoreCase));

        return decimal.Round(matches / (decimal)expectedFields.Count * 100m, 2);
    }

    private static decimal CalculateRegulatoryScore(LabelLineItem lineItem, TenderSettings tenderSettings)
    {
        decimal configuredCriteria = 0m;
        decimal matchingCriteria = 0m;

        if (tenderSettings.MaximumLabelWeightGrams is not null)
        {
            configuredCriteria++;
            if (lineItem.LabelWeightGrams is >= 0m
                && lineItem.LabelWeightGrams <= tenderSettings.MaximumLabelWeightGrams)
            {
                matchingCriteria++;
            }
        }

        AddBooleanRegulatoryScore(tenderSettings.ExpectedMonoMaterial, lineItem.IsMonoMaterial, ref configuredCriteria, ref matchingCriteria);
        AddBooleanRegulatoryScore(tenderSettings.ExpectedEasySeparation, lineItem.IsEasyToSeparate, ref configuredCriteria, ref matchingCriteria);
        AddBooleanRegulatoryScore(tenderSettings.ExpectedReusableOrRecyclableMaterial, lineItem.IsReusableOrRecyclableMaterial, ref configuredCriteria, ref matchingCriteria);
        AddBooleanRegulatoryScore(tenderSettings.ExpectedTraceability, lineItem.HasTraceability, ref configuredCriteria, ref matchingCriteria);

        if (configuredCriteria == 0m)
        {
            return 0m;
        }

        return decimal.Round(matchingCriteria / configuredCriteria * 100m, 2);
    }

    private static void AddBooleanRegulatoryScore(
        bool? expectedValue,
        bool? actualValue,
        ref decimal configuredCriteria,
        ref decimal matchingCriteria)
    {
        if (expectedValue is null)
        {
            return;
        }

        configuredCriteria++;
        if (actualValue == expectedValue)
        {
            matchingCriteria++;
        }
    }
}

