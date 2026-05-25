namespace PackagingTenderTool.Core.DTOs;

/// <summary>Summary row per supplier in price comparison.</summary>
public sealed record SupplierPriceCompareRow(
    string SupplierName,
    int QuotedLineCount,
    int MissingLineCount,
    decimal TotalOfferedSpend,
    decimal AverageUnitPrice,
    int LowestPriceCount,
    int HighestPriceCount,
    decimal SpendVsCheapest,
    decimal PctVsCheapest);

/// <summary>Price entry for one supplier on one tender line.</summary>
public sealed record SupplierLinePriceEntry(
    string SupplierName,
    decimal? Price,
    bool IsCheapest);

/// <summary>One tender line with prices from all compared suppliers.</summary>
public sealed record SupplierPriceLineRow(
    string ItemNo,
    string LabelSize,
    string Site,
    string Material,
    IReadOnlyList<SupplierLinePriceEntry> Prices);

/// <summary>Full result of a price comparison across selected suppliers.</summary>
public sealed record SupplierPriceCompareResult(
    IReadOnlyList<SupplierPriceCompareRow> SupplierRows,
    IReadOnlyList<SupplierPriceLineRow> LineRows);
