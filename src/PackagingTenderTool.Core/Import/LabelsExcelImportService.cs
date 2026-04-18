using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Import;

public sealed class LabelsExcelImportService
{
    private static readonly IReadOnlyDictionary<string, string[]> ColumnAliases =
        new Dictionary<string, string[]>
        {
            [nameof(LabelLineItem.ItemNo)] = ["Item no", "Item no.", "Item number"],
            [nameof(LabelLineItem.ItemName)] = ["Item name", "Item"],
            [nameof(LabelLineItem.SupplierName)] = ["Supplier name", "Supplier"],
            [nameof(LabelLineItem.Site)] = ["Site"],
            [nameof(LabelLineItem.Quantity)] = ["Quantity", "Qty"],
            [nameof(LabelLineItem.Spend)] = ["Spend"],
            [nameof(LabelLineItem.PricePerThousand)] = ["Price per 1,000", "Price per 1000", "Price/1000"],
            [nameof(LabelLineItem.Price)] = ["Price"],
            [nameof(LabelLineItem.TheoreticalSpend)] = ["Theoretical spend"],
            [nameof(LabelLineItem.LabelSize)] = ["Label size"],
            [nameof(LabelLineItem.WindingDirection)] = ["Winding direction"],
            [nameof(LabelLineItem.Material)] = ["Material"],
            [nameof(LabelLineItem.ReelDiameterOrPcsPerRoll)] =
                ["Reel diameter / pcs per roll", "Reel diameter/pcs per roll", "Reel diameter", "Pcs per roll"],
            [nameof(LabelLineItem.NumberOfColors)] = ["No. of colors", "No of colors", "Number of colors"],
            [nameof(LabelLineItem.Comment)] = ["Comment", "Comments"]
        };

    public Tender ImportTender(
        string filePath,
        string tenderName = "Imported Labels Tender",
        TenderSettings? settings = null)
    {
        using var stream = File.OpenRead(filePath);

        return ImportTender(stream, tenderName, settings);
    }

    public Tender ImportTender(
        Stream excelStream,
        string tenderName = "Imported Labels Tender",
        TenderSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(excelStream);

        var tender = new Tender
        {
            Name = tenderName,
            Settings = settings ?? new TenderSettings()
        };

        foreach (var lineItem in ImportLineItems(excelStream))
        {
            tender.LabelLineItems.Add(lineItem);
        }

        return tender;
    }

    public IReadOnlyList<LabelLineItem> ImportLineItems(Stream excelStream)
    {
        ArgumentNullException.ThrowIfNull(excelStream);

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("The Excel workbook does not contain a worksheet.");

        var headerRow = worksheet.FirstRowUsed()
            ?? throw new InvalidOperationException("The Excel worksheet does not contain a header row.");
        var columnMap = BuildColumnMap(headerRow);

        return worksheet.RowsUsed()
            .Where(row => row.RowNumber() > headerRow.RowNumber())
            .Where(RowHasAnyContent)
            .Select(row => MapRow(row, columnMap))
            .ToList();
    }

    private static IReadOnlyDictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var aliasLookup = ColumnAliases
            .SelectMany(pair => pair.Value.Select(alias => new
            {
                PropertyName = pair.Key,
                NormalizedAlias = NormalizeColumnName(alias)
            }))
            .GroupBy(alias => alias.NormalizedAlias)
            .ToDictionary(group => group.Key, group => group.First().PropertyName);

        var columnMap = new Dictionary<string, int>();
        foreach (var cell in headerRow.CellsUsed())
        {
            var normalizedHeader = NormalizeColumnName(cell.GetString());
            if (aliasLookup.TryGetValue(normalizedHeader, out var propertyName)
                && !columnMap.ContainsKey(propertyName))
            {
                columnMap[propertyName] = cell.Address.ColumnNumber;
            }
        }

