using MudBlazor.Services;
using PackagingTenderTool.Blazor;
using PackagingTenderTool.Blazor.Components;
using PackagingTenderTool.Core.Services;
using PackagingTenderTool.Core.Services.LabelTenderScoring;
using PackagingTenderTool.Core.Import;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

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

