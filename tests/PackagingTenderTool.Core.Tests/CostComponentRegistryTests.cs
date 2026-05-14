using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Tests;

public sealed class CostComponentRegistryTests
{
    [Fact]
    public void TestComponent_is_visible_in_dashboard()
    {
        var test = CostComponentRegistry.Default.Components.Single(c => c.Key == "test_component");
        Assert.True(test.VisibleInDashboard);
    }

    [Fact]
    public void TestComponent_is_included_in_total()
    {
        var test = CostComponentRegistry.Default.Components.Single(c => c.Key == "test_component");
        Assert.True(test.IncludeInTotal);
    }

    [Fact]
    public void TestComponent_appears_in_export()
    {
        var exportKeys = CostComponentRegistry.Default.GetExportComponents().Select(c => c.Key).ToList();
        Assert.Contains("test_component", exportKeys);
    }

    [Fact]
    public void Component_order_is_stable()
    {
        var expected = new[] { "commercial", "technical", "regulatory", "switching", "test_component", "total" };
        var actual = CostComponentRegistry.Default.Components.OrderBy(c => c.Order).Select(c => c.Key).ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Test_Component_display_name_is_stable_for_bi_headers()
    {
        var test = CostComponentRegistry.Default.Components.Single(c => c.Key == "test_component");
        Assert.Equal("Test Component", test.DisplayName);
    }
}
