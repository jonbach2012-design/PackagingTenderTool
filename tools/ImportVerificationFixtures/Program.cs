using ClosedXML.Excel;

// Writes local files for manual import recovery QA (see testdata/import-verification/README.md).
var outDir = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "testdata", "import-verification"));

Directory.CreateDirectory(outDir);

// Renamed fixture: avoid stale filename from earlier runs.
foreach (var legacy in new[] { "bad-missing-itemno.xlsx" })
{
    var legacyPath = Path.Combine(outDir, legacy);
    if (File.Exists(legacyPath))
        File.Delete(legacyPath);
}

var txtPath = Path.Combine(outDir, "not-excel.txt");
File.WriteAllText(txtPath, "This is plain text, not an Excel workbook.\r\n");

// Recognizable Labels sheet shape, but no column that maps to Item no → FindHeaderRow fails.
var unrecognizablePath = Path.Combine(outDir, "bad-unrecognizable-labels-header.xlsx");
using (var wb = new XLWorkbook())
{
    var ws = wb.Worksheets.Add("Sheet1");
    ws.Cell(1, 1).Value = "Supplier name";
    ws.Cell(1, 2).Value = "Site";
    ws.Cell(1, 3).Value = "Quantity";
    ws.Cell(2, 1).Value = "Acme Labels";
    ws.Cell(2, 2).Value = "DK01";
    ws.Cell(2, 3).Value = 1000m;
    wb.SaveAs(unrecognizablePath);
}

// Labels identity header (Item no, Item name, Material, quantity, price-like) but no Supplier name column → missing required column.
var missingSupplierPath = Path.Combine(outDir, "bad-missing-required-supplier.xlsx");
using (var wb = new XLWorkbook())
{
    var ws = wb.Worksheets.Add("Labels");
    var headers = new[] { "Item no", "Item name", "Material", "Site", "Quantity", "Spend", "Price per 1,000" };
    for (var i = 0; i < headers.Length; i++)
        ws.Cell(1, i + 1).Value = headers[i];

    ws.Cell(2, 1).Value = "BAD-001";
    ws.Cell(2, 2).Value = "Part";
    ws.Cell(2, 3).Value = "PP";
    ws.Cell(2, 4).Value = "DK01";
    ws.Cell(2, 5).Value = 1000m;
    ws.Cell(2, 6).Value = 500m;
    ws.Cell(2, 7).Value = 10m;
    wb.SaveAs(missingSupplierPath);
}

var goodPath = Path.Combine(outDir, "min-valid-labels.xlsx");
using (var wb = new XLWorkbook())
{
    var ws = wb.Worksheets.Add("Labels");
    var headers = new[] { "Item no", "Item name", "Supplier name", "Material", "Site", "Quantity", "Spend", "Price per 1,000" };
    for (var i = 0; i < headers.Length; i++)
        ws.Cell(1, i + 1).Value = headers[i];

    ws.Cell(2, 1).Value = "FIX-001";
    ws.Cell(2, 2).Value = "Fixture item";
    ws.Cell(2, 3).Value = "Fixture Supplier";
    ws.Cell(2, 4).Value = "PP";
    ws.Cell(2, 5).Value = "DK01";
    ws.Cell(2, 6).Value = 1000m;
    ws.Cell(2, 7).Value = 500m;
    ws.Cell(2, 8).Value = 10m;
    wb.SaveAs(goodPath);
}

Console.WriteLine($"Wrote import verification fixtures to:{Environment.NewLine}{outDir}");
