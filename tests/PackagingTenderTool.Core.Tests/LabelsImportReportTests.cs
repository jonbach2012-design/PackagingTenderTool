using ClosedXML.Excel;
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Tests;

public sealed class LabelsImportReportTests
{
    [Fact]
    public void ImportTenderWithReportDetectsHeaderSkipsSummaryRowsAndReturnsCleanedRows()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Tender data");
        worksheet.Cell(1, 1).Value = "Intro";
        var headers = new[]
        {
            "Item no", "Item name", "Supplier name", "DSH Site", "Quantity", "Spend (NOK)",
            "Price per 1,000", "Price (DKK)", "Theoretical spend (NOK)", "Label size",
            "Winding direction", "Material", "Reel diameter / pcs per roll", "No. of colors", "Comment"
        };
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(3, index + 1).Value = headers[index];
        }

        AddRow(worksheet, 4, "540119", "Item A", "FLEXOPRINT AS", "Jæren", "1.533.600", "282.468 kr", "184,19 kr", "117,75 kr", "290.592 kr", "100X169", "OUT Bottom first", "PP top white", "3.500", "5 to 6", "DONE");
        AddRow(worksheet, 5, "540536", "Item B", "FLEXOPRINT AS", "Stokke", "15.000", "4.691 kr", "312,76 kr", "197,68 kr", "4.772 kr", "74,2X219", "OUT Head first", "Thermo top", "1.000", "4", "DONE");
        worksheet.Cell(6, 1).Value = "Summary";
        worksheet.Cell(6, 6).Value = "287.159 kr";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = new LabelsExcelImportService().ImportTenderWithReport(stream, "DSH");

        Assert.Equal("Tender data", result.Summary.WorksheetName);
        Assert.Equal(3, result.Summary.HeaderRowNumber);
        Assert.Equal(3, result.Summary.TotalRowsScanned);
        Assert.Equal(2, result.Summary.ImportedRows);
        Assert.Equal(1, result.Summary.SkippedRows);
        Assert.Equal(1, result.Summary.SupplierCount);
        Assert.Equal(2, result.Summary.SiteCount);
        Assert.Equal(2, result.Summary.SizeCount);
        Assert.Equal(2, result.Summary.MaterialCount);
        Assert.Equal(287159m, result.Summary.TotalSpend);
        Assert.Equal(2, result.RawRows.Count);
        Assert.Equal("100x169", result.CleanedRows[0].NormalizedLabelSize);
        Assert.Equal("74.2x219", result.CleanedRows[1].NormalizedLabelSize);
        Assert.NotNull(result.ValidationReport);
        var colorsIssue = result.ValidationReport.Issues.FirstOrDefault(i => i.RawValue == "5 to 6");
        Assert.NotNull(colorsIssue);
        Assert.Equal(4, colorsIssue!.RowNumber);
        Assert.Equal("No. of colors", colorsIssue.ColumnName);
        Assert.Equal(ImportValidationIssueType.ManualReviewRequired, colorsIssue.IssueType);
        Assert.Contains("converted to 6", colorsIssue.Message, StringComparison.OrdinalIgnoreCase);
        var firstLine = result.Tender.LabelLineItems.First(i => i.ItemNo == "540119");
        Assert.Equal(6, firstLine.NumberOfColors);
        Assert.Equal("5 to 6", firstLine.OriginalColorsValue);
    }

    [Fact]
    public void ImportAndEvaluateCarriesImportSummaryAndAnalytics()
    {
        using var stream = CreateWorkbookStream();

        var result = new LabelsTenderEvaluationService()
            .ImportAndEvaluate(stream, "DSH");

        Assert.NotNull(result.ImportSummary);
        Assert.NotNull(result.Analytics);
        Assert.Equal(2, result.ImportSummary.ImportedRows);
        Assert.Equal(287159m, result.Analytics.TotalSpend);
        Assert.Contains(result.Analytics.SpendBySite, item => item.Name == "Jæren");
    }

    private static MemoryStream CreateWorkbookStream()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Tender data");
        var headers = new[]
        {
            "Item no", "Item name", "Supplier name", "DSH Site", "Quantity", "Spend (NOK)",
            "Price per 1,000", "Price (DKK)", "Theoretical spend (NOK)", "Label size",
            "Winding direction", "Material", "Reel diameter / pcs per roll", "No. of colors", "Comment"
        };
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        AddRow(worksheet, 2, "540119", "Item A", "FLEXOPRINT AS", "Jæren", "1.533.600", "282.468 kr", "184,19 kr", "117,75 kr", "290.592 kr", "100X169", "OUT Bottom first", "PP top white", "3.500", "5 to 6", "DONE");
        AddRow(worksheet, 3, "540536", "Item B", "FLEXOPRINT AS", "Stokke", "15.000", "4.691 kr", "312,76 kr", "197,68 kr", "4.772 kr", "74,2X219", "OUT Head first", "Thermo top", "1.000", "4", "DONE");

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static void AddRow(IXLWorksheet worksheet, int rowNumber, params object[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            worksheet.Cell(rowNumber, index + 1).Value = XLCellValue.FromObject(values[index]);
        }
    }
}
