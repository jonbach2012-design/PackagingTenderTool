using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class CostComponentRegistry
{
    public static CostComponentRegistry Default { get; } = CreateDefault();

    private readonly IReadOnlyList<CostComponentDefinition> _components;

    public CostComponentRegistry(IEnumerable<CostComponentDefinition> components)
    {
        var list = components.ToList();
        foreach (var c in list)
        {
            if (c.GetValue is null)
                throw new InvalidOperationException($"Cost component '{c.Key}' has a null GetValue delegate.");
        }

        _components = list;
    }

    public IReadOnlyList<CostComponentDefinition> Components => _components;

    public IReadOnlyList<CostComponentDefinition> GetDashboardComponents() =>
        _components.Where(c => c.VisibleInDashboard).OrderBy(c => c.Order).ToList();

    public IReadOnlyList<CostComponentDefinition> GetExportComponents() =>
        _components.Where(c => c.VisibleInExport).OrderBy(c => c.Order).ToList();

    private static CostComponentRegistry CreateDefault()
    {
        IReadOnlyList<CostComponentDefinition> definitions =
        [
            new(
                Key: "commercial",
                DisplayName: "Commercial",
                Order: 1,
                Group: "Commercial",
                IncludeInTotal: true,
                VisibleInDashboard: true,
                VisibleInExport: true,
                GetValue: r => r.Commercial),
            new(
                Key: "technical",
                DisplayName: "Technical",
                Order: 2,
                Group: "Technical",
                IncludeInTotal: true,
                VisibleInDashboard: true,
                VisibleInExport: true,
                GetValue: r => r.Technical),
            new(
                Key: "regulatory",
                DisplayName: "Regulatory",
                Order: 3,
                Group: "Regulatory",
                IncludeInTotal: true,
                VisibleInDashboard: true,
                VisibleInExport: true,
                GetValue: r => r.Regulatory),
            new(
                Key: "switching",
                DisplayName: "Switching",
                Order: 4,
                Group: "Commercial",
                IncludeInTotal: true,
                VisibleInDashboard: true,
                VisibleInExport: true,
                GetValue: r => r.Switching),
            new(
                Key: "ppwr_risk",
                DisplayName: "PPWR Risk",
                Order: 5,
                Group: "Regulatory",
                IncludeInTotal: true,
                VisibleInDashboard: true,
                VisibleInExport: true,
                GetValue: r => r.PpwrRiskPenalty),
            new(
                Key: "total",
                DisplayName: "Total",
                Order: 99,
                Group: "Total",
                IncludeInTotal: false,
                VisibleInDashboard: false,
                VisibleInExport: true,
                GetValue: r => r.Total),
        ];

        return new CostComponentRegistry(definitions);
    }
}
