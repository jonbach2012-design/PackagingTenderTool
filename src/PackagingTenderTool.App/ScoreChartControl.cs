namespace PackagingTenderTool.App;

using System.ComponentModel;

internal enum ScoreChartMode
{
    TotalScoreBySupplier,
    ScoreDimensions
}

internal sealed class ScoreChartControl : Control
{
    private IReadOnlyList<SupplierResultRow> rows = [];

    public ScoreChartControl()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.CardBackground;
        ForeColor = AppTheme.MainText;
        Font = AppTheme.BodyFont(8.5F);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ScoreChartMode Mode { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ChartTitle { get; set; } = string.Empty;

    public void SetRows(IReadOnlyList<SupplierResultRow> supplierRows)
    {
        rows = supplierRows ?? [];
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        try
        {
            PaintChart(e.Graphics);
        }
        catch
        {
            PaintPlaceholder(e.Graphics, "Chart could not be rendered.");
        }
    }

    private void PaintChart(Graphics graphics)
    {
        graphics.Clear(AppTheme.CardBackground);
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bounds = ClientRectangle;
        if (bounds.Width < 140 || bounds.Height < 120)
        {
            PaintPlaceholder(graphics, "Chart area is too small.");
            return;
        }

        bounds.Inflate(-12, -10);
        using var titleFont = AppTheme.TitleFont(10F);
        using var titleBrush = new SolidBrush(AppTheme.MainText);
        using var axisPen = new Pen(AppTheme.PrimaryLight, 1);

        graphics.DrawString(ShortText(ChartTitle, 42), titleFont, titleBrush, bounds.Location);
        var plot = new Rectangle(bounds.Left, bounds.Top + 34, Math.Max(1, bounds.Width), Math.Max(1, bounds.Height - 40));
        if (plot.Width < 80 || plot.Height < 60)
        {
            PaintPlaceholder(graphics, "Chart area is too small.");
            return;
        }

        if (rows.Count == 0)
        {
            PaintPlaceholder(graphics, "No supplier data available.");
            return;
        }

        graphics.DrawLine(axisPen, plot.Left, plot.Bottom - 22, plot.Right, plot.Bottom - 22);
        if (Mode == ScoreChartMode.TotalScoreBySupplier)
        {
            DrawTotalScoreBars(graphics, plot);
        }
        else
        {
            DrawDimensionBars(graphics, plot);
        }
    }

    private void DrawTotalScoreBars(Graphics graphics, Rectangle plot)
    {
        if (plot.Width < 80 || plot.Height < 60 || rows.Count == 0)
        {
            PaintPlaceholder(graphics, "Not enough chart space.");
            return;
        }

        var barAreaHeight = Math.Max(30, plot.Height - 44);
        var visibleRows = rows.Take(Math.Max(1, Math.Min(rows.Count, plot.Width / 80))).ToList();
        var slotWidth = Math.Max(70, plot.Width / visibleRows.Count);

        for (var index = 0; index < visibleRows.Count; index++)
        {
            var row = visibleRows[index];
            var value = ClampScore(row.TotalScore);
            var barHeight = (int)(barAreaHeight * value / 100m);
            var x = plot.Left + index * slotWidth + 14;
            var barWidth = Math.Max(10, Math.Min(54, slotWidth - 28));
            var y = plot.Bottom - 24 - barHeight;

            using var brush = new SolidBrush(BarColor(row));
            using var textBrush = new SolidBrush(AppTheme.MainText);
            using var mutedBrush = new SolidBrush(AppTheme.MutedText);
            graphics.FillRectangle(brush, x, y, barWidth, Math.Max(1, barHeight));
            if (barAreaHeight > 50)
            {
                graphics.DrawString($"{value:0}", Font, textBrush, x, Math.Max(plot.Top, y - 18));
            }

            graphics.DrawString(ShortName(row.SupplierName), Font, mutedBrush, x - 4, plot.Bottom - 18);
        }
    }

    private void DrawDimensionBars(Graphics graphics, Rectangle plot)
    {
        if (plot.Width < 160 || plot.Height < 70)
        {
            PaintPlaceholder(graphics, "Not enough chart space.");
            return;
        }

        var visibleRows = rows.Take(4).ToList();
        if (visibleRows.Count == 0)
        {
            PaintPlaceholder(graphics, "No supplier data available.");
            return;
        }

        var barAreaHeight = Math.Max(30, plot.Height - 50);
        var groupWidth = Math.Max(90, plot.Width / visibleRows.Count);
        var colors = new[] { AppTheme.PrimaryDark, AppTheme.Primary, AppTheme.Warning };

        for (var index = 0; index < visibleRows.Count; index++)
        {
            var row = visibleRows[index];
            var values = new[]
            {
                ClampScore(row.CommercialScore),
                ClampScore(row.TechnicalScore),
                ClampScore(row.RegulatoryScore)
            };
            var x = plot.Left + index * groupWidth + 12;

            for (var valueIndex = 0; valueIndex < values.Length; valueIndex++)
            {
                var barWidth = Math.Max(8, Math.Min(22, (groupWidth - 34) / 3));
                var barHeight = (int)(barAreaHeight * values[valueIndex] / 100m);
                var barX = x + valueIndex * (barWidth + 6);
                var barY = plot.Bottom - 24 - barHeight;
                using var brush = new SolidBrush(colors[valueIndex]);
                graphics.FillRectangle(brush, barX, barY, barWidth, Math.Max(1, barHeight));
            }

            using var mutedBrush = new SolidBrush(AppTheme.MutedText);
            graphics.DrawString(ShortName(row.SupplierName), Font, mutedBrush, x - 2, plot.Bottom - 18);
        }

        if (plot.Width >= 360)
        {
            DrawLegend(graphics, plot);
        }
    }

    private static void DrawLegend(Graphics graphics, Rectangle plot)
    {
        if (plot.Right < 280)
        {
            return;
        }

        var labels = new[] { "Commercial", "Technical", "Regulatory" };
        var colors = new[] { AppTheme.PrimaryDark, AppTheme.Primary, AppTheme.Warning };
        var x = Math.Max(plot.Left, plot.Right - 260);
        for (var index = 0; index < labels.Length; index++)
        {
            using var brush = new SolidBrush(colors[index]);
            using var textBrush = new SolidBrush(AppTheme.MutedText);
            graphics.FillRectangle(brush, x + index * 88, plot.Top - 24, 10, 10);
            graphics.DrawString(labels[index], SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont, textBrush, x + 14 + index * 88, plot.Top - 28);
        }
    }

    private void PaintPlaceholder(Graphics graphics, string message)
    {
        graphics.Clear(AppTheme.CardBackground);
        using var brush = new SolidBrush(AppTheme.MutedText);
        var bounds = ClientRectangle;
        bounds.Inflate(-12, -10);
        graphics.DrawString(message, Font, brush, bounds);
    }

    private static decimal ClampScore(decimal? score)
    {
        return Math.Max(0m, Math.Min(100m, score ?? 0m));
    }

    private static Color BarColor(SupplierResultRow row)
    {
        return row.Compare ? AppTheme.PrimaryDark : AppTheme.Primary;
    }

    private static string ShortName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return ShortText(parts.Length == 0 ? name : parts[0], 12);
    }

    private static string ShortText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "-";
        }

        return text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, Math.Max(1, maxLength - 1)), "...");
    }
}
