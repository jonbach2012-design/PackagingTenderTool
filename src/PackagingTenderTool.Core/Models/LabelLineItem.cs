namespace PackagingTenderTool.Core.Models;

public sealed class LabelLineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? ItemNo { get; set; }

    public string? ItemName { get; set; }

    public string? SupplierName { get; set; }

    public string? Site { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? Spend { get; set; }

    public decimal? PricePerThousand { get; set; }

    public decimal? Price { get; set; }

    public decimal? TheoreticalSpend { get; set; }

    /// <summary>
    /// Optional technical rating (0-100) from supplier/procurement input for CTR scoring.
    /// </summary>
    public decimal? TechnicalRating { get; set; }

    public string? LabelSize { get; set; }

    public string? WindingDirection { get; set; }

    public string? Material { get; set; }

    public string? ReelDiameterOrPcsPerRoll { get; set; }

    public int? NumberOfColors { get; set; }

    public decimal? LabelWeightGrams { get; set; }

    // Normalization 2.0: split taxable mass (Scandi Standard / Kronfågel fallback ratios).
    public decimal? FrontWeightGrams { get; set; }

    public decimal? AdhesiveWeightGrams { get; set; }

    public decimal? BackingWeightGrams { get; set; }

    public bool? IsMonoMaterial { get; set; }

    public bool? IsEasyToSeparate { get; set; }

    public bool? IsReusableOrRecyclableMaterial { get; set; }

    public bool? HasTraceability { get; set; }

    // PPWR / EPR (Regulatory) - structured input fields used for assessment & scoring.
    // Kept nullable to support partial/uncertain tender data (Manual Review flow).

    /// <summary>
    /// Material composition of the label (fractions should typically sum to 100).
    /// </summary>
    public List<MaterialFraction> MaterialFractions { get; set; } = [];

    /// <summary>
    /// Optional recyclability/recycling grade for the specific line item, if provided by supplier or derived.
    /// </summary>
    public RecyclingGrade? RecyclingGrade { get; set; }

    /// <summary>
    /// Recycled content percentage (0-100) for the line item material (overall).
    /// </summary>
    public decimal? RecycledContentPercent { get; set; }

    /// <summary>
    /// rPET content percentage (0-100) when PET is relevant (optional, profile-dependent).
    /// </summary>
    public decimal? RecycledPetContentPercent { get; set; }

    /// <summary>
    /// Optional recycling stream/category (e.g. plastic/paper), if relevant for PPWR/EPR interpretation.
    /// </summary>
    public string? RecyclingStream { get; set; }

    /// <summary>
    /// EPR scheme(s) applicable to this line item (can vary by market/country/site).
    /// </summary>
    public List<EprSchemeInfo> EprSchemes { get; set; } = [];

    public List<ManualReviewFlag> SourceManualReviewFlags { get; set; } = [];

    public string? Comment { get; set; }
}

public sealed class MaterialFraction
{
    /// <summary>
    /// Material identifier as provided/normalized (e.g. "PP", "PET", "Paper").
    /// </summary>
    public string Material { get; set; } = string.Empty;

    /// <summary>
    /// Share of total composition in percent (0-100).
    /// </summary>
    public decimal? Percent { get; set; }
}

public enum RecyclingGrade
{
    Unknown = 0,
    A = 1,
    B = 2,
    C = 3,
    D = 4,
    E = 5
}

public sealed class EprSchemeInfo
{
    /// <summary>
    /// Country/market code the scheme applies to (e.g. "DK", "SE").
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Scheme/PRO name (e.g. a national EPR scheme or producer responsibility organization).
    /// </summary>
    public string? SchemeName { get; set; }

    /// <summary>
    /// Packaging category within the scheme, if applicable (e.g. "Packaging", "Labels", "Plastic packaging").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Optional eco-modulation / fee class code, if provided (scheme-specific).
    /// </summary>
    public string? ModulationClass { get; set; }

    /// <summary>
    /// Optional compliance/documentation reference (certificate ID, link, or statement).
    /// </summary>
    public string? ComplianceReference { get; set; }
}
