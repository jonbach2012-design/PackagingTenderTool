using MudBlazor;

namespace PackagingTenderTool.Blazor.Components.Layout;

public static class AppTheme
{
    // Cursor instruction: "Configure the MudTheme using these exact colors"
    public static MudTheme Scandi { get; } = new MudTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#485230",          // Din grønne accent (Sustainability & Action)
            Secondary = "#57606f",        // Muted slate (secondary UI)
            AppbarBackground = "#FFFFFF",
            AppbarText = "#2D3436",
            Background = "#FFFFFF",
            Surface = "#FFFFFF",
            TextPrimary = "#2D3436",      // Mørk slate (ikke sort)
            TextSecondary = "#5c6b76",
            ActionDefault = "#485230",
            DrawerBackground = "#485230",
            DrawerText = "#ffffff",
            DrawerIcon = "rgba(255,255,255,0.75)"
        },
        Typography = new Typography()
        {
            Default = new Default
            {
                FontFamily = new[] { "Inter", "Segoe UI", "sans-serif" },
                FontSize = "0.9375rem",
                LineHeight = 1.55,
                LetterSpacing = "normal"
            },
            H3 = new H3 { FontFamily = new[] { "Inter", "Segoe UI", "sans-serif" }, FontWeight = 700 },
            H4 = new H4 { FontFamily = new[] { "Inter", "Segoe UI", "sans-serif" }, FontWeight = 700 },
            H5 = new H5 { FontFamily = new[] { "Inter", "Segoe UI", "sans-serif" }, FontWeight = 700 },
            H6 = new H6 { FontFamily = new[] { "Inter", "Segoe UI", "sans-serif" }, FontWeight = 600 }
        }
    };

    /// <summary>Alias for layouts that still reference <see cref="Default"/>.</summary>
    public static MudTheme Default => Scandi;
}
