using PackagingTenderTool.Blazor.Models;

namespace PackagingTenderTool.Blazor.Services;

public sealed class MockDataService : IMockDataService
{
    private readonly ITcoCalculator tcoCalculator;

    public MockDataService(ITcoCalculator tcoCalculator)
    {
        this.tcoCalculator = tcoCalculator ?? throw new ArgumentNullException(nameof(tcoCalculator));
    }

    private static readonly string[] Suppliers =
        ["CC Pack", "EuroPrint", "LabelWorks", "NordTag", "OptiLabel", "ScanPrint"];

    private static readonly string[] Sites =
        ["DK-East", "DK-West", "NO-Jæren", "SE-West"];

    private static readonly string[] Countries =
        ["DK", "NO", "SE"];

    private static readonly string[] Categories =
        ["Labels", "Trays", "Flexibles", "Cardboard"];

    private static readonly string[] MaterialClasses = ["A", "B", "C", "D"];

    private static readonly IReadOnlyDictionary<string, decimal> SupplierQuality =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["CC Pack"] = 92m,
            ["EuroPrint"] = 81m,
            ["LabelWorks"] = 73m,
            ["NordTag"] = 88m,
            ["OptiLabel"] = 69m,
            ["ScanPrint"] = 79m
        };

    public IReadOnlyList<TenderLine> GenerateTenderLines(int count = 120)
    {
        count = Math.Clamp(count, 1, 5_000);

        var lines = new List<TenderLine>(capacity: count);

        // Interpret count as number of distinct line items, and create multiple supplier bids per line item.
        for (var i = 1; i <= count; i++)
        {
            var site = Sites[(i * 5) % Sites.Length];
            var country = Countries[(i * 3) % Countries.Length];
            var category = Categories[(i * 11) % Categories.Length];

            // Dummy procurement-ish base price (kr./unit)
            var basePrice = 4.50m + ((i % 37) * 0.18m);
            var lineItem = $"LN-{i:0000}";

            for (var s = 0; s < Suppliers.Length; s++)
            {
                var supplier = Suppliers[s];
                // Supplier-specific delta so every line has a leader.
                var supplierDelta = ((s - 2) * 0.12m) + ((i % 5) * 0.03m);
                var supplierPrice = decimal.Round(basePrice + supplierDelta, 2, MidpointRounding.AwayFromZero);

                lines.Add(new TenderLine(
                    LineItem: lineItem,
                    Supplier: supplier,
                    Site: site,
                    Country: country,
                    Category: category,
                    BasePrice: supplierPrice));
            }
        }

        return lines;
    }

    public IReadOnlyList<SupplierSummary> GetSupplierSummaries(int lineCount = 140)
    {
        var lines = GenerateTenderLines(lineCount);
        var offers = BuildOffers(lines);

        var summaries =
            offers.Select(o => new
                {
                    Offer = o,
                    Result = tcoCalculator.Calculate(o)
                })
                .GroupBy(x => new { x.Offer.TenderLine.Site, x.Offer.TenderLine.Supplier })
                .Select(g => new SupplierSummary(
                    Site: g.Key.Site,
                    Supplier: g.Key.Supplier,
                    Lines: g.Count(),
                    DataQualityScore: g.Average(x => x.Offer.DataQualityScore),
                    Commercial: g.Sum(x => x.Result.Commercial),
                    Regulatory: g.Sum(x => x.Result.Regulatory),
                    Technical: g.Sum(x => x.Result.Technical),
                    Switching: g.Sum(x => x.Result.Switching),
                    Total: g.Sum(x => x.Result.Total)))
                .OrderBy(s => s.Site)
                .ThenBy(s => s.Total)
                .ToList();

        return summaries;
    }

    public IReadOnlyList<LineTcoEntry> GetLineTcoEntries(int lineCount = 140)
    {
        var lines = GenerateTenderLines(lineCount);
        var offers = BuildOffers(lines);

        var list = new List<LineTcoEntry>(offers.Count);
        foreach (var o in offers)
        {
            var decision = tcoCalculator.CalculateDecision(o);
            list.Add(new LineTcoEntry(
                Line: o.TenderLine,
                Offer: o,
                Result: decision.Actual,
                WeightedDecisionScore: decision.WeightedDecisionScore,
                DecisionScoreIndex: decision.DecisionScoreIndex));
        }

        return list;
    }

    public IReadOnlyList<AuditGridRow> GetAuditRows(int lineCount = 140)
    {
        var entries = GetLineTcoEntries(lineCount);
        return entries.Select(e => new AuditGridRow(
                LineItem: e.Line.LineItem,
                Supplier: e.Line.Supplier,
                Site: e.Line.Site,
                Category: e.Line.Category,
                MaterialClass: e.Offer.MaterialClass,
                BasePrice: e.Line.BasePrice,
                ActualTco: e.Result.Total,
                WeightedDecisionScore: e.WeightedDecisionScore,
                DecisionScoreIndex: e.DecisionScoreIndex,
                DataQualityScore: e.Offer.DataQualityScore))
            .ToList();
    }

    public IReadOnlyList<NegotiationBidRow> GetNegotiationRows(string lineItem, string site, int lineCount = 140)
    {
        var entries = GetLineTcoEntries(lineCount)
            .Where(e => e.Line.LineItem == lineItem && e.Line.Site == site)
            .ToList();

        if (entries.Count == 0)
            return Array.Empty<NegotiationBidRow>();

        var leadingTco = entries.Min(e => e.Result.Total);
        var bestInScenario = entries.OrderBy(e => e.DecisionScoreIndex).First();

        return entries
            .OrderBy(e => e.Result.Total)
            .Select(e =>
            {
                var variancePct = leadingTco == 0m ? 0m : ((e.Result.Total - leadingTco) / leadingTco) * 100m;
                return new NegotiationBidRow(
                    Supplier: e.Line.Supplier,
                    ActualTco: e.Result.Total,
                    DecisionScoreIndex: e.DecisionScoreIndex,
                    VariancePercent: decimal.Round(variancePct, 1, MidpointRounding.AwayFromZero),
                    IsBestInScenario: e.Line.Supplier == bestInScenario.Line.Supplier);
            })
            .ToList();
    }

    private static IReadOnlyList<SupplierOffer> BuildOffers(IReadOnlyList<TenderLine> lines)
    {
        var offers = new List<SupplierOffer>(lines.Count);

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var materialClass = MaterialClasses[(i * 13) % MaterialClasses.Length];
            var quality = SupplierQuality.TryGetValue(line.Supplier, out var q) ? q : 85m;

            // Small deterministic deltas to make KPIs non-trivial but stable.
            var technicalFit = decimal.Round(((i % 19) * 0.07m), 2, MidpointRounding.AwayFromZero);
            var switchingCost = decimal.Round(((i % 11) * 0.09m), 2, MidpointRounding.AwayFromZero);

            offers.Add(new SupplierOffer(
                TenderLine: line,
                BasePrice: line.BasePrice,
                MaterialClass: materialClass,
                TechnicalFit: technicalFit,
                SwitchingCost: switchingCost,
                DataQualityScore: quality));
        }

        return offers;
    }
}

