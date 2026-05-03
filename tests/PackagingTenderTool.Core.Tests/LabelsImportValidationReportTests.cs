using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Tests;

public sealed class LabelsImportValidationReportTests
{
    [Fact]
    public void Colors_range_5_to_6_parses_to_upper_bound_with_manual_review_warning_and_original_value()
    {
        using var stream = LabelsExcelImportServiceTests.CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000", "No. of colors"],
            [
                ["LBL-C1", "It", "PP", "Sup", 1m, 10m, 5m, "5 to 6"]
            ]);

        var result = new LabelsExcelImportService().ImportTenderWithReport(stream, "T");
        Assert.True(result.ImportCommitted);
        var line = result.Tender.LabelLineItems.Single();
        Assert.Equal(6, line.NumberOfColors);
        Assert.Equal("5 to 6", line.OriginalColorsValue);

        var issue = result.ValidationReport!.Issues.First(i =>
            i.IssueType == ImportValidationIssueType.ManualReviewRequired
            && i.RawValue == "5 to 6");
        Assert.Equal(2, issue.RowNumber);
        Assert.Equal("No. of colors", issue.ColumnName);
        Assert.Equal(ImportValidationSeverity.Warning, issue.Severity);
        Assert.Contains("converted to 6", issue.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("issue-warning", issue.SeverityClass);
    }

    [Fact]
    public void Invalid_decimal_in_price_surfaces_invalid_cell_value_issue()
    {
        using var stream = LabelsExcelImportServiceTests.CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000", "Price"],
            [
                ["LBL-P1", "It", "PP", "Sup", 1m, 10m, 5m, "not-a-price"]
            ]);

        var result = new LabelsExcelImportService().ImportTenderWithReport(stream, "T");
        Assert.True(result.ImportCommitted);
        var priceIssues = result.ValidationReport!.Issues
            .Where(i => i.IssueType == ImportValidationIssueType.InvalidCellValue && i.ColumnName == "Price")
            .ToList();
        Assert.Single(priceIssues);
        var issue = priceIssues[0];
        Assert.Equal(2, issue.RowNumber);
        Assert.Equal("not-a-price", issue.RawValue);
        Assert.Equal(ImportValidationSeverity.Error, issue.Severity);
        Assert.Equal("issue-error", issue.SeverityClass);
    }

    [Fact]
    public void Blank_required_supplier_blocks_import_and_creates_empty_required_issue()
    {
        using var stream = LabelsExcelImportServiceTests.CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000"],
            [
                ["LBL-B1", "It", "PP", "", 1m, 10m, 5m]
            ]);

        var result = new LabelsExcelImportService().ImportTenderWithReport(stream, "T");
        Assert.False(result.ImportCommitted);
        Assert.False(result.ValidationReport!.Success);
        Assert.Empty(result.Tender.LabelLineItems);
        var blocking = result.ValidationReport.Issues.First(i => i.BlocksImport);
        Assert.Equal(ImportValidationIssueType.EmptyRequiredCell, blocking.IssueType);
        Assert.Equal("Supplier name", blocking.ColumnName);
        Assert.Equal(2, blocking.RowNumber);
    }

    [Fact]
    public void Blank_site_with_column_emits_manual_review_warning()
    {
        using var stream = LabelsExcelImportServiceTests.CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Site", "Quantity", "Spend", "Price per 1,000"],
            [
                ["LBL-S1", "It", "PP", "Sup", "", 1m, 10m, 5m]
            ]);

        var result = new LabelsExcelImportService().ImportTenderWithReport(stream, "T");
        Assert.True(result.ImportCommitted);
        var warn = result.ValidationReport!.Issues.First(i =>
            i.ColumnName == "Site" && i.IssueType == ImportValidationIssueType.ManualReviewRequired);
        Assert.Equal(ImportValidationSeverity.Warning, warn.Severity);
        Assert.Equal(2, warn.RowNumber);
    }

    [Fact]
    public void Invalid_whole_number_non_range_emits_invalid_cell_issue_with_metadata()
    {
        using var stream = LabelsExcelImportServiceTests.CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000", "No. of colors"],
            [
                ["LBL-X", "It", "PP", "Sup", 1m, 10m, 5m, 2.5m]
            ]);

        var result = new LabelsExcelImportService().ImportTenderWithReport(stream, "T");
        var issue = result.ValidationReport!.Issues.First(i =>
            i.IssueType == ImportValidationIssueType.InvalidCellValue
            && i.ColumnName == "No. of colors");
        Assert.Equal(2, issue.RowNumber);
        // ClosedXML / Excel formatting may render the stored decimal as "2,5" or "2.5" depending on locale.
        Assert.True(issue.RawValue is "2.5" or "2,5", $"Unexpected raw colors cell text: '{issue.RawValue}'");
        Assert.Contains("whole number", issue.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Valid_import_after_blocked_import_commits_lines()
    {
        using var bad = LabelsExcelImportServiceTests.CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000"],
            [["LBL-B1", "It", "PP", "", 1m, 10m, 5m]]);
        var badResult = new LabelsExcelImportService().ImportTenderWithReport(bad, "T");
        Assert.False(badResult.ImportCommitted);

        using var good = LabelsExcelImportServiceTests.CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000"],
            [["LBL-G1", "It", "PP", "Sup", 1m, 10m, 5m]]);
        var goodResult = new LabelsExcelImportService().ImportTenderWithReport(good, "T");
        Assert.True(goodResult.ImportCommitted);
        Assert.Single(goodResult.Tender.LabelLineItems);
    }
}
