using PackagingTenderTool.Blazor.Models.LabelTender;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services.LabelTenderScoring;

namespace PackagingTenderTool.Blazor.Services;

public interface ITcoEngineService
{
    IReadOnlyList<LabelTenderDashboardDto> GetResults(PackagingProfileSession session, IReadOnlyList<SupplierModel> suppliers);

    LabelTenderDashboardDto CalculateResult(PackagingProfileSession session, SupplierModel supplier);

    decimal ComputeTenderValueWeighted(IReadOnlyList<SupplierPillarAnalysisRow> rows, decimal tenderVolumeUnits);

    int CountCompliancePassed(IReadOnlyList<SupplierModel> suppliers, decimal maxCo2Impact, decimal maxLeadTimeDays);

    bool HasCo2Data(SupplierModel supplier);

    bool HasLeadTimeData(SupplierModel supplier);
}

