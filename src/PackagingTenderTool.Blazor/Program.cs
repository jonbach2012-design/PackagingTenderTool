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
builder.Services.AddScoped<ScenarioStateService>();
builder.Services.AddScoped<ITcoCalculator, TcoCalculator>();
builder.Services.AddScoped<MockDataService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<TcoEngineService>();

builder.Services.AddScoped<PackagingProfileSession>();
builder.Services.AddSingleton<ILabelTenderScoringStrategy, RelativeToBestScoringStrategy>();
builder.Services.AddSingleton<LabelTenderScoringService>();
builder.Services.AddSingleton<IEprFeeService, EprFeeService>();
builder.Services.AddSingleton<RegulatoryService>();
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

