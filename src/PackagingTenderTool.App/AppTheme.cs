namespace PackagingTenderTool.App;

internal static class AppTheme
{
    public static readonly Color Primary = Color.FromArgb(145, 163, 99);
    public static readonly Color PrimaryDark = Color.FromArgb(111, 130, 80);
    public static readonly Color PrimaryLight = Color.FromArgb(220, 228, 207);
    public static readonly Color PageBackground = Color.FromArgb(246, 247, 243);
    public static readonly Color CardBackground = Color.White;
    public static readonly Color MainText = Color.FromArgb(47, 53, 47);
    public static readonly Color MutedText = Color.FromArgb(102, 112, 95);
    public static readonly Color Warning = Color.FromArgb(201, 138, 46);
    public static readonly Color Error = Color.FromArgb(178, 74, 74);

    public static Font TitleFont(float size = 18F)
    {
        return new Font("Segoe UI Semibold", size, FontStyle.Bold);
    }

    public static Font BodyFont(float size = 9F)
    {
        return new Font("Segoe UI", size);
    }
}
