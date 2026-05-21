using System.Globalization;
using ClosedXML.Excel;
using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Import;

/// <summary>
/// Reads pivot-format label tenders (one row per item, supplier prices in columns) and imports them via <see cref="LabelsExcelImportService"/>.
/// </summary>
public sealed class PivotLabelsExcelImportService
{
    public const string PivotSheetName = "All labels DSH";
    public const string PivotSheetNotFoundMarker = "PIVOT_SHEET_NOT_FOUND";

    private const int FirstDataRow = 2;

    private const int ColItemNo = 1;
    private const int ColItemName = 2;
    private const int ColSite = 3;
    private const int ColQuantity = 4;
    private const int ColLabelSize = 5;
    private const int ColWindingDirection = 6;
    private const int ColMaterial = 7;
    private const int ColReelDiameter = 8;
    private const int ColNumberOfColors = 9;
    private const int ColSuggestedMoq = 10;
    private const int ColCurrentPrice = 11;

    private static readonly (string SupplierName, int PriceCol, int MoqCol, int CommentCol)[] SupplierBlocks =
    [
        ("Flexoprint", 12, 13, 14),
        ("Norsk Etikett", 15, 16, 17),
        ("Grafiket", 18, 19, 20),
        ("Ettiketto", 21, 22, 23)
    ];

    /// <summary>Headers for the synthetic workbook — must match <c>LabelsExcelImportService</c> column aliases.</summary>
    private static readonly string[] SyntheticHeaderRow =
    [
        "Item no.",
        "Item name",
        "Site",
        "Quantity",
        "Label size",
        "Winding direction",
        "Material",
        "Reel diameter / pcs per roll",
        "No. of colors",
        "Supplier name",
        "Price per 1000",
        "Comment",
        "current_price"
    ];

    public LabelsTenderImportResult ImportTenderWithReport(Stream stream, string tenderName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenderName);

        using var pivotWorkbook = new XLWorkbook(stream);
        if (!pivotWorkbook.TryGetWorksheet(PivotSheetName, out var pivotSheet))
        {
            throw new InvalidOperationException(PivotSheetNotFoundMarker);
        }

