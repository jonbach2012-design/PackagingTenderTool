namespace PackagingTenderTool.Core.Models;

public sealed class TenderSettings
{
    public PackagingProfile PackagingProfile { get; set; } = PackagingProfile.Labels;

    public string CurrencyCode { get; set; } = "EUR";

    /// <summary>
    /// Target currency for all price comparisons in this tender.
    /// All imported prices are converted to this currency.
    /// Supported values: "DKK", "NOK", "SEK", "EUR"
    /// </summary>
    public string TargetCurrency { get; set; } = "NOK";

    /// <summary>
    /// Exchange rates used for currency conversion at import time.
    /// Key format: "FROM:TO" e.g. "DKK:NOK". Inverse computed automatically.
    /// Default: DKK→NOK = 1.4403
    /// </summary>
    public Dictionary<string, decimal> CurrencyRates { get; set; } = new()
    {
        ["DKK:NOK"] = 1.4403m,
        ["NOK:SEK"] = 0.9650m,
        ["NOK:EUR"] = 0.0870m,
        ["DKK:EUR"] = 0.1342m,
        ["DKK:SEK"] = 1.3901m,
    };

    // Scoring weights (slider-ready). Defaults match spec v1 direction.
    // These are normalized at calculation time to remain robust.
    public decimal CommercialWeight { get; set; } = 0.30m;

    public decimal TechnicalWeight { get; set; } = 0.30m;

    public decimal RegulatoryWeight { get; set; } = 0.40m;

    public string? ExpectedMaterial { get; set; }

    public string? ExpectedWindingDirection { get; set; }

    public string? ExpectedLabelSize { get; set; }

    public decimal? MaximumLabelWeightGrams { get; set; }

    public bool? ExpectedMonoMaterial { get; set; }

    public bool? ExpectedEasySeparation { get; set; }

    public bool? ExpectedReusableOrRecyclableMaterial { get; set; }

    public bool? ExpectedTraceability { get; set; }

    // PPWR / EPR (Regulatory) - reference/expectation settings.
    // Kept nullable/optional to allow incremental rollout and Manual Review behavior.

    /// <summary>
    /// Expected minimum recyclability/recycling grade for evaluated line items.
    /// </summary>
    public RecyclingGrade? MinimumRecyclingGrade { get; set; }

    /// <summary>
    /// Expected minimum recycled content percentage (0-100), if required by PPWR/EPR direction.
    /// </summary>
    public decimal? MinimumRecycledContentPercent { get; set; }

    /// <summary>
    /// Expected minimum rPET content percentage (0-100), if PET-specific targets are used.
    /// </summary>
    public decimal? MinimumRecycledPetContentPercent { get; set; }

    /// <summary>
    /// Optional reference fractions for preferred composition (e.g. mono-material targets).
    /// If configured, scoring/compliance checks can compare actual fractions to these.
    /// </summary>
    public List<MaterialFractionExpectation> ExpectedMaterialFractions { get; set; } = [];

    /// <summary>
    /// Expected EPR schemes/markets to be covered by the tender (PPWR/EPR readiness).
    /// </summary>
    public List<EprSchemeRequirement> RequiredEprSchemes { get; set; } = [];
}

public sealed class MaterialFractionExpectation
{
    public string Material { get; set; } = string.Empty;

    /// <summary>
    /// Target percent, if a specific composition is expected.
    /// </summary>
    public decimal? TargetPercent { get; set; }

    /// <summary>
    /// Minimum percent, if a lower bound is expected.
    /// </summary>
    public decimal? MinPercent { get; set; }

    /// <summary>
    /// Maximum percent, if an upper bound is expected.
    /// </summary>
    public decimal? MaxPercent { get; set; }
}

public sealed class EprSchemeRequirement
{
    /// <summary>
    /// Country/market code the tender must support (e.g. "DK", "SE").
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Scheme/PRO name required for the tender scope.
    /// </summary>
    public string? SchemeName { get; set; }

    /// <summary>
    /// Packaging category required by the scheme (optional).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// If true, missing scheme info should trigger Manual Review.
    /// </summary>
    public bool TriggerManualReviewIfMissing { get; set; } = true;
}
