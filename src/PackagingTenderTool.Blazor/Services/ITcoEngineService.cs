using PackagingTenderTool.Blazor.Models.LabelTender;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services.LabelTenderScoring;

namespace PackagingTenderTool.Blazor.Services;

public interface ITcoEngineService
{
    /// <summary>Display token for null/empty dimensions (filters and grouping).</summary>
    string UnknownDimensionValue { get; }

    string NormalizeTenderDimension(string? value);

    TenderFilterOptions GetTenderFilterOptions(
        IReadOnlyList<SupplierModel> allSuppliers,
        string? selectedCountryFilter);

    bool SupplierMatchesTenderFilters(
        SupplierModel supplier,
        string countryFilter,
        string siteFilter,
        string materialFilter,
        string adhesiveFilter);

    IReadOnlyList<LabelTenderDashboardDto> GetResults(PackagingProfileSession session, IReadOnlyList<SupplierModel> suppliers);

    /// <summary>Top supplier after applying explicit pillar weights (same rule as <see cref="GetResults"/>).</summary>
    string? GetWeightedLeaderSupplierId(
        PackagingProfileSession session,
        IReadOnlyList<SupplierModel> suppliers,
        int commercialPct,
        int technicalPct,
        int regulatoryPct);

    LabelTenderDashboardDto CalculateResult(PackagingProfileSession session, SupplierModel supplier);

    decimal ComputeTenderValueWeighted(IReadOnlyList<SupplierPillarAnalysisRow> rows, decimal tenderVolumeUnits);

    int CountCompliancePassed(IReadOnlyList<SupplierModel> suppliers, decimal maxCo2Impact, decimal maxLeadTimeDays);

    bool HasCo2Data(SupplierModel supplier);

    bool HasLeadTimeData(SupplierModel supplier);

    /// <summary>Compares top weighted pick vs. lowest commercial spend; explains value trade-offs.</summary>
    string GetRecommendationNarrative(
        PackagingProfileSession session,
        IReadOnlyList<LabelTenderDashboardDto> results);

    /// <summary>KPI payload: narrative, anchors, weighted pillar shares, and weight sensitivity.</summary>
    TenderDecisionInsight GetDecisionInsight(
        PackagingProfileSession session,
        IReadOnlyList<LabelTenderDashboardDto> results,
        IReadOnlyList<SupplierModel> suppliersWeightedInResults);
}

