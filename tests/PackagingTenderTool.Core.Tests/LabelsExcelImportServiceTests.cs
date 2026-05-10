using System.IO;

// Tredjepartsbibliotek til at oprette Excel-filer i memory.
using ClosedXML.Excel;

// Projektets egne domæne- og serviceklasser.
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

// NSubstitute er et testbibliotek til mocking.
// Det bruges til at lave kontrollerede erstatninger for afhængigheder,
// fx en fake IEprFeeService, så testen selv bestemmer service-outputtet.
using NSubstitute;

namespace PackagingTenderTool.Core.Tests;

/// <summary>
/// NEVER TRUST AN AI: tests, evidence and execution.
/// 
/// Eksamen-case: automatiske tests for Excel-import i Packaging Tender Tool.
/// 
/// Denne testfil viser, hvordan programmering bruges til at omsætte
/// forretningsregler og Excel-importkrav til præcise, kontrollerbare instruktioner.
///
/// Læringsmål der kan forklares med filen:
/// - Hvad er programmering:
///   Regler og beslutninger bliver omsat til præcis C#-kode,
///   hvor input, handling og forventet output kan kontrolleres.
///
/// - Programstrukturer:
///   Filen viser klasser, metoder, objekter, lister, loops, exceptions,
///   streams, services og assertions.
///
/// - Versionsstyring:
///   Testene fungerer som sikkerhedsnet i Git. Hvis en AI-agent eller udvikler
///   ændrer importlogikken forkert, fejler dotnet test, og man kan rulle tilbage
///   til en tidligere fungerende commit.
///
/// - Programmering med agenter:
///   Testene fungerer som en eksekverbar specifikation for Cursor/Codex.
///   I stedet for at stole på AI-genereret kode, definerer testene den ønskede adfærd.
///   Agenten skal få implementationen til at bestå testene.
///   Hvis testen skrives før implementationen, er det TDD.
///   Hvis testen bruges som kravspecifikation for agenten, er det SDD
///   I begge tilfælde bliver automatiske tests brugt som acceptkriterier.
///
/// - Debugging:
///   Hver test isolerer én konkret fejltype eller forretningsregel,
///   så fejl kan findes hurtigt og præcist.
///   Kan dokumenteres via GitHub
///   - teknik til debugging: 858bb80
///   - overvåge kørende programkode: 1e9fdd8 
///   - tilføjede automatiske log-linier: 426527b
///   - afvisning af 2 AI-forslag: 1e9fdd8
///
/// - Automatiske test:
///   xUnit [Fact]-metoder og Assert-kald gør det muligt at køre beviset igen og igen
///   med dotnet test uden manuel GUI-test.
///   Hver FACT er et automatisk testpunkt, der kan kontrolleres med dotnet test.
///   136 / 136 automatiske kontroller gennemført -> direkte forbundet til README 
///
/// - Projektstruktur:
///   ClosedXML bruges til at oprette Excel-workbooks i memory.
///   NSubstitute bruges til mocking af afhængigheder.
///   Services og strategy-klasser viser opdeling af ansvar.
///
/// Kort sagt:
/// Filen er både testkode, dokumentation og kvalitetskontrol.
/// Den viser hvordan AI-genereret eller AI-assisteret kode kan kontrolleres
/// med automatiske tests i stedet for bare at stole på, at det “ser rigtigt ud”.
/// </summary>


