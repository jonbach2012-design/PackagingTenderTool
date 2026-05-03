using MudBlazor.Services;
using PackagingTenderTool.Blazor;
using PackagingTenderTool.Blazor.Components;
using PackagingTenderTool.Blazor.Models;
using PackagingTenderTool.Blazor.Services;
using PackagingTenderTool.Core.Services;
using PackagingTenderTool.Core.Services.LabelTenderScoring;
using PackagingTenderTool.Core.Import;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

builder.Services.Configure<TcoSettings>(builder.Configuration.GetSection("TcoSettings"));
builder.Services.AddScoped<IScenarioStateService, ScenarioStateService>();
builder.Services.AddScoped<ITcoCalculator, TcoCalculator>();
builder.Services.AddScoped<IMockDataService, MockDataService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ITcoEngineService, TcoEngineService>();
builder.Services.AddScoped<LabelTenderSidebarBridge>();

builder.Services.AddScoped<PackagingProfileSession>();
builder.Services.AddSingleton<ILabelTenderScoringStrategy, RelativeToBestScoringStrategy>();
builder.Services.AddSingleton<LabelTenderScoringService>();
builder.Services.AddSingleton<IEprFeeService, EprFeeService>();
builder.Services.AddSingleton<IRegulatoryService, RegulatoryService>();
builder.Services.AddSingleton<LabelsExcelImportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
    // Smoke checks for DI + calculation paths (no UI). Use: curl -k https://localhost:7144/api/tco-engine-smoke
    app.MapGet("/api/tco-smoke", (ITcoCalculator calc) =>
    {
        var line = new TenderLine("SMOKE-001", "TestSupplier", "DK-East", "DK", "Labels", 10.50m);
        var offer = new SupplierOffer(line, 10.50m, "C", 1.25m, 0.50m, 90m);
        var actual = calc.Calculate(offer);
        var decision = calc.CalculateDecision(offer);
        return Results.Json(new
        {
            path = "ITcoCalculator",
            input = new { offer.BasePrice, offer.MaterialClass, offer.TechnicalFit, offer.SwitchingCost },
            tco = new { actual.Commercial, actual.Technical, actual.Switching, actual.Regulatory, actual.Total },
            decision = new { decision.WeightedDecisionScore, decision.DecisionScoreIndex }
        });
    });

    app.MapGet("/api/tco-engine-smoke", (IServiceProvider sp) =>
    {
        using var scope = sp.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<PackagingProfileSession>();
        session.SelectLabels();
        var engine = scope.ServiceProvider.GetRequiredService<ITcoEngineService>();
        var supplier = LabelTenderDemoSupplierData.Create()[0];
        var dto = engine.CalculateResult(session, supplier);
        return Results.Json(new
        {
            path = "ITcoEngineService",
            supplierId = dto.SupplierId,
            commercial = dto.Commercial,
            epr = dto.Epr,
            switching = dto.Switching,
            moq = dto.Moq,
            totalTco = dto.TotalTco,
            finalCtrScore = dto.FinalCtrScore,
            isCompliant = dto.IsCompliant,
            technicalSummary = dto.TechnicalSummary,
            calculationBreakdown = dto.CalculationBreakdown.Length > 240
                ? string.Concat(dto.CalculationBreakdown.AsSpan(0, 240), "…")
                : dto.CalculationBreakdown
        });
    });
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

