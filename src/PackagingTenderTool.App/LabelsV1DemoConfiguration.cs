using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.App;

internal static class LabelsV1DemoConfiguration
{
    public static TenderSettings CreateTenderSettings()
    {
        return new TenderSettings
        {
            PackagingProfile = PackagingProfile.Labels,
            CurrencyCode = "EUR",
            ExpectedMaterial = "PP white",
            ExpectedWindingDirection = "Left",
            ExpectedLabelSize = "80x120",
            MaximumLabelWeightGrams = 2m,
            ExpectedMonoMaterial = true,
            ExpectedEasySeparation = true,
            ExpectedReusableOrRecyclableMaterial = true,
            ExpectedTraceability = true
        };
    }
}
