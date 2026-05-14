using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Tests;

public sealed class PpwrRiskMultiplierTests
{
    [Fact]
    public void Grade_A_penalty_is_zero()
    {
        var e = PpwrRiskEvaluator.Evaluate(100m, "A");
        Assert.Equal(0m, e.PenaltyAmount);
    }

    [Fact]
    public void Grade_D_penalty_is_fifteen_percent_of_commercial()
    {
        var e = PpwrRiskEvaluator.Evaluate(100m, "D");
        Assert.Equal(15m, e.PenaltyAmount);
    }

    [Fact]
    public void Grade_E_penalty_is_twenty_five_percent_of_commercial()
    {
        var e = PpwrRiskEvaluator.Evaluate(100m, "E");
        Assert.Equal(25m, e.PenaltyAmount);
    }

    [Fact]
    public void MarketAccessRisk2030_is_true_for_grade_D()
    {
        var e = PpwrRiskEvaluator.Evaluate(100m, "D");
        Assert.True(e.MarketAccessRisk2030);
        Assert.False(e.MarketAccessRiskNow);
    }

    [Fact]
    public void Ppwr_risk_component_has_IncludeInTotal_in_registry()
    {
        var c = CostComponentRegistry.Default.Components.Single(x => x.Key == "ppwr_risk");
        Assert.True(c.IncludeInTotal);
    }
}
