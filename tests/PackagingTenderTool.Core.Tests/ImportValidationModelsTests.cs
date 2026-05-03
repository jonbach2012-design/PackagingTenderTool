using PackagingTenderTool.Core.Import;

namespace PackagingTenderTool.Core.Tests;

public sealed class ImportValidationModelsTests
{
    public static TheoryData<ImportValidationSeverity, string> SeverityClassCases => new()
    {
        { ImportValidationSeverity.Fatal, "issue-fatal" },
        { ImportValidationSeverity.Error, "issue-error" },
        { ImportValidationSeverity.Warning, "issue-warning" },
        { ImportValidationSeverity.Info, "issue-info" }
    };

    [Theory]
    [MemberData(nameof(SeverityClassCases))]
    public void ImportValidationIssue_SeverityClass_maps_to_css_token(
        ImportValidationSeverity severity,
        string expectedClass)
    {
        var issue = new ImportValidationIssue { Severity = severity };
        Assert.Equal(expectedClass, issue.SeverityClass);
    }

    [Fact]
    public void ImportValidationIssue_SeverityClass_unknown_enum_falls_back_to_info()
    {
        var issue = new ImportValidationIssue { Severity = (ImportValidationSeverity)999 };
        Assert.Equal("issue-info", issue.SeverityClass);
    }

    [Fact]
    public void ImportValidationIssue_stores_metadata_for_display_and_policy()
    {
        var issue = new ImportValidationIssue
        {
            Severity = ImportValidationSeverity.Error,
            IssueType = ImportValidationIssueType.InvalidCellValue,
            RowNumber = 14,
            ColumnName = "Price",
            RawValue = "12,50x",
            Message = "Row 14, Price: Value '12,50x' is not a valid number.",
            SuggestedAction = "Replace with a numeric cell.",
            BlocksImport = false
        };

        Assert.Equal(ImportValidationSeverity.Error, issue.Severity);
        Assert.Equal(ImportValidationIssueType.InvalidCellValue, issue.IssueType);
        Assert.Equal(14, issue.RowNumber);
        Assert.Equal("Price", issue.ColumnName);
        Assert.Equal("12,50x", issue.RawValue);
        Assert.Contains("Row 14", issue.Message, StringComparison.Ordinal);
        Assert.Equal("Replace with a numeric cell.", issue.SuggestedAction);
        Assert.False(issue.BlocksImport);
        Assert.Equal("issue-error", issue.SeverityClass);
    }

    [Fact]
    public void ImportValidationReport_Create_sets_sheet_counts_and_success_without_fatal()
    {
        var issues = new List<ImportValidationIssue>
        {
            new()
            {
                Severity = ImportValidationSeverity.Warning,
                IssueType = ImportValidationIssueType.ManualReviewRequired,
                Message = "Review spend."
            }
        };

        var report = ImportValidationReport.Create("Data", 3, 10, 10, issues, importCommitted: true);

        Assert.Equal("Data", report.SheetName);
        Assert.Equal(3, report.HeaderRowNumber);
        Assert.Equal(10, report.RowsAttempted);
        Assert.Equal(10, report.RowsImported);
        Assert.Single(report.Issues);
        Assert.Equal(ImportValidationIssueType.ManualReviewRequired, report.Issues[0].IssueType);
        Assert.True(report.Success);
        Assert.False(report.HasFatalErrors);
        Assert.False(report.HasErrors);
        Assert.True(report.HasWarnings);
        Assert.False(report.HasBlockingImport);
    }

    [Fact]
    public void ImportValidationReport_Create_success_false_when_fatal_issue_even_if_committed_flag_true()
    {
        var issues = new[]
        {
            new ImportValidationIssue
            {
                Severity = ImportValidationSeverity.Fatal,
                IssueType = ImportValidationIssueType.WorkbookOpenFailed,
                Message = "Could not open file."
            }
        };

        var report = ImportValidationReport.Create("Sheet1", 1, 0, 0, issues, importCommitted: true);

        Assert.False(report.Success);
        Assert.True(report.HasFatalErrors);
    }

    [Fact]
    public void ImportValidationReport_GetIssuesOrderedForDisplay_sorts_by_row_then_column()
    {
        var report = new ImportValidationReport
        {
            Issues =
            [
                new ImportValidationIssue { RowNumber = 5, ColumnName = "Zed", Message = "b" },
                new ImportValidationIssue { RowNumber = 2, ColumnName = "Price", Message = "a" },
                new ImportValidationIssue { RowNumber = 2, ColumnName = "Quantity", Message = "c" }
            ]
        };

        var ordered = report.GetIssuesOrderedForDisplay();
        Assert.Equal(2, ordered[0].RowNumber);
        Assert.Equal("Price", ordered[0].ColumnName);
        Assert.Equal("Quantity", ordered[1].ColumnName);
        Assert.Equal(5, ordered[2].RowNumber);
    }

    [Fact]
    public void ImportValidationReport_HasBlockingImport_reflects_issue_flag()
    {
        var report = new ImportValidationReport
        {
            Success = true,
            Issues =
            [
                new ImportValidationIssue
                {
                    Severity = ImportValidationSeverity.Error,
                    IssueType = ImportValidationIssueType.EmptyRequiredCell,
                    BlocksImport = true,
                    Message = "Missing supplier."
                }
            ]
        };

        Assert.True(report.HasBlockingImport);
        Assert.True(report.HasErrors);
        Assert.Contains("import-validation-title--error", report.SummaryBannerClass ?? "", StringComparison.Ordinal);
    }
}
