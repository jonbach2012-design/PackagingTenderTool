using PackagingTenderTool.Core.DTOs;
using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class SupplierPriceCompareService
{
    public SupplierPriceCompareResult Compare(
        IReadOnlyList<LabelLineItem> allLines,
        IReadOnlyList<string> supplierNames)
    {
        ArgumentNullException.ThrowIfNull(allLines);
        ArgumentNullException.ThrowIfNull(supplierNames);

        var suppliers = supplierNames
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (suppliers.Count == 0)
        {
            return new SupplierPriceCompareResult([], []);
        }

        var supplierSet = new HashSet<string>(suppliers, StringComparer.OrdinalIgnoreCase);

        var filtered = allLines
            .Where(l => !string.IsNullOrWhiteSpace(l.SupplierName)
                && supplierSet.Contains(l.SupplierName.Trim()))
            .ToList();

        var byItem = filtered
            .Where(l => !string.IsNullOrWhiteSpace(l.ItemNo))
            .GroupBy(l => l.ItemNo!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var itemNos = byItem.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

        var quotedBySupplierItem = suppliers.ToDictionary(
            s => s,
            _ => new Dictionary<string, (decimal PricePerThousand, decimal Quantity, LabelLineItem Line)>(
                StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        foreach (var (itemNo, lines) in byItem)
        {
            foreach (var supplier in suppliers)
            {
                var supplierLine = lines
                    .Where(l => string.Equals(
                        l.SupplierName?.Trim(),
                        supplier,
                        StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault(l => l.PricePerThousand is > 0m);

                if (supplierLine is null)
                {
                    continue;
                }

                quotedBySupplierItem[supplier][itemNo] = (
                    supplierLine.PricePerThousand!.Value,
                    supplierLine.Quantity ?? 0m,
                    supplierLine);
            }
        }

        var lowestCounts = suppliers.ToDictionary(s => s, _ => 0, StringComparer.OrdinalIgnoreCase);
        var highestCounts = suppliers.ToDictionary(s => s, _ => 0, StringComparer.OrdinalIgnoreCase);
        var lineRows = new List<SupplierPriceLineRow>(itemNos.Count);

        foreach (var itemNo in itemNos)
        {
            var lines = byItem[itemNo];
            var referenceLine = lines[0];

            var pricesOnLine = suppliers
                .Select(s => quotedBySupplierItem[s].TryGetValue(itemNo, out var q) ? q.PricePerThousand : (decimal?)null)
                .Where(p => p is > 0m)
                .Select(p => p!.Value)
                .ToList();

            decimal? minPrice = pricesOnLine.Count > 0 ? pricesOnLine.Min() : null;
            decimal? maxPrice = pricesOnLine.Count > 0 ? pricesOnLine.Max() : null;

            var entries = new List<SupplierLinePriceEntry>(suppliers.Count);
            foreach (var supplier in suppliers)
            {
                decimal? price = quotedBySupplierItem[supplier].TryGetValue(itemNo, out var quoted)
                    ? quoted.PricePerThousand
                    : null;

                var isCheapest = minPrice is not null && price is > 0m && price.Value == minPrice.Value;
                entries.Add(new SupplierLinePriceEntry(supplier, price, isCheapest));

                if (isCheapest)
                {
                    lowestCounts[supplier]++;
                }

                if (maxPrice is not null && price is > 0m && price.Value == maxPrice.Value)
                {
                    highestCounts[supplier]++;
                }
            }

            lineRows.Add(new SupplierPriceLineRow(
                itemNo,
                referenceLine.LabelSize ?? string.Empty,
                referenceLine.Site ?? string.Empty,
                referenceLine.Material ?? string.Empty,
                entries));
        }

        var supplierRows = new List<SupplierPriceCompareRow>(suppliers.Count);
        foreach (var supplier in suppliers)
        {
            var quotedCount = 0;
            var missingCount = 0;
            decimal totalSpend = 0m;
            var unitPrices = new List<decimal>();

            foreach (var itemNo in itemNos)
            {
                if (quotedBySupplierItem[supplier].TryGetValue(itemNo, out var quoted))
                {
                    quotedCount++;
                    totalSpend += quoted.PricePerThousand / 1000m * quoted.Quantity;
                    unitPrices.Add(quoted.PricePerThousand);
                }
                else
                {
                    missingCount++;
                }
            }

            var averageUnitPrice = unitPrices.Count > 0
                ? unitPrices.Average()
                : 0m;

            supplierRows.Add(new SupplierPriceCompareRow(
                supplier,
                quotedCount,
                missingCount,
                totalSpend,
                averageUnitPrice,
                lowestCounts[supplier],
                highestCounts[supplier],
                SpendVsCheapest: 0m,
                PctVsCheapest: 0m));
        }

        var cheapestSpend = supplierRows.Count > 0
            ? supplierRows.Min(r => r.TotalOfferedSpend)
            : 0m;

        supplierRows = supplierRows
            .Select(row =>
            {
                var spendVsCheapest = row.TotalOfferedSpend - cheapestSpend;
                var pctVsCheapest = cheapestSpend > 0m && spendVsCheapest > 0m
                    ? spendVsCheapest / cheapestSpend * 100m
                    : 0m;

                return row with
                {
                    SpendVsCheapest = spendVsCheapest,
                    PctVsCheapest = pctVsCheapest
                };
            })
            .OrderBy(r => r.TotalOfferedSpend)
            .ThenBy(r => r.SupplierName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SupplierPriceCompareResult(supplierRows, lineRows);
    }
}
