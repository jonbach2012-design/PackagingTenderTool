using ClosedXML.Excel;
using PackagingTenderTool.Core.Import;

namespace PackagingTenderTool.Core.Tests;

/// <summary>
/// Automated checks for <c>testdata/import-verification</c> workbooks used in manual QA and import error tests.
/// </summary>
public sealed class LabelsImportVerificationFixtureTests
{
    private static readonly string[] ItemNoHeaderAliases = ["Item no", "Item no.", "Item number"];
    private static readonly string[] SupplierNameHeaderAliases = ["Supplier name", "Supplier"];

    [Fact]
    public void Bad_missing_required_supplier_xlsx_is_valid_excel_with_item_no_but_no_supplier_header_and_data_rows()
    {
        var path = ResolveImportVerificationFixture("bad-missing-required-supplier.xlsx");

        using var workbook = new XLWorkbook(path);
        Assert.NotEmpty(workbook.Worksheets);

        var worksheet = workbook.Worksheets.First();
        var headerTexts = ReadHeaderRowTexts(worksheet);
        Assert.NotEmpty(headerTexts);

        Assert.Contains(headerTexts, IsItemNoHeader);
        Assert.Contains(headerTexts, h => string.Equals(h, "Item name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(headerTexts, h => string.Equals(h, "Material", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headerTexts, IsSupplierNameHeader);

        var firstDataRow = worksheet.RowsUsed().FirstOrDefault(r => r.RowNumber() > 1);
        Assert.NotNull(firstDataRow);
        Assert.True(firstDataRow.CellsUsed().Any(), "Expected at least one populated cell in a data row below the header.");
    }

    [Fact]
    public void Bad_unrecognizable_labels_header_import_formats_as_header_not_workbook_read_failure()
    {
        var path = ResolveImportVerificationFixture("bad-unrecognizable-labels-header.xlsx");
        var importer = new LabelsExcelImportService();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var stream = File.OpenRead(path);
            _ = importer.ImportTender(stream, "Unrecognizable");
        });

        Assert.Equal(LabelsExcelImportService.HeaderNotRecognizedMarker, ex.Message);

        var userFacing = LabelTenderImportFailureMessage.Format(ex);
        Assert.Contains("workbook opened", userFacing, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("header could not be recognized", userFacing, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("could not be read", userFacing, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bad_unrecognizable_labels_header_xlsx_opens_and_has_no_item_no_header_but_has_data()
    {
        var path = ResolveImportVerificationFixture("bad-unrecognizable-labels-header.xlsx");

        using var workbook = new XLWorkbook(path);
        Assert.NotEmpty(workbook.Worksheets);

        var worksheet = workbook.Worksheets.First();
        var headerTexts = ReadHeaderRowTexts(worksheet);
        Assert.NotEmpty(headerTexts);
        Assert.DoesNotContain(headerTexts, IsItemNoHeader);

        var firstDataRow = worksheet.RowsUsed().FirstOrDefault(r => r.RowNumber() > 1);
        Assert.NotNull(firstDataRow);
        Assert.True(firstDataRow.CellsUsed().Any(), "Expected at least one data row (unrecognizable header is not an empty sheet).");
    }

    [Fact]
    public void Bad_missing_required_supplier_xlsx_import_hits_missing_column_path_and_formats_user_message()
    {
        var path = ResolveImportVerificationFixture("bad-missing-required-supplier.xlsx");
        var importer = new LabelsExcelImportService();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var stream = File.OpenRead(path);
            _ = importer.ImportTender(stream, "Fixture test tender");
        });

        Assert.StartsWith(LabelsExcelImportService.MissingRequiredColumnMarker + ":", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Supplier name", ex.Message, StringComparison.Ordinal);

        var userFacing = LabelTenderImportFailureMessage.Format(ex);
        Assert.Contains("Import failed:", userFacing, StringComparison.Ordinal);
        Assert.Contains("Missing required column", userFacing, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Supplier name", userFacing, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> ReadHeaderRowTexts(IXLWorksheet worksheet)
    {
        var firstRow = worksheet.FirstRowUsed();
        Assert.NotNull(firstRow);

        return firstRow.CellsUsed()
            .Select(c => c.GetString().Trim())
            .Where(s => s.Length > 0)
            .ToList();
    }

    private static bool IsItemNoHeader(string cellText) =>
        ItemNoHeaderAliases.Any(a => string.Equals(a, cellText, StringComparison.OrdinalIgnoreCase));

    private static bool IsSupplierNameHeader(string cellText) =>
        SupplierNameHeaderAliases.Any(a => string.Equals(a, cellText, StringComparison.OrdinalIgnoreCase));

    private static string ResolveImportVerificationFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var path = Path.Combine(dir.FullName, "testdata", "import-verification", fileName);
            if (File.Exists(path))
                return path;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate testdata/import-verification/{fileName} (search upward from {AppContext.BaseDirectory}).");
    }
}
