using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Tests;

public sealed class CostComponentRegistryTests
{
    [Fact]
    public void Ppwr_risk_is_visible_in_dashboard()
    {
        var c = CostComponentRegistry.Default.Components.Single(x => x.Key == "ppwr_risk");
        Assert.True(c.VisibleInDashboard);
    }

    [Fact]
    public void Ppwr_risk_is_included_in_total()
    {
        var c = CostComponentRegistry.Default.Components.Single(x => x.Key == "ppwr_risk");
        Assert.True(c.IncludeInTotal);
    }

    [Fact]
    public void Ppwr_risk_appears_in_export()
    {
        var exportKeys = CostComponentRegistry.Default.GetExportComponents().Select(c => c.Key).ToList();
        Assert.Contains("ppwr_risk", exportKeys);
    }

    [Fact]
    public void Component_order_is_stable()
    {
        var expected = new[] { "commercial", "technical", "regulatory", "switching", "ppwr_risk", "total" };
        var actual = CostComponentRegistry.Default.Components.OrderBy(c => c.Order).Select(c => c.Key).ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Ppwr_risk_display_name_is_stable_for_bi_headers()
    {
        var c = CostComponentRegistry.Default.Components.Single(x => x.Key == "ppwr_risk");
        Assert.Equal("PPWR Risk", c.DisplayName);
    }
}
