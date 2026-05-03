namespace PackagingTenderTool.Core.Import;

/// <summary>One user-visible import finding.</summary>
public sealed class ImportValidationIssue
{
    public ImportValidationSeverity Severity { get; set; }

    public ImportValidationIssueType IssueType { get; set; }

    public int? RowNumber { get; set; }

    /// <summary>Spreadsheet column header text (user-facing).</summary>
    public string? ColumnName { get; set; }

    public string? RawValue { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? SuggestedAction { get; set; }

    /// <summary>When true, the workbook must not replace the in-app tender until fixed.</summary>
    public bool BlocksImport { get; set; }

    /// <summary>CSS class for list rows (no hex in Razor).</summary>
    public string SeverityClass => Severity switch
    {
        ImportValidationSeverity.Fatal => "issue-fatal",
        ImportValidationSeverity.Error => "issue-error",
        ImportValidationSeverity.Warning => "issue-warning",
        ImportValidationSeverity.Info => "issue-info",
        _ => "issue-info"
    };
}
