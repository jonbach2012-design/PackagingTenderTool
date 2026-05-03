using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Import;

public sealed class LabelsTenderImportResult
{
    public Tender Tender { get; set; } = new();

    public LabelsImportSummary Summary { get; set; } = new();

    public List<RawLabelTenderRow> RawRows { get; set; } = [];

    public List<CleanedLabelLineItem> CleanedRows { get; set; } = [];

    public List<ImportValidationIssue> Issues { get; set; } = [];

    /// <summary>Structured validation view (aligned with <see cref="Issues"/>).</summary>
    public ImportValidationReport? ValidationReport { get; set; }

    /// <summary>False when blocking row issues require fixes before the tender is replaced.</summary>
    public bool ImportCommitted { get; set; } = true;

    public bool HasErrors => Issues.Any(issue => issue.Severity == ImportValidationSeverity.Error);
}
