using PackagingTenderTool.Blazor.Models;

namespace PackagingTenderTool.Blazor.Services;

public interface ITcoCalculator
{
    TcoResult Calculate(SupplierOffer offer);
    TcoDecisionOutput CalculateDecision(SupplierOffer offer);
}