        return columnMap;
    }

    private static LabelLineItem MapRow(IXLRow row, IReadOnlyDictionary<string, int> columnMap)
    {
        var lineItem = new LabelLineItem
        {
            ItemNo = GetString(row, columnMap, nameof(LabelLineItem.ItemNo)),
            ItemName = GetString(row, columnMap, nameof(LabelLineItem.ItemName)),
            SupplierName = GetString(row, columnMap, nameof(LabelLineItem.SupplierName)),
            Site = GetString(row, columnMap, nameof(LabelLineItem.Site)),
            LabelSize = GetString(row, columnMap, nameof(LabelLineItem.LabelSize)),
            WindingDirection = GetString(row, columnMap, nameof(LabelLineItem.WindingDirection)),
            Material = GetString(row, columnMap, nameof(LabelLineItem.Material)),
            ReelDiameterOrPcsPerRoll = GetString(row, columnMap, nameof(LabelLineItem.ReelDiameterOrPcsPerRoll)),
            Comment = GetString(row, columnMap, nameof(LabelLineItem.Comment))
        };

        lineItem.Quantity = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.Quantity),
            nameof(LabelLineItem.Quantity),
            lineItem.SourceManualReviewFlags);
        lineItem.Spend = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.Spend),
            nameof(LabelLineItem.Spend),
            lineItem.SourceManualReviewFlags);
        lineItem.PricePerThousand = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.PricePerThousand),
            nameof(LabelLineItem.PricePerThousand),
            lineItem.SourceManualReviewFlags);
        lineItem.Price = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.Price),
            nameof(LabelLineItem.Price),
            lineItem.SourceManualReviewFlags);
        lineItem.TheoreticalSpend = GetDecimal(
            row,
            columnMap,
            nameof(LabelLineItem.TheoreticalSpend),
            nameof(LabelLineItem.TheoreticalSpend),
            lineItem.SourceManualReviewFlags);
        lineItem.NumberOfColors = GetInteger(
            row,
            columnMap,
            nameof(LabelLineItem.NumberOfColors),
            nameof(LabelLineItem.NumberOfColors),
            lineItem.SourceManualReviewFlags);

        return lineItem;
    }

    private static string? GetString(
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        string propertyName)
    {
        if (!columnMap.TryGetValue(propertyName, out var columnNumber))
        {
            return null;
        }

        var value = row.Cell(columnNumber).GetFormattedString().Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static decimal? GetDecimal(
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        string propertyName,
        string fieldName,
        ICollection<ManualReviewFlag> manualReviewFlags)
    {
        if (!columnMap.TryGetValue(propertyName, out var columnNumber))
        {
            return null;
        }

        var cell = row.Cell(columnNumber);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.Number
            && cell.TryGetValue<decimal>(out var numericValue))
        {
            return numericValue;
        }

        var sourceValue = cell.GetFormattedString().Trim();
        if (TryParseDecimal(sourceValue, out var parsedValue))
        {
            return parsedValue;
        }

        AddInvalidNumericFlag(manualReviewFlags, fieldName, sourceValue, "Imported numeric value could not be parsed.");

        return null;
    }

    private static int? GetInteger(
        IXLRow row,
        IReadOnlyDictionary<string, int> columnMap,
        string propertyName,
        string fieldName,
        ICollection<ManualReviewFlag> manualReviewFlags)
    {
        var decimalValue = GetDecimal(row, columnMap, propertyName, fieldName, manualReviewFlags);
        if (decimalValue is null)
        {
            return null;
        }

        if (decimalValue.Value % 1m == 0m)
        {
            return decimal.ToInt32(decimalValue.Value);
        }

        AddInvalidNumericFlag(
            manualReviewFlags,
            fieldName,
            decimalValue.Value.ToString("G", CultureInfo.InvariantCulture),
            "Imported integer value had a decimal component.");

        return null;
    }

    private static void AddInvalidNumericFlag(
        ICollection<ManualReviewFlag> manualReviewFlags,
        string fieldName,
        string sourceValue,
        string reason)
    {
        manualReviewFlags.Add(new ManualReviewFlag
        {
            FieldName = fieldName,
            SourceValue = sourceValue,
            Reason = reason,
            Severity = ManualReviewSeverity.Error
        });
    }

    private static bool TryParseDecimal(string sourceValue, out decimal value)
    {
        var normalizedValue = NormalizeDecimalValue(sourceValue);
        if (normalizedValue is not null
            && decimal.TryParse(
                normalizedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value))
        {
            return true;
        }

        return decimal.TryParse(
                sourceValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value)
            || decimal.TryParse(
                sourceValue,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out value);
    }

    private static string? NormalizeDecimalValue(string sourceValue)
    {
        var value = sourceValue
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var lastCommaIndex = value.LastIndexOf(',');
        var lastDotIndex = value.LastIndexOf('.');

        if (lastCommaIndex >= 0 && lastDotIndex >= 0)
        {
            return lastCommaIndex > lastDotIndex
                ? value.Replace(".", string.Empty).Replace(',', '.')
                : value.Replace(",", string.Empty);
        }

        if (lastCommaIndex >= 0)
        {
            return value.Replace(',', '.');
        }

        return value;
    }

    private static bool RowHasAnyContent(IXLRow row)
    {
        return row.CellsUsed().Any(cell => !string.IsNullOrWhiteSpace(cell.GetFormattedString()));
    }

    private static string NormalizeColumnName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}
