using System.Globalization;
using ClosedXML.Excel;
using PackagingTenderTool.Blazor.Models;

namespace PackagingTenderTool.Blazor.Services;

public sealed class ExportService : IExportService
{
    public byte[] ExportAudit(
        IReadOnlyList<AuditGridRow> rows,
        IScenarioStateService scenario,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(scenario);

        using var wb = new XLWorkbook();

        AddDataSheet(wb, rows);
        AddAuditTrailSheet(wb, scenario, timestamp);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void AddDataSheet(XLWorkbook wb, IReadOnlyList<AuditGridRow> rows)
    {
        var ws = wb.Worksheets.Add("Data");

        var headers = new[]
        {
            "LineItemID","Site","Category","Supplier","MaterialClass","BasePrice","ActualTCO","WeightedDecisionScore","DecisionScoreIndex","DataQuality","Warning"
        };

        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var row = i + 2;

            ws.Cell(row, 1).Value = r.LineItem;
            ws.Cell(row, 2).Value = r.Site;
            ws.Cell(row, 3).Value = r.Category;
            ws.Cell(row, 4).Value = r.Supplier;

            var mc = ws.Cell(row, 5);
            mc.Value = r.MaterialClass;
            ApplyMaterialClassColor(mc, r.MaterialClass);

            ws.Cell(row, 6).Value = (double)r.BasePrice;
            ws.Cell(row, 7).Value = (double)r.ActualTco;
            ws.Cell(row, 8).Value = (double)r.WeightedDecisionScore;
            ws.Cell(row, 9).Value = (double)r.DecisionScoreIndex;
            ws.Cell(row, 10).Value = (double)r.DataQualityScore;

            var warn = ws.Cell(row, 11);
            if (r.DataQualityScore < 75m)
            {
                warn.Value = "Low data quality";
                warn.Style.Font.FontColor = XLColor.FromHtml("#D9534F");
                warn.Style.Font.Bold = true;
            }
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        ws.Column(6).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(7).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(8).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(9).Style.NumberFormat.Format = "0.0";
        ws.Column(10).Style.NumberFormat.Format = "0";
    }

    private static void AddAuditTrailSheet(XLWorkbook wb, IScenarioStateService scenario, DateTimeOffset timestamp)
    {
        var ws = wb.Worksheets.Add("Audit Trail");

        ws.Cell(1, 1).Value = "Export timestamp";
        ws.Cell(1, 2).Value = timestamp.ToString("u", CultureInfo.InvariantCulture);

        ws.Cell(3, 1).Value = "EPR Multipliers (Baseline)";
        ws.Cell(4, 1).Value = "Class";
        ws.Cell(4, 2).Value = "Multiplier";
        ws.Cell(4, 1).Style.Font.Bold = true;
        ws.Cell(4, 2).Style.Font.Bold = true;

        var row = 5;
        foreach (var kvp in scenario.BaselineWeights.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            ws.Cell(row, 1).Value = kvp.Key;
            ws.Cell(row, 2).Value = (double)kvp.Value;
            row++;
        }

        row += 1;
        ws.Cell(row, 1).Value = "EPR Multipliers (Active)";
        row++;
        ws.Cell(row, 1).Value = "Class";
        ws.Cell(row, 2).Value = "Multiplier";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.Bold = true;
        row++;

        foreach (var kvp in scenario.ActiveWeights.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            ws.Cell(row, 1).Value = kvp.Key;
            ws.Cell(row, 2).Value = (double)kvp.Value;
            row++;
        }

        row += 1;
        ws.Cell(row, 1).Value = "Strategic Weights";
        row++;
        ws.Cell(row, 1).Value = "Pillar";
        ws.Cell(row, 2).Value = "Weight";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.Bold = true;
        row++;

        var w = scenario.StrategicWeights;
        ws.Cell(row++, 1).Value = "Commercial"; ws.Cell(row - 1, 2).Value = (double)w.Commercial;
        ws.Cell(row++, 1).Value = "Technical"; ws.Cell(row - 1, 2).Value = (double)w.Technical;
        ws.Cell(row++, 1).Value = "Switching"; ws.Cell(row - 1, 2).Value = (double)w.Switching;
        ws.Cell(row++, 1).Value = "Regulatory"; ws.Cell(row - 1, 2).Value = (double)w.Regulatory;

        ws.Columns().AdjustToContents();
    }

    private static void ApplyMaterialClassColor(IXLCell cell, string materialClass)
    {
        var (bg, fg) = materialClass switch
        {
            "A" => ("#91A363", "#FFFFFF"),
            "B" => ("#5C6B76", "#FFFFFF"),
            "C" => ("#FFB300", "#2D3436"),
            "D" => ("#D9534F", "#FFFFFF"),
            _ => ("#BDBDBD", "#2D3436")
        };

        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(bg);
        cell.Style.Font.FontColor = XLColor.FromHtml(fg);
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
}

