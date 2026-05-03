using System.Linq;

namespace PackagingTenderTool.Core.Import;

/// <summary>Structured outcome of a Labels Excel import.</summary>
public sealed class ImportValidationReport
{
    /// <summary>True when the tender was committed (no blocking row validation).</summary>
    public bool Success { get; set; }

    public bool HasFatalErrors => Issues.Any(i => i.Severity == ImportValidationSeverity.Fatal);

    public bool HasErrors => Issues.Any(i => i.Severity == ImportValidationSeverity.Error);

    public bool HasWarnings => Issues.Any(i => i.Severity == ImportValidationSeverity.Warning);

    public bool HasBlockingImport => Issues.Any(i => i.BlocksImport);

    /// <summary>CSS classes for summary banner (no severity branching in Razor).</summary>
    public string? SummaryBannerClass =>
        !Success || HasBlockingImport
            ? "import-validation-title import-validation-title--error"
            : HasErrors || HasWarnings
                ? "import-validation-title import-validation-title--warning"
                : null;

    public string SummaryBannerText =>
        !Success || HasBlockingImport
            ? "Import blocked — fix issues below and try again."
            : HasErrors || HasWarnings
                ? "Imported with warnings — review the list below."
                : string.Empty;

    public int RowsAttempted { get; set; }

    public int RowsImported { get; set; }

    public string SheetName { get; set; } = string.Empty;

    public int HeaderRowNumber { get; set; }

    public IReadOnlyList<ImportValidationIssue> Issues { get; set; } = [];

    /// <summary>Stable display order for UI tables (row, then column name).</summary>
    public IReadOnlyList<ImportValidationIssue> GetIssuesOrderedForDisplay() =>
        Issues
            .OrderBy(static i => i.RowNumber ?? int.MaxValue)
            .ThenBy(static i => i.ColumnName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static ImportValidationReport Create(
        string sheetName,
        int headerRowNumber,
        int rowsAttempted,
        int rowsImported,
        IReadOnlyList<ImportValidationIssue> issues,
        bool importCommitted)
    {
        var list = issues.ToArray();
        return new ImportValidationReport
        {
            SheetName = sheetName,
            HeaderRowNumber = headerRowNumber,
            RowsAttempted = rowsAttempted,
            RowsImported = rowsImported,
            Issues = list,
            Success = importCommitted && !list.Any(i => i.Severity == ImportValidationSeverity.Fatal)
        };
    }
}
