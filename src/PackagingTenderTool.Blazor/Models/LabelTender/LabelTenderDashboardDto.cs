namespace PackagingTenderTool.Blazor.Models.LabelTender;

public sealed class LabelTenderDashboardDto
{
    public required string SupplierName { get; init; }
    public required decimal Commercial { get; init; }
    public required decimal Epr { get; init; }
    public required decimal Switching { get; init; }
    public required decimal Moq { get; init; }
    public required decimal Total { get; init; }
    public required decimal PriceScore { get; init; }
    public required decimal TechScore { get; init; }
    public required decimal RegScore { get; init; }
    public required decimal FinalCtrScore { get; init; }

    public required double CommercialWidth { get; init; }
    public required double RegulatoryWidth { get; init; }
    public required double SwitchingWidth { get; init; }
    public required double MoqWidth { get; init; }
    public required double TotalWidth { get; init; }

    public required string CalculationBreakdown { get; init; }
}

