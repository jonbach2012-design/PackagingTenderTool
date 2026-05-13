using ClosedXML.Excel;
using PackagingTenderTool.Core.Import;

namespace PackagingTenderTool.Core.Tests;

public sealed class PivotLabelsImportServiceTests
{
    private const string PivotFixtureFileName = "pivot-labels-all-dsh.xlsx";

    private static readonly string[] AllowedSuppliers =
        ["Flexoprint", "Norsk Etikett", "Grafiket", "Ettiketto"];

    static PivotLabelsImportServiceTests()
    {
        EnsureDefaultPivotFixtureIfMissing();
    }

    [Fact]
    public void Pivot_fixture_import_commits()
    {
        var path = ResolveImportVerificationFixture(PivotFixtureFileName);
        using var stream = File.OpenRead(path);
        var result = new PivotLabelsExcelImportService().ImportTenderWithReport(stream, "Pivot tender");

        Assert.True(result.ImportCommitted);
    }

    [Fact]
    public void Pivot_fixture_line_item_count_matches_non_empty_supplier_prices()
    {
        var path = ResolveImportVerificationFixture(PivotFixtureFileName);
        var expected = CountExpectedLongRowsFromPivot(path);

        using var stream = File.OpenRead(path);
        var result = new PivotLabelsExcelImportService().ImportTenderWithReport(stream, "Pivot tender");

        Assert.Equal(expected, result.Tender.LabelLineItems.Count);
    }

    [Fact]
    public void Pivot_fixture_supplier_names_are_only_the_four_literals()
    {
        var path = ResolveImportVerificationFixture(PivotFixtureFileName);
        using var stream = File.OpenRead(path);
        var result = new PivotLabelsExcelImportService().ImportTenderWithReport(stream, "Pivot tender");

        Assert.All(result.Tender.LabelLineItems, line =>
        {
            Assert.Contains(line.SupplierName, AllowedSuppliers);
        });
    }

    [Fact]
    public void In_memory_pivot_skips_suppliers_with_empty_price()
    {
        using var stream = CreatePivotStreamWithOneRowTwoPricedTwoEmptyPrices();
        var result = new PivotLabelsExcelImportService().ImportTenderWithReport(stream, "In-memory pivot");

        Assert.True(result.ImportCommitted);
        Assert.Equal(2, result.Tender.LabelLineItems.Count);
        Assert.Equal("Flexoprint", result.Tender.LabelLineItems[0].SupplierName);
        Assert.Equal("Norsk Etikett", result.Tender.LabelLineItems[1].SupplierName);
    }

    [Fact]
    public void Pivot_fixture_distinct_sites_include_jaeren_and_stokke()
    {
        var path = ResolveImportVerificationFixture(PivotFixtureFileName);
        using var stream = File.OpenRead(path);
        var result = new PivotLabelsExcelImportService().ImportTenderWithReport(stream, "Pivot tender");

        var sites = result.Tender.LabelLineItems
            .Select(l => l.Site)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Jæren", sites);
        Assert.Contains("Stokke", sites);
    }

    private static MemoryStream CreatePivotStreamWithOneRowTwoPricedTwoEmptyPrices()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(PivotLabelsExcelImportService.PivotSheetName);
        for (var c = 1; c <= 22; c++)
        {
            ws.Cell(1, c).Value = $"H{c}";
        }

        var r = 2;
        ws.Cell(r, 1).Value = "T-INMEM-1";
        ws.Cell(r, 2).Value = "In-memory label";
        ws.Cell(r, 3).Value = "NO";
        ws.Cell(r, 4).Value = 1000m;
        ws.Cell(r, 5).Value = "50x30";
        ws.Cell(r, 6).Value = "CW";
        ws.Cell(r, 7).Value = "PP";
        ws.Cell(r, 8).Value = "400";
        ws.Cell(r, 9).Value = 3;
        ws.Cell(r, 10).Value = "5000";
        ws.Cell(r, 11).Value = 11.5m;
        ws.Cell(r, 12).Value = "L1";
        ws.Cell(r, 13).Value = "C1";
        ws.Cell(r, 14).Value = 22.5m;
        ws.Cell(r, 15).Value = "L2";
        ws.Cell(r, 16).Value = "C2";
        // Q–V left empty → Grafiket + Ettiketto skipped

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static int CountExpectedLongRowsFromPivot(string pivotPath)
    {
        using var wb = new XLWorkbook(pivotPath);
        var ws = wb.Worksheet(PivotLabelsExcelImportService.PivotSheetName);
        var last = ws.LastRowUsed()?.RowNumber() ?? 0;
        var sum = 0;
        for (var row = 2; row <= last; row++)
        {
            var itemNo = TrimmedText(ws, row, 1);
            var itemName = TrimmedText(ws, row, 2);
            if (string.IsNullOrEmpty(itemNo) && string.IsNullOrEmpty(itemName))
            {
                continue;
            }

            if (HasNonEmptyPrice(ws, row, 11))
            {
                sum++;
            }

            if (HasNonEmptyPrice(ws, row, 14))
            {
                sum++;
            }

            if (HasNonEmptyPrice(ws, row, 17))
            {
                sum++;
            }

            if (HasNonEmptyPrice(ws, row, 20))
            {
                sum++;
            }
        }

        return sum;
    }

