namespace PackagingTenderTool.Blazor.Models.LabelTender;

/// <summary>Distinct filter values derived from the loaded tender supplier set.</summary>
public sealed record TenderFilterOptions
{
    public required IReadOnlyList<string> Countries { get; init; }

    public required IReadOnlyList<string> Sites { get; init; }

    public required IReadOnlyList<string> Materials { get; init; }

    public required IReadOnlyList<string> Adhesives { get; init; }
}
