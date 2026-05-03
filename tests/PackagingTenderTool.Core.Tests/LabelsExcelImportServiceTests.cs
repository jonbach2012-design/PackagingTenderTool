using System.IO;
using ClosedXML.Excel;
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;
using NSubstitute;

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
            ["ITEM NO.", "Item name", "Material", "Supplier", "Qty", "Price per 1000", "No of colors"],
            [
                ["LBL-002", "It", "PP", "Beta Packaging", 25000m, 8.75m, 2]
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
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000", "No. of colors"],
            [
                ["LBL-003", "It", "PP", "Gamma Labels", "not-a-number", "", "bad-price", 2.5m]
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
            && (flag.SourceValue == "2.5" || flag.SourceValue == "2,5")
            && flag.Severity == ManualReviewSeverity.Error);
    }

    [Fact]
    public void ImportTenderParsesCommaAndDotDecimalSeparators()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000", "Price", "Theoretical spend"],
            [
                ["LBL-004", "N", "PP", "Acme Labels", "100000,5", "1.250,50", "12,50", "0,0125", "1.250,50"],
                ["LBL-005", "N", "PP", "Beta Packaging", "100000.5", "1,250.50", "12.50", "0.0125", "1,250.50"]
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
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000", "Winding direction", "Label size"],
            [
                ["LBL-004", "N", "PP white", "Acme Labels", 1m, 100m, 10m, "Left", "80x120"],
                ["LBL-005", "N", "invalid", "Contested Supplier", 1m, 0m, "invalid", "Left", ""]
            ]);
        var settings = new TenderSettings
        {
            ExpectedMaterial = "PP white",
            ExpectedWindingDirection = "Left",
            ExpectedLabelSize = "80x120"
        };

        var tender = new LabelsExcelImportService().ImportTender(stream, "Imported tender", settings);

        // Strategy now applies EPR fee impact; ensure test data contains valid EPR inputs.
        foreach (var lineItem in tender.LabelLineItems)
        {
            lineItem.LabelWeightGrams ??= 1.0m;
            lineItem.EprSchemes.Add(new EprSchemeInfo { CountryCode = "DK", Category = "Labels" });
        }

        var epr = Substitute.For<IEprFeeService>();
        epr.TryCalculateFee(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), out Arg.Any<decimal>(), out Arg.Any<ManualReviewFlag?>())
            .Returns(call =>
            {
                var weightKg = (decimal)call[2]!;
                call[3] = decimal.Round(weightKg * 0.50m, 4);
                call[4] = null;
                return true;
            });

        var lineEvaluations = new LineEvaluationService(new LabelsEvaluationStrategy(epr))
            .EvaluateMany(tender.LabelLineItems, tender.Settings);
        var supplierEvaluations = new SupplierAggregationService().AggregateBySupplierName(lineEvaluations);

        Assert.Equal(2, lineEvaluations.Count);
        Assert.False(lineEvaluations[0].RequiresManualReview);
        Assert.True(lineEvaluations[1].RequiresManualReview);
        Assert.Contains(lineEvaluations[1].ManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.PricePerThousand)
            && flag.SourceValue == "invalid");
        Assert.Contains(lineEvaluations[1].ManualReviewFlags, flag =>
            flag.FieldName == nameof(LabelLineItem.LabelSize));
        Assert.Equal(2, supplierEvaluations.Count);
        Assert.Contains(supplierEvaluations, evaluation => evaluation.SupplierName == "Acme Labels");
        Assert.Contains(supplierEvaluations, evaluation => evaluation.SupplierName == "Contested Supplier");
    }

    [Fact]
    public void ImportTenderMapsRegulatoryColumnsIntoLineItems()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000", "Label weight (g)", "Mono-material design", "Easy separation", "Reusable or recyclable material direction", "Traceability"],
            [
                ["LBL-006", "N", "PP", "Acme Labels", 1m, 100m, 10m, "1,8", "yes", "true", "1", "ja"],
                ["LBL-007", "N", "PP", "Beta Packaging", 1m, 100m, 20m, "2.5", "no", "false", "0", "nej"]
            ]);

        var lineItems = new LabelsExcelImportService()
            .ImportTender(stream)
            .LabelLineItems;

        Assert.Equal(1.8m, lineItems[0].LabelWeightGrams);
        Assert.True(lineItems[0].IsMonoMaterial);
        Assert.True(lineItems[0].IsEasyToSeparate);
        Assert.True(lineItems[0].IsReusableOrRecyclableMaterial);
        Assert.True(lineItems[0].HasTraceability);
        Assert.Equal(2.5m, lineItems[1].LabelWeightGrams);
        Assert.False(lineItems[1].IsMonoMaterial);
        Assert.False(lineItems[1].IsEasyToSeparate);
        Assert.False(lineItems[1].IsReusableOrRecyclableMaterial);
        Assert.False(lineItems[1].HasTraceability);
    }

    [Fact]
    public void ImportTenderMapsLdpeMaterialToFlexiblesAndCalculatesEprFee()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Site", "Quantity", "Spend", "Price per 1,000", "Label weight (g)"],
            [
                ["LBL-009", "N", "LDPE", "Acme Labels", "DK01", 1m, 100m, 10m, 1000m]
            ]);

        var tender = new LabelsExcelImportService().ImportTender(stream, "Imported tender");
        var lineItem = tender.LabelLineItems.Single();

        Assert.Single(lineItem.EprSchemes);
        Assert.Equal("DK", lineItem.EprSchemes[0].CountryCode);
        Assert.Equal("Flexibles", lineItem.EprSchemes[0].Category);

        var result = new LabelsTenderEvaluationService().Evaluate(tender);
        var lineEvaluation = result.LineEvaluations.Single();

        // Flexibles placeholder rate = 1.20 per kg; 1000g = 1kg => fee 1.2
        Assert.Equal(1.2m, lineEvaluation.EprFee);
    }

    [Fact]
    public void ImportedRegulatoryValuesContributeToEvaluationTotalAndClassification()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Spend", "Price per 1,000", "Winding direction", "Label size", "Label weight (g)", "Mono-material design", "Easy separation", "Reusable or recyclable material direction", "Traceability"],
            [
                ["LBL-008", "N", "PP white", "Acme Labels", 1m, 100m, 10m, "Left", "80x120", 1.5m, true, true, true, true]
            ]);
        var settings = new TenderSettings
        {
            ExpectedMaterial = "PP white",
            ExpectedWindingDirection = "Left",
            ExpectedLabelSize = "80x120",
            MaximumLabelWeightGrams = 2m,
            ExpectedMonoMaterial = true,
            ExpectedEasySeparation = true,
            ExpectedReusableOrRecyclableMaterial = true,
            ExpectedTraceability = true
        };

        var tender = new LabelsExcelImportService().ImportTender(stream, "Imported tender", settings);

        // Strategy now applies EPR fee impact; ensure test data contains valid EPR inputs.
        foreach (var lineItem in tender.LabelLineItems)
        {
            lineItem.EprSchemes.Add(new EprSchemeInfo { CountryCode = "DK", Category = "Labels" });
        }

        var epr = Substitute.For<IEprFeeService>();
        epr.TryCalculateFee(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), out Arg.Any<decimal>(), out Arg.Any<ManualReviewFlag?>())
            .Returns(call =>
            {
                var weightKg = (decimal)call[2]!;
                call[3] = decimal.Round(weightKg * 0.50m, 4);
                call[4] = null;
                return true;
            });

        var lineEvaluations = new LineEvaluationService(new LabelsEvaluationStrategy(epr))
            .EvaluateMany(tender.LabelLineItems, tender.Settings);
        var supplierEvaluation = new SupplierAggregationService()
            .AggregateBySupplierName(lineEvaluations)
            .Single();
        new SupplierClassificationService().ApplyClassification(supplierEvaluation);

        Assert.Equal(100m, lineEvaluations.Single().ScoreBreakdown.Regulatory);
        Assert.Equal(100m, supplierEvaluation.ScoreBreakdown.Regulatory);
        Assert.Equal(100m, supplierEvaluation.ScoreBreakdown.Total);
        Assert.Equal(SupplierClassification.Recommended, supplierEvaluation.Classification);
    }

    [Fact]
    public void Import_maps_dsh_style_currency_headers_to_site_spend_price_and_theoretical_spend()
    {
        using var stream = CreateWorkbookStream(
            [
                "Item no", "Item name", "Supplier name", "DSH Site", "Quantity", "Spend (NOK)",
                "Price per 1,000", "Price (DKK)", "Theoretical spend (NOK)", "Material"
            ],
            [
                ["X-1", "N", "Sup", "DK01", 1000m, 500m, 10m, 0.5m, 480m, "PP"]
            ]);

        var line = new LabelsExcelImportService().ImportTender(stream).LabelLineItems.Single();
        Assert.Equal("DK01", line.Site);
        Assert.Equal(500m, line.Spend);
        Assert.Equal(10m, line.PricePerThousand);
        Assert.Equal(0.5m, line.Price);
        Assert.Equal(480m, line.TheoreticalSpend);
    }

    [Fact]
    public void Import_maps_location_plant_and_spend_price_currency_headers_used_on_dsh_workbooks()
    {
        using var streamLocation = CreateWorkbookStream(
            [
                "Item no", "Item name", "Material", "Supplier name", "Location", "Quantity", "Spend (SEK)",
                "Price per 1,000", "Price (EUR)"
            ],
            [
                ["L-1", "It", "PP", "Sup", "SE99", 10m, 77m, 1m, 6.6m]
            ]);
        var loc = new LabelsExcelImportService().ImportTender(streamLocation).LabelLineItems.Single();
        Assert.Equal("SE99", loc.Site);
        Assert.Equal(77m, loc.Spend);
        Assert.Equal(6.6m, loc.Price);

        using var streamPlant = CreateWorkbookStream(
            [
                "Item no", "Item name", "Material", "Supplier name", "Plant", "Quantity", "Spend (DKK)",
                "Price per 1,000", "Price (NOK)"
            ],
            [
                ["P-1", "It", "PP", "Sup", "PL01", 5m, 88m, 2m, 7.7m]
            ]);
        var plant = new LabelsExcelImportService().ImportTender(streamPlant).LabelLineItems.Single();
        Assert.Equal("PL01", plant.Site);
        Assert.Equal(88m, plant.Spend);
        Assert.Equal(7.7m, plant.Price);

        using var streamEurSpend = CreateWorkbookStream(
            [
                "Item no", "Item name", "Material", "Supplier name", "DSH Site", "Quantity", "Spend (EUR)",
                "Price per 1,000", "Price (DKK)"
            ],
            [
                ["E-1", "It", "PP", "Sup", "DK02", 3m, 42m, 1m, 2.2m]
            ]);
        var eurSpend = new LabelsExcelImportService().ImportTender(streamEurSpend).LabelLineItems.Single();
        Assert.Equal("DK02", eurSpend.Site);
        Assert.Equal(42m, eurSpend.Spend);
        Assert.Equal(2.2m, eurSpend.Price);
    }

    [Fact]
    public void Import_finds_labels_header_on_second_worksheet_when_first_sheet_has_no_item_no_column()
    {
        using var workbook = new XLWorkbook();
        var cover = workbook.Worksheets.Add("Cover");
        cover.Cell(1, 1).Value = "Executive summary";
        cover.Cell(2, 1).Value = "Not a tender grid";

        var tenderWs = workbook.Worksheets.Add("Tender data");
        var headers = new[]
        {
            "Item no", "Item name", "Supplier name", "DSH Site", "Quantity", "Spend (NOK)",
            "Price per 1,000", "Price (DKK)", "Theoretical spend (NOK)", "Material"
        };
        for (var i = 0; i < headers.Length; i++)
            tenderWs.Cell(1, i + 1).Value = headers[i];
        tenderWs.Cell(2, 1).Value = "Y-2";
        tenderWs.Cell(2, 2).Value = "Part";
        tenderWs.Cell(2, 3).Value = "Vendor";
        tenderWs.Cell(2, 4).Value = "SE02";
        tenderWs.Cell(2, 5).Value = 2000m;
        tenderWs.Cell(2, 6).Value = 100m;
        tenderWs.Cell(2, 7).Value = 5m;
        tenderWs.Cell(2, 8).Value = 0.1m;
        tenderWs.Cell(2, 9).Value = 99m;
        tenderWs.Cell(2, 10).Value = "LDPE";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var tender = new LabelsExcelImportService().ImportTender(stream, "Second sheet");
        var line = tender.LabelLineItems.Single();
        Assert.Equal("SE02", line.Site);
        Assert.Equal(100m, line.Spend);
    }

    [Fact]
    public void Dsh_style_header_row_is_recognized_and_imports_successfully()
    {
        using var stream = CreateWorkbookStream(
            [
                "Item no", "Item name", "Supplier name", "DSH Site", "Quantity", "Spend (NOK)",
                "Price per 1,000", "Price (DKK)", "Theoretical spend (NOK)", "Label size",
                "Winding direction", "Material", "Reel diameter / pcs per roll", "No. of colors", "Comment"
            ],
            [
                ["540119", "Item A", "FLEXOPRINT AS", "Jæren", 1533600m, 282468m, 184.19m, 117.75m, 290592m, "100X169", "OUT Bottom first", "PP top white", "3.500", 5, "DONE"]
            ]);

        var tender = new LabelsExcelImportService().ImportTender(stream, "DSH");
        var line = tender.LabelLineItems.Single();
        Assert.Equal("540119", line.ItemNo);
        Assert.Equal("Jæren", line.Site);
        Assert.Equal(282468m, line.Spend);
        Assert.Equal(117.75m, line.Price);
    }

    [Fact]
    public void Header_missing_material_is_not_recognized_as_labels_tender()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Item name", "Supplier name", "Quantity", "Spend", "Price per 1,000"],
            [
                ["X", "N", "S", 1m, 1m, 1m]
            ]);

        var ex = Assert.Throws<InvalidOperationException>(() => _ = new LabelsExcelImportService().ImportTender(stream));
        Assert.Equal(LabelsExcelImportService.HeaderNotRecognizedMarker, ex.Message);
    }

    [Fact]
    public void Header_missing_supplier_column_fails_missing_required_not_header_not_recognized()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Quantity", "Spend", "Price per 1,000"],
            [
                ["X", "N", "PP", 1m, 1m, 1m]
            ]);

        var ex = Assert.Throws<InvalidOperationException>(() => _ = new LabelsExcelImportService().ImportTender(stream));
        Assert.StartsWith(LabelsExcelImportService.MissingRequiredColumnMarker + ":", ex.Message);
        Assert.Contains("Supplier name", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Price_per_thousand_alone_satisfies_price_like_identity_for_recognition()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Price per 1,000"],
            [
                ["Z-1", "N", "PP", "Sup", 10m, 5m]
            ]);

        var line = new LabelsExcelImportService().ImportTender(stream).LabelLineItems.Single();
        Assert.Equal(5m, line.PricePerThousand);
    }

    [Fact]
    public void Import_theoretical_spend_dkk_header_alias_maps()
    {
        using var stream = CreateWorkbookStream(
            ["Item no", "Item name", "Material", "Supplier name", "Quantity", "Theoretical spend (DKK)"],
            [
                ["Z-2", "N", "PP", "Sup", 2m, 99m]
            ]);

        var line = new LabelsExcelImportService().ImportTender(stream).LabelLineItems.Single();
        Assert.Equal(99m, line.TheoreticalSpend);
    }

    [Fact]
    public void ImportFailureMessage_workbook_open_failed_maps_to_workbook_read_user_text()
    {
        var inner = new InvalidDataException("EOF");
        var ex = new InvalidOperationException(LabelsExcelImportService.WorkbookOpenFailedMarker, inner);
        var msg = LabelTenderImportFailureMessage.Format(ex);
        Assert.Equal(LabelTenderImportFailureMessage.WorkbookCouldNotReadUserMessage, msg);
        Assert.DoesNotContain("header could not be recognized", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportFailureMessage_header_not_recognized_maps_to_distinct_user_text()
    {
        var ex = new InvalidOperationException(LabelsExcelImportService.HeaderNotRecognizedMarker);
        var msg = LabelTenderImportFailureMessage.Format(ex);
        Assert.Contains("workbook opened", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("header could not be recognized", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportFailureMessage_missing_required_column_marker_formats_with_column_names()
    {
        var ex = new InvalidOperationException($"{LabelsExcelImportService.MissingRequiredColumnMarker}:Supplier name");
        var msg = LabelTenderImportFailureMessage.Format(ex);
        Assert.Equal("Import failed: Missing required column 'Supplier name'.", msg);
    }

    public static MemoryStream CreateWorkbookStream(
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