    private static string? TrimmedText(IXLWorksheet sheet, int row, int column)
    {
        var cell = sheet.Cell(row, column);
        if (cell.IsEmpty())
        {
            return null;
        }

        var s = cell.GetFormattedString().Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static bool HasNonEmptyPrice(IXLWorksheet sheet, int row, int column)
    {
        var cell = sheet.Cell(row, column);
        if (cell.IsEmpty())
        {
            return false;
        }

        if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out _))
        {
            return true;
        }

        var text = cell.GetFormattedString().Trim();
        return text.Length > 0;
    }

    private static void EnsureDefaultPivotFixtureIfMissing()
    {
        var dir = GetOrCreateImportVerificationDirectory();
        var path = Path.Combine(dir, PivotFixtureFileName);
        if (File.Exists(path))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(PivotLabelsExcelImportService.PivotSheetName);
        for (var c = 1; c <= 22; c++)
        {
            ws.Cell(1, c).Value = $"Col{c}";
        }

        WriteDataRow(ws, 2, "PIV-001", "Alpha", "Jæren", 5000, 100m, 200m, 300m, 400m);
        WriteDataRow(ws, 3, "PIV-002", "Beta", "Stokke", 3000, 10m, decimal.Zero, 20m, decimal.Zero);
        WriteDataRow(ws, 4, "PIV-003", "Gamma", "Jæren", 1200, decimal.Zero, 5m, decimal.Zero, 7m);

        wb.SaveAs(path);
    }

    private static void WriteDataRow(
        IXLWorksheet ws,
        int row,
        string itemNo,
        string itemName,
        string site,
        decimal quantity,
        decimal flexPrice,
        decimal norskPrice,
        decimal grafPrice,
        decimal ettiPrice)
    {
        ws.Cell(row, 1).Value = itemNo;
        ws.Cell(row, 2).Value = itemName;
        ws.Cell(row, 3).Value = site;
        ws.Cell(row, 4).Value = quantity;
        ws.Cell(row, 5).Value = "100x50";
        ws.Cell(row, 6).Value = "CW";
        ws.Cell(row, 7).Value = "PP";
        ws.Cell(row, 8).Value = "330";
        ws.Cell(row, 9).Value = 2;
        ws.Cell(row, 10).Value = "10000";
        WriteOptionalPrice(ws, row, 11, flexPrice);
        WriteOptionalPrice(ws, row, 14, norskPrice);
        WriteOptionalPrice(ws, row, 17, grafPrice);
        WriteOptionalPrice(ws, row, 20, ettiPrice);
    }

    private static void WriteOptionalPrice(IXLWorksheet ws, int row, int col, decimal price)
    {
        if (price == decimal.Zero)
        {
            return;
        }

        ws.Cell(row, col).Value = price;
    }

    private static string GetOrCreateImportVerificationDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "testdata", "import-verification");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            var sln = Path.Combine(dir.FullName, "PackagingTenderTool.sln");
            if (File.Exists(sln))
            {
                Directory.CreateDirectory(candidate);
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate or create testdata/import-verification (search upward from {AppContext.BaseDirectory}).");
    }

    private static string ResolveImportVerificationFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, "testdata", "import-verification", fileName);
            if (File.Exists(path))
            {
                return path;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate testdata/import-verification/{fileName} (search upward from {AppContext.BaseDirectory}).");
    }
}
