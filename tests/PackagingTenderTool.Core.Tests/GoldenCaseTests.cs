using NSubstitute;
using PackagingTenderTool.Blazor;
using PackagingTenderTool.Blazor.Services;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Tests;

public sealed class GoldenCaseTests
{
    [Fact]
    public void IncompleteData_MissingWeight_WithUnitPrice150_IsNonCompliant_AddsPenaltyAndExplainability()
    {
        // Arrange: EPR service mocked (should not be called for missing weight penalty path, but stays deterministic).
        var epr = Substitute.For<IEprFeeService>();
        epr.TryCalculateFee(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), out Arg.Any<decimal>(), out Arg.Any<ManualReviewFlag?>())
            .Returns(call =>
            {
                call[3] = 0m;
                call[4] = null;
                return false;
            });

        var regulatory = new RegulatoryService(epr);
        var engine = new TcoEngineService(regulatory);

        var session = new PackagingProfileSession();
        session.SelectLabels();
        session.SetApplyPpwr2030Scenario(true);

        var line = new PackagingTenderBuilder()
            .WithPrice(150.00m)
            .WithMissingWeight()
            .WithQuantity(1m)
            .BuildSingleLineItem();

        var supplier = new SupplierModel
        {
            SupplierId = "acme",
            SupplierName = line.SupplierName ?? "Acme Labels",
            Country = "DK",
            QuantityLabels = line.Quantity ?? 0m,
            Price = line.Price ?? 0m,
            LabelWeightGramsPerUnit = line.LabelWeightGrams ?? 0m,
            TechnicalScore = 50m,
            RegulatoryScore = 50m,
            CommercialScore = 50m
        };

        // Act
        var result = engine.CalculateResult(session, supplier);

        // Assert
        Assert.False(result.IsCompliant);
        Assert.True(result.TotalTco > 150.00m);
        Assert.Contains("Penalty", result.CalculationBreakdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing weight", result.CalculationBreakdown, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Total > result.Commercial);
    }
}

