using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public interface IEvaluationStrategy
{
    string GetCategoryName();

    LineScoringResult EvaluateLine(
        LabelLineItem lineItem,
        IReadOnlyCollection<LabelLineItem> comparisonLines,
        TenderSettings tenderSettings);
}

