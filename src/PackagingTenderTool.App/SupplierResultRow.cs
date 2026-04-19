using System.Globalization;
using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.App;

internal sealed class SupplierResultRow
{
    private SupplierResultRow()
    {
    }

    public bool Compare { get; set; }

    public string SupplierName { get; private init; } = string.Empty;

    public decimal TotalSpend { get; private init; }

    public decimal? CommercialScore { get; private init; }

    public decimal? TechnicalScore { get; private init; }

    public decimal? RegulatoryScore { get; private init; }

    public decimal? TotalScore { get; private init; }

    public SupplierClassification? Classification { get; private init; }

    public int ManualReviewFlagCount { get; private init; }

    public string CurrencyCode { get; private init; } = "EUR";

    public string Notes { get; private init; } = string.Empty;

    public string TotalSpendDisplay => $"{TotalSpend:N2} {CurrencyCode}";

    public string CommercialScoreDisplay => FormatScore(CommercialScore);

    public string TechnicalScoreDisplay => FormatScore(TechnicalScore);

    public string RegulatoryScoreDisplay => FormatScore(RegulatoryScore);

    public string TotalScoreDisplay => FormatScore(TotalScore);

    public static SupplierResultRow FromSupplier(SupplierEvaluation supplier, string currencyCode)
    {
        var scoreBreakdown = supplier.ScoreBreakdown ?? new ScoreBreakdown();
        return new SupplierResultRow
        {
            SupplierName = string.IsNullOrWhiteSpace(supplier.SupplierName) ? "(missing supplier)" : supplier.SupplierName,
            TotalSpend = supplier.TotalSpend,
            CommercialScore = scoreBreakdown.Commercial,
            TechnicalScore = scoreBreakdown.Technical,
            RegulatoryScore = scoreBreakdown.Regulatory,
            TotalScore = scoreBreakdown.Total,
            Classification = supplier.Classification,
            ManualReviewFlagCount = supplier.ManualReviewFlags?.Count ?? 0,
            CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "EUR" : currencyCode,
            Notes = BuildNotes(supplier)
        };
    }

    private static string BuildNotes(SupplierEvaluation supplier)
    {
        if (supplier.ManualReviewFlags?.Count > 0)
        {
            var firstFlag = supplier.ManualReviewFlags[0];
            return $"Manual review is required. First flag: {firstFlag.FieldName ?? "Source data"} - {firstFlag.Reason}";
        }

        return supplier.ClassificationReason ??
            "No manual review flags. Classification is based on the provisional total score thresholds.";
    }

    private static string FormatScore(decimal? value)
    {
        return value.HasValue ? value.Value.ToString("N2", CultureInfo.CurrentCulture) : "-";
    }
}