// Testklassen for LabelsExcelImportService.
public sealed class LabelsExcelImportServiceTests
{
    [Fact]
    public void ImportTenderMapsLabelsColumnsIntoLineItems()
    {
        // ARRANGE:
        // Forbered data, objekter og afhængigheder.
        // Her bygges et realistisk Excel-ark i hukommelsen, så testen ikke afhænger af en fysisk .xlsx-fil.
        // Hvorfor: testen bliver hurtig, gentagelig og nem at køre med dotnet test, GitHub eller Cursor.
        // Effekt: vi kan bevise at standardkolonner bliver mappet korrekt til LabelLineItem.
        // Risiko / forbedring: hvis mange kolonner ændres, kan testen blive lang; evt. kan testdata senere deles i fixtures.
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
        // ARRANGE:
        // Forbereder et Excel-ark med alternative kolonnenavne som ITEM NO., Supplier, Qty og Price per 1000.
        // Hvorfor: rigtige leverandørfiler bruger sjældent perfekte headers.
        // Effekt: testen beviser at importeren er robust over for praktiske Excel-varianter.
        // Risiko / forbedring: for mange aliaser kan gøre importlogikken uklar; alias-regler bør derfor holdes samlet og testet.
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
        // ARRANGE:
        // Forbereder en negativ test med ugyldige talværdier.
        // Hvorfor: importen må ikke crashe eller lave skjulte forkerte beregninger på dårlig data.
        // Effekt: ugyldige tal bliver til null, og fejlen gemmes som ManualReviewFlag.
        // Risiko / forbedring: UI skal vise disse flags tydeligt, ellers opdager brugeren ikke dataproblemet.
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

        // Testen kontrollerer 2 ting:
        // 1. Ugyldige numeriske værdier bliver ikke brugt i modellen.
        //    De sættes til null, så de ikke kan indgå i forkerte beregninger.
        // 2. Fejlen bevares som et ManualReviewFlag.
        //    Dermed kan brugeren se hvilket felt der fejlede,
        //    hvilken original værdi der kom fra Excel,
        //    og hvor alvorlig fejlen er.
        //
        // Det giver både datakvalitet og sporbarhed:
        // dårlig inputdata bliver hverken skjult, ignoreret eller brugt forkert.
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
        // ARRANGE:
        // Forbereder to rækker med forskellige decimalformater: nordisk format og internationalt format.
        // Hvorfor: Excel-filer fra forskellige lande bruger både komma og punktum forskelligt.
        // Effekt: testen beskytter mod klassiske parsing-fejl i valuta, mængder og priser.
        // Risiko / forbedring: parsing-regler bør være deterministic og ikke afhænge af maskinens lokale kulturindstillinger.
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
        // ARRANGE:
        // Forbereder importdata, tender settings og en mock af EPR fee service.
        // Hvorfor: testen skal kontrollere hele flowet fra import til linjeevaluering og leverandøraggregering.
        // Effekt: vi beviser at gode linjer kan evalueres, mens dårlige linjer markeres til manuel gennemgang.
        // Risiko / forbedring: testen er tættere på integrationstest end ren unit test; den giver høj værdi, men kan blive større at vedligeholde.
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
        // ARRANGE:
        // Forbereder Excel-data med regulatory/compliance-kolonner.
        // Hvorfor: PPWR/EPR-relaterede felter skal ikke kun ligge i Excel, men ind i datamodellen.
        // Effekt: testen beviser at vægt, mono-materiale, separation, genanvendelighed og traceability mappes korrekt.
        // Risiko / forbedring: boolean parsing bør dokumenteres tydeligt, så ja/nej, true/false og 1/0 ikke bliver tilfældig magi.
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
        // ARRANGE:
        // Forbereder en linje med LDPE-materiale, site og label weight.
        // Hvorfor: materialetype skal kunne drive EPR-kategori og fee-beregning.
        // Effekt: testen beviser både mapping til Flexibles og efterfølgende EPR fee.
        // Risiko / forbedring: placeholder rates bør senere erstattes af datadrevne satser, men testen beskytter selve flowet.
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

        Assert.Equal(1.2m, lineEvaluation.EprFee);
    }

