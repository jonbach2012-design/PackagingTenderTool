using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Forms;
using PackagingTenderTool.Core.Import;

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

    /// <summary>Main label-tender tab from query (<c>dashboard</c>, <c>settings</c>, …).</summary>
    public string ActiveTab { get; set; } = "dashboard";

    /// <summary>Latest structured import validation (blocking failures, row corrections, warnings).</summary>
    public ImportValidationReport? LastImportValidationReport { get; set; }

    public Func<string, byte[], Task>? ImportExcelAsync { get; set; }

    public string FilterCountry { get; set; } = string.Empty;

    public string FilterSite { get; set; } = string.Empty;

    public string FilterMaterial { get; set; } = string.Empty;

    public string FilterLabelSize { get; set; } = string.Empty;

    public string FilterWinding { get; set; } = string.Empty;

    public string FilterColors { get; set; } = string.Empty;

    public string FilterAdhesive { get; set; } = string.Empty;

    public bool SynergyToggle { get; set; }

    public bool PpwrEffectToggle { get; set; }

    public bool SitesSelectDisabled { get; set; }

    public string[] FilterCountries { get; set; } = [];

    /// <summary>True after tender data is loaded and filter dimensions are available (drives filter column visibility).</summary>
    public bool HasImportedData =>
        FilterCountries is not null && FilterCountries.Length > 0;

    /// <summary>Whether the label-tender filter column is expanded (mirrors MainLayout; used by shell toggle).</summary>
    public bool FilterPanelOpen { get; set; } = true;

    /// <summary>Raised by shell toggle; MainLayout flips <see cref="FilterPanelOpen"/> and filter strip width.</summary>
    public Action? OnToggleFilterPanel { get; set; }

    /// <summary>Raised when shell navigation selects a main view tab (<c>dashboard</c>, <c>matrix</c>, …).</summary>
    public Action<string>? OnTabChanged { get; set; }

    public string[] FilterSitesForCountry { get; set; } = [];

    public string[] FilterMaterials { get; set; } = [];

    public string[] FilterAdhesives { get; set; } = [];

    public IReadOnlyList<string> FilterLabelSizes { get; set; } = [];

    public IReadOnlyList<string> FilterWindings { get; set; } = [];

    public IReadOnlyList<string> FilterColorValues { get; set; } = [];

    /// <summary>Current tender supplier rows (from <c>LabelTender</c> after refresh/import). Drives supplier checkboxes and settings target list.</summary>
    public IReadOnlyList<DrawerSupplierRow> DrawerSuppliers { get; set; } = [];

    public sealed record DrawerSupplierRow(string Id, string Name);

    public IReadOnlySet<string> SelectedSupplierIds { get; set; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public Action<string>? OnCountryChanged { get; set; }

    public Action<string>? OnSiteChanged { get; set; }

    public Action<string>? OnMaterialChanged { get; set; }

    public Action<string>? OnAdhesiveChanged { get; set; }

    public Action<bool>? OnSynergyChanged { get; set; }

    public Action<bool>? OnPpwrEffectChanged { get; set; }

    public Action<string, bool>? OnSupplierSelectionChanged { get; set; }

    public Action<string>? OnLabelSizeChanged { get; set; }

    public Action<string>? OnWindingChanged { get; set; }

    public Action<string>? OnColorsChanged { get; set; }

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
