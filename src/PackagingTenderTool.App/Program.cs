using System.Globalization;
using ClosedXML.Excel;
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

var tenderSettings = CreateSampleTenderSettings();
using var importStream = CreateSampleLabelsWorkbook();
var tender = new LabelsExcelImportService().ImportTender(
    importStream,
    "Labels Tender v1 Excel Import Sample",
    tenderSettings);

var lineEvaluationService = new LineEvaluationService();
var supplierAggregationService = new SupplierAggregationService();

var lineEvaluations = lineEvaluationService.EvaluateMany(tender.LabelLineItems, tender.Settings);
var supplierEvaluations = supplierAggregationService.AggregateBySupplierName(lineEvaluations);

PrintSummary(tender, supplierEvaluations);

static TenderSettings CreateSampleTenderSettings()
{
    return new TenderSettings
    {
        PackagingProfile = PackagingProfile.Labels,
        CurrencyCode = "EUR",
        ExpectedMaterial = "PP white",
        ExpectedWindingDirection = "Left",
        ExpectedLabelSize = "80x120"
    };
}

static MemoryStream CreateSampleLabelsWorkbook()
{
    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add("Labels");
    var headers = new[]
    {
        "Item no",
        "Item name",
        "Supplier name",
        "Site",
        "Quantity",
        "Spend",
        "Price per 1,000",
        "Price",
        "Theoretical spend",
        "Label size",
        "Winding direction",
        "Material",
        "Reel diameter / pcs per roll",
        "No. of colors",
        "Comment"
    };
    var rows = new object?[][]
    {
        ["LBL-001", "Front label 80x120", "Acme Labels", "DK01", "100000", "1.250,00", "12,50", null, "1.250,00", "80x120", "Left", "PP white", "300mm", 4, "Imported sample row with comma decimals."],
        ["LBL-002", "Back label 60x90", "Acme Labels", "DK01", "80000", "740.00", "9.25", null, "740.00", "80x120", "Left", "Paper", "300mm", 2, "Material mismatch with dot decimals."],
        ["LBL-003", "Neck label 35x45", "Beta Packaging", "SE01", "60000", "690,00", "11,50", null, "690,00", "80x120", "Right", "PP clear", "250mm", 3, "Winding and material mismatch."],
        ["LBL-004", "Promo label 50x50", "Beta Packaging", "SE01", "25000", null, "8.75", null, null, null, "Left", null, "200mm", 1, "Missing values demonstrate manual review."]
    };

    for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
    {
        worksheet.Cell(1, columnIndex + 1).Value = headers[columnIndex];
    }

    for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
    {
        for (var columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
        {
            worksheet.Cell(rowIndex + 2, columnIndex + 1).Value = XLCellValue.FromObject(rows[rowIndex][columnIndex]);
        }
    }

    var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;

    return stream;
}

static void PrintSummary(Tender tender, IReadOnlyCollection<SupplierEvaluation> supplierEvaluations)
{
    Console.WriteLine("PackagingTenderTool evaluation sample");
    Console.WriteLine("-------------------------------------");
    Console.WriteLine("Import source: generated Labels v1 Excel workbook");
    Console.WriteLine($"Tender: {tender.Name}");
    Console.WriteLine($"Profile: {tender.Settings.PackagingProfile}");
    Console.WriteLine($"Currency: {tender.Settings.CurrencyCode}");
    Console.WriteLine($"Imported sample lines: {tender.LabelLineItems.Count}");
    Console.WriteLine();

    foreach (var supplierEvaluation in supplierEvaluations)
    {
        Console.WriteLine($"Supplier: {DisplaySupplierName(supplierEvaluation.SupplierName)}");
        Console.WriteLine($"  Total spend: {FormatMoney(supplierEvaluation.TotalSpend, tender.Settings.CurrencyCode)}");
        Console.WriteLine($"  Manual review required: {FormatYesNo(supplierEvaluation.RequiresManualReview)}");
        Console.WriteLine($"  Line evaluations: {supplierEvaluation.LineEvaluations.Count}");
        Console.WriteLine($"  Manual review flags: {supplierEvaluation.ManualReviewFlags.Count}");
        Console.WriteLine($"  Scores: {FormatScoreBreakdown(supplierEvaluation.ScoreBreakdown)}");
        Console.WriteLine();
    }
}

static string DisplaySupplierName(string supplierName)
{
    return string.IsNullOrWhiteSpace(supplierName)
        ? "(missing supplier)"
        : supplierName;
}

static string FormatMoney(decimal amount, string currencyCode)
{
    return $"{amount.ToString("0.00", CultureInfo.InvariantCulture)} {currencyCode}";
}

static string FormatYesNo(bool value)
{
    return value ? "Yes" : "No";
}

static string FormatScoreBreakdown(ScoreBreakdown scoreBreakdown)
{
    return $"Commercial={FormatScore(scoreBreakdown.Commercial)}, "
        + $"Technical={FormatScore(scoreBreakdown.Technical)}, "
        + $"Regulatory={FormatScore(scoreBreakdown.Regulatory)}, "
        + $"Total={FormatScore(scoreBreakdown.Total)}";
}

static string FormatScore(decimal? score)
{
    return score.HasValue
        ? score.Value.ToString("0.##", CultureInfo.InvariantCulture)
        : "n/a";
}
