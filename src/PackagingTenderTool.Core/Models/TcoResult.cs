namespace PackagingTenderTool.Core.Models;

public sealed record TcoResult(
    decimal Commercial,
    decimal Regulatory,
    decimal Technical,
    decimal Switching,
    decimal Total)
{
    public string TechnicalSummary { get; set; } = string.Empty;

    public decimal PpwrRiskPenalty { get; set; }

    public string PpwrGrade { get; set; } = "A";

    public bool MarketAccessRisk2030 { get; set; }

    public bool MarketAccessRiskNow { get; set; }

    public string PpwrRiskBreakdown { get; set; } = string.Empty;
}
