using Microsoft.AspNetCore.Components.Forms;

namespace PackagingTenderTool.Blazor.Services;

/// <summary>
/// Syncs Label Tender page state with the shell sidebar (import, filters). Scoped per circuit.
/// </summary>
public sealed class LabelTenderSidebarBridge
{
    public string ImportStatus { get; set; } = string.Empty;

    public bool ImportInProgress { get; set; }

    /// <summary>Last uploaded Excel file name (sidebar display).</summary>
    public string LastImportedFileName { get; set; } = string.Empty;

    public Func<InputFileChangeEventArgs, Task>? ImportExcelAsync { get; set; }

    public string FilterCountry { get; set; } = string.Empty;

    public string FilterSite { get; set; } = string.Empty;

    public string FilterMaterial { get; set; } = string.Empty;

    public string FilterAdhesive { get; set; } = string.Empty;

    public bool SynergyToggle { get; set; }

    public bool SitesSelectDisabled { get; set; }

    public string[] FilterCountries { get; set; } = [];

    public string[] FilterSitesForCountry { get; set; } = [];

    public string[] FilterMaterials { get; set; } = [];

    public string[] FilterAdhesives { get; set; } = [];

    /// <summary>Current tender supplier rows (from <c>LabelTender</c> after refresh/import). Drives supplier checkboxes and settings target list.</summary>
    public IReadOnlyList<DrawerSupplierRow> DrawerSuppliers { get; set; } = [];

    public sealed record DrawerSupplierRow(string Id, string Name);

    public Action<string>? OnCountryChanged { get; set; }

    public Action<string>? OnSiteChanged { get; set; }

    public Action<string>? OnMaterialChanged { get; set; }

    public Action<string>? OnAdhesiveChanged { get; set; }

    public Action<bool>? OnSynergyChanged { get; set; }

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
