namespace PackagingTenderTool.Core.Services;

public sealed record PpwrRiskEvaluation(
    decimal PenaltyAmount,
    string NormalizedGrade,
    bool MarketAccessRisk2030,
    bool MarketAccessRiskNow,
    string Breakdown);

public static class PpwrRiskEvaluator
{
    public static PpwrRiskEvaluation Evaluate(decimal commercialSpend, string? ppwrGrade)
    {
        var grade = NormalizeGrade(ppwrGrade);
        var penaltyRate = grade switch
        {
            "C" => 0.05m,
            "D" => 0.15m,
            "E" => 0.25m,
            _ => 0m
        };

        var penalty = decimal.Round(commercialSpend * penaltyRate, 2, MidpointRounding.AwayFromZero);
        var breakdown = $"Grade {grade} — penalty {penaltyRate:P0} of commercial spend";

        return new PpwrRiskEvaluation(
            PenaltyAmount: penalty,
            NormalizedGrade: grade,
            MarketAccessRisk2030: grade == "D",
            MarketAccessRiskNow: grade == "E",
            Breakdown: breakdown);
    }

    public static string NormalizeGrade(string? g)
    {
        if (string.IsNullOrWhiteSpace(g))
            return "A";

        var t = g.Trim().ToUpperInvariant();
        return t is "A" or "B" or "C" or "D" or "E" ? t : "A";
    }
}
