namespace PackagingTenderTool.Core.Models;

public sealed class ManualReviewFlag
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Reason { get; set; } = string.Empty;

    public string? FieldName { get; set; }

    public string? SourceValue { get; set; }

    public ManualReviewSeverity Severity { get; set; } = ManualReviewSeverity.Warning;

    /// <summary>Optional hint shown in import validation UI (e.g. how to fix the cell).</summary>
    public string? SuggestedAction { get; set; }
}

public enum ManualReviewSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3
}
