using ClosedXML.Excel;
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Core.Tests;

public sealed class LabelsExcelImportServiceTests
{
    [Fact]
    public void ImportTenderMapsLabelsColumnsIntoLineItems()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Item name", "Supplier name", "Site", "Quantity", "Spend", "Price per 1,000", "Price", "Theoretical spend", "Label size", "Winding direction", "Material", "Reel diameter / pcs per roll", "No. of colors", "Comment"],
            [
                ["LBL-001", "Front label", "Acme Labels", "DK01", 100000m, 1250m, 12.5m, 0.0125m, 1250m, "80x120", "Left", "PP white", "300mm", 4, "Ready"]
            ]);

        var tender = new LabelsExcelImportService().ImportTender(stream, "Imported tender");
        var lineItem = tender.LabelLineItems.Single();

        Assert.Equal("Imported tender", tender.Name);
        Assert.Equal("EUR", tender.Settings.CurrencyCode);
        Assert.Equal("LBL-001", lineItem.ItemNo);
        Assert.Equal("Front label", lineItem.ItemName);
        Assert.Equal("Acme Labels", lineItem.SupplierName);
        Assert.Equal("DK01", lineItem.Site);
        Assert.Equal(100000m, lineItem.Quantity);
        Assert.Equal(1250m, lineItem.Spend);
        Assert.Equal(12.5m, lineItem.PricePerThousand);
        Assert.Equal(0.0125m, lineItem.Price);
        Assert.Equal(1250m, lineItem.TheoreticalSpend);
        Assert.Equal("80x120", lineItem.LabelSize);
        Assert.Equal("Left", lineItem.WindingDirection);
        Assert.Equal("PP white", lineItem.Material);
        Assert.Equal("300mm", lineItem.ReelDiameterOrPcsPerRoll);
        Assert.Equal(4, lineItem.NumberOfColors);
        Assert.Equal("Ready", lineItem.Comment);
        Assert.Empty(lineItem.SourceManualReviewFlags);
    }

    [Fact]
    public void ImportTenderMatchesColumnNamesRobustly()
    {
        using var stream = CreateWorkbookStream(
            ["ITEM NO.", "Supplier", "Qty", "Price per 1000", "No of colors"],
            [
                ["LBL-002", "Beta Packaging", 25000m, 8.75m, 2]
            ]);

        var lineItem = new LabelsExcelImportService()
            .ImportTender(stream)
            .LabelLineItems
            .Single();

        Assert.Equal("LBL-002", lineItem.ItemNo);
        Assert.Equal("Beta Packaging", lineItem.SupplierName);
        Assert.Equal(25000m, lineItem.Quantity);
        Assert.Equal(8.75m, lineItem.PricePerThousand);
        Assert.Equal(2, lineItem.NumberOfColors);
    }

    [Fact]
    public void ImportTenderPreservesNullsAndFlagsInvalidNumericValues()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Supplier name", "Quantity", "Spend", "Price per 1,000", "No. of colors"],
            [
                ["LBL-003", "Gamma Labels", "not-a-number", "", "bad-price", 2.5m]
            ]);

        var lineItem = new LabelsExcelImportService()
            .ImportTender(stream)
            .LabelLineItems
            .Single();

        Assert.Null(lineItem.Quantity);
        Assert.Null(lineItem.Spend);
        Assert.Null(lineItem.PricePerThousand);
        Assert.Null(lineItem.NumberOfColors);
        Assert.Contains(lineItem.SourceManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.Quantity)
            && flag.SourceValue == "not-a-number"
            && flag.Severity == ManualReviewSeverity.Error);
        Assert.Contains(lineItem.SourceManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.PricePerThousand)
            && flag.SourceValue == "bad-price"
            && flag.Severity == ManualReviewSeverity.Error);
        Assert.Contains(lineItem.SourceManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.NumberOfColors)
            && flag.SourceValue == "2.5"
            && flag.Severity == ManualReviewSeverity.Error);
    }

    [Fact]
    public void ImportTenderParsesCommaAndDotDecimalSeparators()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Supplier name", "Quantity", "Spend", "Price per 1,000", "Price", "Theoretical spend"],
            [
                ["LBL-004", "Acme Labels", "100000,5", "1.250,50", "12,50", "0,0125", "1.250,50"],
                ["LBL-005", "Beta Packaging", "100000.5", "1,250.50", "12.50", "0.0125", "1,250.50"]
            ]);

        var lineItems = new LabelsExcelImportService()
            .ImportTender(stream)
            .LabelLineItems;

        Assert.Equal(100000.5m, lineItems[0].Quantity);
        Assert.Equal(1250.50m, lineItems[0].Spend);
        Assert.Equal(12.50m, lineItems[0].PricePerThousand);
        Assert.Equal(0.0125m, lineItems[0].Price);
        Assert.Equal(1250.50m, lineItems[0].TheoreticalSpend);
        Assert.Empty(lineItems[0].SourceManualReviewFlags);

        Assert.Equal(100000.5m, lineItems[1].Quantity);
        Assert.Equal(1250.50m, lineItems[1].Spend);
        Assert.Equal(12.50m, lineItems[1].PricePerThousand);
        Assert.Equal(0.0125m, lineItems[1].Price);
        Assert.Equal(1250.50m, lineItems[1].TheoreticalSpend);
        Assert.Empty(lineItems[1].SourceManualReviewFlags);
    }

    [Fact]
    public void ImportedLineItemsCanBeEvaluatedWithManualReviewFlags()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Supplier name", "Spend", "Price per 1,000", "Material", "Winding direction", "Label size"],
            [
                ["LBL-004", "Acme Labels", 100m, 10m, "PP white", "Left", "80x120"],
                ["LBL-005", "", "", "invalid", "", "Left", ""]
            ]);
        var settings = new TenderSettings
        {
            ExpectedMaterial = "PP white",
            ExpectedWindingDirection = "Left",
            ExpectedLabelSize = "80x120"
        };

        var tender = new LabelsExcelImportService().ImportTender(stream, "Imported tender", settings);
        var lineEvaluations = new LineEvaluationService().EvaluateMany(tender.LabelLineItems, tender.Settings);
        var supplierEvaluations = new SupplierAggregationService().AggregateBySupplierName(lineEvaluations);

        Assert.Equal(2, lineEvaluations.Count);
        Assert.False(lineEvaluations[0].RequiresManualReview);
        Assert.True(lineEvaluations[1].RequiresManualReview);
        Assert.Contains(lineEvaluations[1].ManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.PricePerThousand)
            && flag.SourceValue == "invalid");
        Assert.Contains(lineEvaluations[1].ManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.SupplierName));
        Assert.Equal(2, supplierEvaluations.Count);
        Assert.Contains(supplierEvaluations, evaluation => evaluation.SupplierName == "Acme Labels");
        Assert.Contains(supplierEvaluations, evaluation => evaluation.SupplierName == string.Empty);
    }

    private static MemoryStream CreateWorkbookStream(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Labels");

        for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
        {
            worksheet.Cell(1, columnIndex + 1).Value = headers[columnIndex];
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                worksheet.Cell(rowIndex + 2, columnIndex + 1).Value = XLCellValue.FromObject(row[columnIndex]);
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return stream;
    }
}
