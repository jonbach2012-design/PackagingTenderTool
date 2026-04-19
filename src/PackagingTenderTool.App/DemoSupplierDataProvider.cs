using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.App;

internal static class DemoSupplierDataProvider
{
    public static TenderEvaluationResult Create(DashboardSettings settings)
    {
        var tender = new Tender
        {
            Name = settings.TenderName,
            Settings = LabelsV1DemoConfiguration.CreateTenderSettings()
        };
        tender.Settings.CurrencyCode = settings.CurrencyCode;

        var suppliers = new List<SupplierEvaluation>
        {
            CreateSupplier("NordPack Solutions", 184000m, 86m, 82m, 78m),
            CreateSupplier("GreenWrap Nordic", 126500m, 66m, 63m, 58m),
            CreateSupplier("FlexiForm Europe", 98200m, 72m, 64m, 86m, manualReviewFlags: 3),
            CreateSupplier("ScanLabel Systems", 74250m, 40m, 45m, 38m)
        };

        var classifier = new SupplierClassificationService(
            settings.RecommendedThreshold,
            settings.ConditionalThreshold);
        classifier.ApplyClassifications(suppliers);

        return new TenderEvaluationResult
        {
            Tender = tender,
            SupplierEvaluations = suppliers
        };
    }

    private static SupplierEvaluation CreateSupplier(
        string name,
        decimal spend,
        decimal commercial,
        decimal technical,
        decimal regulatory,
        int manualReviewFlags = 0)
    {
        var scoreBreakdown = new ScoreBreakdown
        {
            Commercial = commercial,
            Technical = technical,
            Regulatory = regulatory
        };
        scoreBreakdown.Total = ScoreBreakdownCalculator.CalculateTotal(scoreBreakdown);

        var supplier = new SupplierEvaluation
        {
            SupplierName = name,
            TotalSpend = spend,
            ScoreBreakdown = scoreBreakdown
        };

        for (var index = 0; index < manualReviewFlags; index++)
        {
            supplier.ManualReviewFlags.Add(new ManualReviewFlag
            {
                FieldName = index == 0 ? nameof(LabelLineItem.Spend) : nameof(LabelLineItem.Material),
                Reason = "Demo supplier contains data that should be checked before final recommendation.",
                Severity = ManualReviewSeverity.Warning
            });
        }

        return supplier;
    }
}
