using System.Globalization;
using ClosedXML.Excel;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.App;

internal static class RegulatoryConsoleDemo
{
    public static void Run(TextWriter output)
    {
        var tenderSettings = LabelsV1DemoConfiguration.CreateTenderSettings();
        using var importStream = CreateSampleLabelsWorkbook();

        var result = new LabelsTenderEvaluationService().ImportAndEvaluate(
            importStream,
            "Labels Tender v1 Regulatory Scoring Sample",
            tenderSettings);

        PrintSummary(result, output);
    }

    private static MemoryStream CreateSampleLabelsWorkbook()
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
            "Label weight (g)",
            "Mono-material design",
            "Easy separation",
            "Reusable or recyclable material direction",
            "Traceability",
            "Comment"
        };
        var rows = new object?[][]
        {
            ["LBL-001", "Front label 80x120", "Acme Labels", "DK01", "100000", "1.250,00", "12,50", null, "1.250,00", "80x120", "Left", "PP white", "300mm", 4, "1,8", "yes", "yes", "yes", "yes", "Matches all regulatory reference values."],
            ["LBL-002", "Back label 60x90", "Acme Labels", "DK01", "80000", "740.00", "9.25", null, "740.00", "80x120", "Left", "Paper", "300mm", 2, "2.2", "no", "yes", "yes", "yes", "Higher weight and mono-material mismatch reduce regulatory score."],
            ["LBL-003", "Neck label 35x45", "Beta Packaging", "SE01", "60000", "690,00", "11,50", null, "690,00", "80x120", "Right", "PP clear", "250mm", 3, "1,6", "yes", "no", "yes", "yes", "Easy separation mismatch reduces regulatory score."],
            ["LBL-004", "Promo label 50x50", "Beta Packaging", "SE01", "25000", null, "8.75", null, null, null, "Left", null, "200mm", 1, null, null, "yes", null, "yes", "Missing values demonstrate non-blocking manual review."]
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

    private static void PrintSummary(TenderEvaluationResult result, TextWriter output)
    {
        var tender = result.Tender;

        output.WriteLine("PackagingTenderTool regulatory scoring sample");
        output.WriteLine("---------------------------------------------");
        output.WriteLine("Import source: generated Labels v1 Excel workbook");
        output.WriteLine($"Tender: {tender.Name}");
        output.WriteLine($"Profile: {tender.Settings.PackagingProfile}");
        output.WriteLine($"Currency: {tender.Settings.CurrencyCode}");
        output.WriteLine($"Imported sample lines: {tender.LabelLineItems.Count}");
        output.WriteLine("Regulatory references:");
        output.WriteLine($"  Maximum label weight: {FormatDecimal(tender.Settings.MaximumLabelWeightGrams)} g");
        output.WriteLine($"  Mono-material design: {FormatExpectedBool(tender.Settings.ExpectedMonoMaterial)}");
        output.WriteLine($"  Easy separation: {FormatExpectedBool(tender.Settings.ExpectedEasySeparation)}");
        output.WriteLine($"  Reusable or recyclable material direction: {FormatExpectedBool(tender.Settings.ExpectedReusableOrRecyclableMaterial)}");
        output.WriteLine($"  Traceability: {FormatExpectedBool(tender.Settings.ExpectedTraceability)}");
        output.WriteLine();

        foreach (var supplierEvaluation in result.SupplierEvaluations)
        {
            output.WriteLine($"Supplier: {DisplaySupplierName(supplierEvaluation.SupplierName)}");
            output.WriteLine($"  Total spend: {FormatMoney(supplierEvaluation.TotalSpend, tender.Settings.CurrencyCode)}");
            output.WriteLine($"  Manual review required: {FormatYesNo(supplierEvaluation.RequiresManualReview)}");
            output.WriteLine($"  Line evaluations: {supplierEvaluation.LineEvaluations.Count}");
            output.WriteLine($"  Manual review flags: {supplierEvaluation.ManualReviewFlags.Count}");
            output.WriteLine($"  Scores: {FormatScoreBreakdown(supplierEvaluation.ScoreBreakdown)}");
            output.WriteLine($"  Classification: {FormatClassification(supplierEvaluation.Classification)}");
            output.WriteLine();
        }
    }

    private static string DisplaySupplierName(string supplierName)
    {
        return string.IsNullOrWhiteSpace(supplierName)
            ? "(missing supplier)"
            : supplierName;
    }

    private static string FormatMoney(decimal amount, string currencyCode)
    {
        return $"{amount.ToString("0.00", CultureInfo.InvariantCulture)} {currencyCode}";
    }

    private static string FormatYesNo(bool value)
    {
        return value ? "Yes" : "No";
    }

    private static string FormatScoreBreakdown(ScoreBreakdown scoreBreakdown)
    {
        return $"Commercial={FormatScore(scoreBreakdown.Commercial)}, "
            + $"Technical={FormatScore(scoreBreakdown.Technical)}, "
            + $"Regulatory={FormatScore(scoreBreakdown.Regulatory)}, "
            + $"Total={FormatScore(scoreBreakdown.Total)}";
    }

    private static string FormatScore(decimal? score)
    {
        return score.HasValue
            ? score.Value.ToString("0.##", CultureInfo.InvariantCulture)
            : "n/a";
    }

    private static string FormatDecimal(decimal? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.##", CultureInfo.InvariantCulture)
            : "n/a";
    }

    private static string FormatExpectedBool(bool? value)
    {
        return value.HasValue ? FormatYesNo(value.Value) : "n/a";
    }

    private static string FormatClassification(SupplierClassification? classification)
    {
        return classification?.ToString() ?? "Unclassified";
    }
}