        var longRows = new List<PivotLongRow>();
        var lastRow = pivotSheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var row = FirstDataRow; row <= lastRow; row++)
        {
            var itemNo = TrimmedCellText(pivotSheet, row, ColItemNo);
            var itemName = TrimmedCellText(pivotSheet, row, ColItemName);
            if (string.IsNullOrEmpty(itemNo) && string.IsNullOrEmpty(itemName))
            {
                continue;
            }

            var site = TrimmedCellText(pivotSheet, row, ColSite);
            var quantityText = RawCellText(pivotSheet, row, ColQuantity);
            var labelSize = TrimmedCellText(pivotSheet, row, ColLabelSize);
            var winding = TrimmedCellText(pivotSheet, row, ColWindingDirection);
            var material = TrimmedCellText(pivotSheet, row, ColMaterial);
            var reel = TrimmedCellText(pivotSheet, row, ColReelDiameter);
            var colorsText = RawCellText(pivotSheet, row, ColNumberOfColors);
            var suggestedMoq = TrimmedCellText(pivotSheet, row, ColSuggestedMoq);
            var currentPriceText = RawCellText(pivotSheet, row, ColCurrentPrice);

            foreach (var block in SupplierBlocks)
            {
                if (!HasNonEmptyPrice(pivotSheet, row, block.PriceCol))
                {
                    continue;
                }

                var priceText = RawCellText(pivotSheet, row, block.PriceCol);
                var supplierMoq = TrimmedCellText(pivotSheet, row, block.MoqCol);
                var supplierComment = TrimmedCellText(pivotSheet, row, block.CommentCol);
                var comment = BuildComment(suggestedMoq, supplierMoq, supplierComment);

                longRows.Add(new PivotLongRow(
                    itemNo,
                    itemName,
                    site,
                    quantityText,
                    labelSize,
                    winding,
                    material,
                    reel,
                    colorsText,
                    block.SupplierName,
                    priceText,
                    comment,
                    currentPriceText));
            }
        }

        using var syntheticStream = BuildSyntheticWorkbookStream(longRows);
        var inner = new LabelsExcelImportService().ImportTenderWithReport(syntheticStream, tenderName);
        return inner;
    }

    private static string? TrimmedCellText(IXLWorksheet sheet, int row, int column)
    {
        var cell = sheet.Cell(row, column);
        if (cell.IsEmpty())
        {
            return null;
        }

        var s = cell.GetFormattedString().Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static string? RawCellText(IXLWorksheet sheet, int row, int column)
    {
        var cell = sheet.Cell(row, column);
        if (cell.IsEmpty())
        {
            return null;
        }

        var s = cell.GetFormattedString().Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static bool HasNonEmptyPrice(IXLWorksheet sheet, int row, int column)
    {
        var cell = sheet.Cell(row, column);
        if (cell.IsEmpty())
        {
            return false;
        }

        if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var d))
        {
            return true;
        }

        var text = cell.GetFormattedString().Trim();
        return text.Length > 0;
    }

    private static string? BuildComment(string? suggestedMoq, string? supplierMoq, string? supplierComment)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(suggestedMoq))
        {
            parts.Add($"Suggested MOQ: {suggestedMoq}");
        }

        if (!string.IsNullOrEmpty(supplierMoq))
        {
            parts.Add(supplierMoq);
        }

        if (!string.IsNullOrEmpty(supplierComment))
        {
            parts.Add(supplierComment);
        }

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }

    private static MemoryStream BuildSyntheticWorkbookStream(IReadOnlyList<PivotLongRow> rows)
    {
        var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Pivot import");
            for (var c = 0; c < SyntheticHeaderRow.Length; c++)
            {
                ws.Cell(1, c + 1).Value = SyntheticHeaderRow[c];
            }

            var r = 2;
            foreach (var row in rows)
            {
                WriteString(ws, r, 1, row.ItemNo);
                WriteString(ws, r, 2, row.ItemName);
                WriteString(ws, r, 3, row.Site);
                WriteDecimalOrText(ws, r, 4, row.QuantityText);
                WriteString(ws, r, 5, row.LabelSize);
                WriteString(ws, r, 6, row.WindingDirection);
                WriteString(ws, r, 7, row.Material);
                WriteString(ws, r, 8, row.ReelDiameter);
                WriteNumberOfColors(ws, r, 9, row.NumberOfColorsText);
                WriteString(ws, r, 10, row.SupplierName);
                WriteDecimalOrText(ws, r, 11, row.PricePerThousandText);
                WriteString(ws, r, 12, row.Comment);
                WriteDecimalOrText(ws, r, 13, row.CurrentPriceText);
                r++;
            }

            wb.SaveAs(ms);
        }

        ms.Position = 0;
        return ms;
    }

    private static void WriteString(IXLWorksheet ws, int row, int col, string? value)
    {
        if (value is null)
        {
            return;
        }

        ws.Cell(row, col).Value = value;
    }

    private static void WriteDecimalOrText(IXLWorksheet ws, int row, int col, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (FlexibleNumberParser.TryParseFlexibleDecimal(text, out var d))
        {
            ws.Cell(row, col).Value = d;
            return;
        }

        ws.Cell(row, col).Value = text;
    }

    private static void WriteNumberOfColors(IXLWorksheet ws, int row, int col, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (FlexibleNumberParser.TryParseFlexibleDecimal(text, out var d) && d % 1m == 0m)
        {
            ws.Cell(row, col).Value = (int)d;
            return;
        }

        ws.Cell(row, col).Value = text;
    }

    private sealed record PivotLongRow(
        string? ItemNo,
        string? ItemName,
        string? Site,
        string? QuantityText,
        string? LabelSize,
        string? WindingDirection,
        string? Material,
        string? ReelDiameter,
        string? NumberOfColorsText,
        string SupplierName,
        string? PricePerThousandText,
        string? Comment,
        string? CurrentPriceText);
}
