using System.Globalization;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

var tender = CreateSampleTender();
var lineEvaluationService = new LineEvaluationService();
var supplierAggregationService = new SupplierAggregationService();

var lineEvaluations = lineEvaluationService.EvaluateMany(tender.LabelLineItems);

var supplierEvaluations = supplierAggregationService.AggregateBySupplierName(lineEvaluations);

PrintSummary(tender, supplierEvaluations);

static Tender CreateSampleTender()
{
    return new Tender
    {
        Name = "Labels Tender v1 Sample",
        Settings = new TenderSettings
        {
            PackagingProfile = PackagingProfile.Labels,
            CurrencyCode = "EUR"
        },
        LabelLineItems =
        [
            new LabelLineItem
            {
                ItemNo = "LBL-001",
                ItemName = "Front label 80x120",
                SupplierName = "Acme Labels",
                Quantity = 100_000m,
                Spend = 1_250m,
                PricePerThousand = 12.50m,
                Material = "PP white",
                NumberOfColors = 4
            },
            new LabelLineItem
            {
                ItemNo = "LBL-002",
                ItemName = "Back label 60x90",
                SupplierName = "Acme Labels",
                Quantity = 80_000m,
                Spend = 740m,
                PricePerThousand = 9.25m,
                Material = "Paper",
                NumberOfColors = 2
            },
            new LabelLineItem
            {
                ItemNo = "LBL-003",
                ItemName = "Neck label 35x45",
                SupplierName = "Beta Packaging",
                Quantity = 60_000m,
                Spend = 690m,
                PricePerThousand = 11.50m,
                Material = "PP clear",
                NumberOfColors = 3
            },
            new LabelLineItem
            {
                ItemNo = "LBL-004",
                ItemName = "Promo label 50x50",
                SupplierName = "Beta Packaging",
                Quantity = 25_000m,
                Spend = null,
                PricePerThousand = 8.75m,
                Material = null,
                NumberOfColors = 1,
                Comment = "Spend intentionally missing to demonstrate manual review."
            }
        ]
    };
}

static void PrintSummary(Tender tender, IReadOnlyCollection<SupplierEvaluation> supplierEvaluations)
{
    Console.WriteLine("PackagingTenderTool evaluation sample");
    Console.WriteLine("-------------------------------------");
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