    [Fact]
    public void ImportedRegulatoryValuesContributeToEvaluationTotalAndClassification()
    {
        // ARRANGE:
        // Forbereder en linje hvor alle regulatory krav matcher tender settings.
        // Hvorfor: regulatory data skal påvirke score og klassifikation, ikke bare importeres passivt.
        // Effekt: testen beviser at korrekt compliance-data giver 100 i regulatory score og Recommended classification.
        // Risiko / forbedring: vægtningen mellem commercial, technical og regulatory bør have egne tests, hvis scoring bliver mere avanceret.
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
        // ARRANGE:
        // Forbereder DSH-style headers med valutaangivelser som Spend (NOK) og Price (DKK).
        // Hvorfor: virkelige workbooks har ofte valuta i headeren.
        // Effekt: testen beviser at site, spend, price og theoretical spend stadig mappes korrekt.
        // Risiko / forbedring: valuta i headeren mappes her som kolonnegenkendelse; egentlig valutakonvertering bør testes separat.
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
        // ARRANGE:
        // Forbereder flere workbook-varianter med Location, Plant, Spend (SEK/DKK/EUR) og Price (EUR/NOK/DKK).
        // Hvorfor: samme begreb kan hedde forskelligt på tværs af lande, sites og leverandører.
        // Effekt: testen beskytter de vigtigste header-aliaser fra DSH-lignende filer.
        // Risiko / forbedring: hvis alias-listen vokser, bør den måske ligge i en central mapping-struktur.
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
        // ARRANGE:
        // Forbereder en workbook med et cover sheet først og tender data på andet sheet.
        // Hvorfor: mange Excel-filer har forsider, noter eller executive summaries før selve dataarket.
        // Effekt: testen beviser at importeren søger efter korrekt header i stedet for blindt at læse første sheet.
        // Risiko / forbedring: ved meget store workbooks bør header-søgning være effektiv og have tydelige stopregler.
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
        // ARRANGE:
        // Forbereder et realistisk DSH-lignende ark med norske tegn, store tal og praktiske kolonnenavne.
        // Hvorfor: testdata bør ligne virkeligheden, ikke kun pæne skoleeksempler.
        // Effekt: testen beskytter importen mod regressioner på en central bruger-case.
        // Risiko / forbedring: en egentlig anonymiseret fixture-fil kunne senere supplere memory-baserede tests.
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
        // ARRANGE:
        // Forbereder et ark der mangler Material, som er nødvendig for at genkende et labels tender.
        // Hvorfor: systemet skal ikke gætte på ufuldstændige eller forkerte ark.
        // Effekt: testen beviser at arket afvises med HeaderNotRecognizedMarker.
        // Risiko / forbedring: brugerbeskeden bør forklare manglen uden at blive for teknisk.
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
        // ARRANGE:
        // Forbereder et ark der ligner et labels tender, men mangler Supplier name.
        // Hvorfor: der er forskel på ukendt header og kendt ark med manglende required column.
        // Effekt: testen beviser at brugeren får en præcis missing required-fejl.
        // Risiko / forbedring: required columns bør være dokumenteret ét sted, så UI, import og tests ikke driver fra hinanden.
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
        // ARRANGE:
        // Forbereder et ark hvor Price per 1,000 er eneste prisrelaterede kolonne.
        // Hvorfor: arket skal stadig kunne genkendes som tender-data, selvom Price og Spend mangler.
        // Effekt: testen beviser den konkrete header identity-regel.
        // Risiko / forbedring: identity-regler bør holdes simple, ellers bliver de svære at forklare og debugge.
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
        // ARRANGE:
        // Forbereder et ark med Theoretical spend (DKK) som header-alias.
        // Hvorfor: theoretical spend kan komme med valutaangivelse i headeren.
        // Effekt: testen beviser at værdien stadig lander i TheoreticalSpend.
        // Risiko / forbedring: hvis flere valutaer understøttes, bør de testes systematisk som parameteriserede tests.
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
        // ARRANGE:
        // Forbereder en teknisk workbook-open exception.
        // Hvorfor: brugeren skal have en forståelig besked, ikke en intern marker eller stack trace.
        // Effekt: testen beviser at fil-læsefejl mappes til korrekt brugerbesked.
        // Risiko / forbedring: logning bør stadig gemme tekniske detaljer, mens UI viser en enkel tekst.
        var inner = new InvalidDataException("EOF");
        var ex = new InvalidOperationException(LabelsExcelImportService.WorkbookOpenFailedMarker, inner);
        var msg = LabelTenderImportFailureMessage.Format(ex);

        Assert.Equal(LabelTenderImportFailureMessage.WorkbookCouldNotReadUserMessage, msg);
        Assert.DoesNotContain("header could not be recognized", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportFailureMessage_header_not_recognized_maps_to_distinct_user_text()
    {
        // ARRANGE:
        // Forbereder en exception hvor workbook kan åbnes, men header ikke genkendes.
        // Hvorfor: det er en anden fejl end at filen slet ikke kan læses.
        // Effekt: testen beviser at brugeren får en særskilt header-fejl.
        // Risiko / forbedring: beskeden kan forbedres med eksempler på forventede kolonnenavne.
        var ex = new InvalidOperationException(LabelsExcelImportService.HeaderNotRecognizedMarker);
        var msg = LabelTenderImportFailureMessage.Format(ex);

        Assert.Contains("workbook opened", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("header could not be recognized", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportFailureMessage_missing_required_column_marker_formats_with_column_names()
    {
        // ARRANGE:
        // Forbereder en missing required column exception med kolonnenavn.
        // Hvorfor: brugeren skal vide præcis hvilken kolonne der mangler i Excel.
        // Effekt: testen beviser at formatteren laver en konkret og handlingsrettet fejlbesked.
        // Risiko / forbedring: ved flere manglende kolonner kunne beskeden vise hele listen på én gang.
        var ex = new InvalidOperationException($"{LabelsExcelImportService.MissingRequiredColumnMarker}:Supplier name");
        var msg = LabelTenderImportFailureMessage.Format(ex);

        Assert.Equal("Import failed: Missing required column 'Supplier name'.", msg);
    }

    /// <summary>
    /// Vigtigt:
    /// Denne metode tester ikke Excel eller ClosedXML i sig selv.
    /// Den bygger kun kontrolleret testinput, så importservicen kan testes.
   
    /// Hjælpemetode til at bygge Excel-testfiler i hukommelsen.
    ///
    /// Formål:
    /// Metoden bruges i ARRANGE-delen af testene.
    /// Den opretter et realistisk Excel-ark ud fra headers og rækker,
    /// så hver test kan definere sit eget input uden at bruge en fysisk .xlsx-fil.
    ///
    /// Hvorfor:
    /// Testene bliver hurtige, gentagelige og uafhængige af lokale filstier.
    ///
    /// Effekt:
    /// Hver test kan isolere ét scenarie, fx korrekt kolonnemapping,
    /// manglende headers, ugyldige talværdier eller forskellige decimalformater.
    ///
    /// Risiko / mulig forbedring:
    /// Memory-workbooks er gode til isolerede tests, men de kan suppleres
    /// med få anonymiserede fixture-filer, hvis man vil teste endnu tættere
    /// på rigtige leverandørark.
    ///
    /// Vigtigt:
    /// Denne metode tester ikke Excel eller ClosedXML i sig selv.
    /// Den bygger kun kontrolleret testinput, så importservicen kan testes.
    /// </summary>
    public static MemoryStream CreateWorkbookStream(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        // ClosedXML bruges til at oprette en Excel-workbook direkte i memory.
        // Det betyder, at testen ikke skal læse eller skrive en fysisk fil på disk.
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Labels");

        // Første loop skriver header-rækken i Excel-arket.
        //
        // Der bruges en for-løkke, fordi vi har brug for columnIndex
        // til at placere hver header i den korrekte Excel-kolonne.
        //
        // C#-lister er 0-baserede: headers[0], headers[1], ...
        // Excel-celler er 1-baserede: Cell(1,1), Cell(1,2), ...
        // Derfor bruges columnIndex + 1.
        for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
        {
            worksheet.Cell(1, columnIndex + 1).Value = headers[columnIndex];
        }

        // Nested loop skriver data-rækkerne i Excel-arket.
        //
        // Ydre loop går gennem rækkerne.
        // Indre loop går gennem kolonnerne i den aktuelle række.
        //
        // Det passer til Excel, fordi et regneark er et gitter:
        // række + kolonne = én bestemt celle.
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                // rowIndex + 2 bruges, fordi række 1 er headers,
                // og første datarække derfor skal starte på række 2.
                //
                // columnIndex + 1 bruges, fordi Excel-kolonner starter ved 1,
                // mens C#-indekser starter ved 0.
                worksheet.Cell(rowIndex + 2, columnIndex + 1).Value = XLCellValue.FromObject(row[columnIndex]);
            }
        }

        // Workbook gemmes som en MemoryStream.
        // Importservicen forventer en stream, så testen giver den samme type input,
        // som den ville få fra en rigtig uploadet Excel-fil.
        var stream = new MemoryStream();
        workbook.SaveAs(stream);

        // Position = 0 nulstiller læsepositionen.
        // Uden dette ville importeren starte efter slutningen af streamen
        // og derfor ikke kunne læse workbooken korrekt.
        stream.Position = 0;

        return stream;
    }
}