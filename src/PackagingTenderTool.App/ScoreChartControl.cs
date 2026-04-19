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
        rows = supplierRows;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(AppTheme.CardBackground);
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using var titleFont = AppTheme.TitleFont(10F);
        using var axisPen = new Pen(AppTheme.PrimaryLight, 1);

        var bounds = ClientRectangle;
        bounds.Inflate(-12, -10);
        e.Graphics.DrawString(ChartTitle, titleFont, new SolidBrush(AppTheme.MainText), bounds.Location);
        var plot = new Rectangle(bounds.Left, bounds.Top + 34, bounds.Width, bounds.Height - 40);

        if (rows.Count == 0)
        {
            e.Graphics.DrawString("No supplier data available.", Font, new SolidBrush(AppTheme.MutedText), plot.Location);
            return;
        }

        e.Graphics.DrawLine(axisPen, plot.Left, plot.Bottom - 22, plot.Right, plot.Bottom - 22);

        if (Mode == ScoreChartMode.TotalScoreBySupplier)
        {
            DrawTotalScoreBars(e.Graphics, plot);
        }
        else
        {
            DrawDimensionBars(e.Graphics, plot);
        }
    }

    private void DrawTotalScoreBars(Graphics graphics, Rectangle plot)
    {
        var barAreaHeight = Math.Max(30, plot.Height - 44);
        var slotWidth = Math.Max(90, plot.Width / Math.Max(rows.Count, 1));

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var value = ClampScore(row.TotalScore);
            var barHeight = (int)(barAreaHeight * value / 100m);
            var x = plot.Left + index * slotWidth + 14;
            var barWidth = Math.Min(54, slotWidth - 28);
            var y = plot.Bottom - 24 - barHeight;

            using var brush = new SolidBrush(BarColor(row));
            graphics.FillRectangle(brush, x, y, barWidth, barHeight);
            graphics.DrawString($"{value:0}", Font, new SolidBrush(AppTheme.MainText), x, y - 18);
            graphics.DrawString(ShortName(row.SupplierName), Font, new SolidBrush(AppTheme.MutedText), x - 8, plot.Bottom - 18);
        }
    }

    private void DrawDimensionBars(Graphics graphics, Rectangle plot)
    {
        var visibleRows = rows.Take(4).ToList();
        var barAreaHeight = Math.Max(30, plot.Height - 50);
        var groupWidth = Math.Max(130, plot.Width / Math.Max(visibleRows.Count, 1));
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
            var x = plot.Left + index * groupWidth + 18;

            for (var valueIndex = 0; valueIndex < values.Length; valueIndex++)
            {
                var barWidth = 24;
                var barHeight = (int)(barAreaHeight * values[valueIndex] / 100m);
                var barX = x + valueIndex * 30;
                var barY = plot.Bottom - 24 - barHeight;
                using var brush = new SolidBrush(colors[valueIndex]);
                graphics.FillRectangle(brush, barX, barY, barWidth, barHeight);
            }

            graphics.DrawString(ShortName(row.SupplierName), Font, new SolidBrush(AppTheme.MutedText), x - 10, plot.Bottom - 18);
        }

        DrawLegend(graphics, plot);
    }

    private static void DrawLegend(Graphics graphics, Rectangle plot)
    {
        var labels = new[] { "Commercial", "Technical", "Regulatory" };
        var colors = new[] { AppTheme.PrimaryDark, AppTheme.Primary, AppTheme.Warning };
        var x = plot.Right - 260;
        for (var index = 0; index < labels.Length; index++)
        {
            using var brush = new SolidBrush(colors[index]);
            graphics.FillRectangle(brush, x + index * 88, plot.Top - 24, 10, 10);
            graphics.DrawString(labels[index], SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont, new SolidBrush(AppTheme.MutedText), x + 14 + index * 88, plot.Top - 28);
        }
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
        return parts.Length == 0 ? name : parts[0];
    }
}
