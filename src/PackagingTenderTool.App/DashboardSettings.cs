using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.App;

internal sealed class DashboardSettings
{
    public string TenderType { get; set; } = "Labels";

    public string TenderName { get; set; } = "Labels Tender v1";

    public string CurrencyCode { get; set; } = "EUR";

    public decimal RecommendedThreshold { get; set; } = SupplierClassificationService.DefaultRecommendedThreshold;

    public decimal ConditionalThreshold { get; set; } = SupplierClassificationService.DefaultConditionalThreshold;

    public bool MissingDataManualReview { get; set; } = true;

    public bool NormalizeInputValues { get; set; } = true;

    public bool StrictMode { get; set; }
}
