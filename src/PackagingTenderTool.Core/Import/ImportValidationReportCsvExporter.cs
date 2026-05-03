using System.Globalization;
using System.Text;

namespace PackagingTenderTool.Core.Import;

public static class ImportValidationReportCsvExporter
{
    public static string ToCsv(ImportValidationReport? report)
    {
        if (report is null || report.Issues.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("Row,Column,Severity,IssueType,RawValue,Message,SuggestedAction");
        foreach (var i in report.Issues)
        {
            sb.Append(Escape(i.RowNumber?.ToString(CultureInfo.InvariantCulture)));
            sb.Append(',');
            sb.Append(Escape(i.ColumnName));
            sb.Append(',');
            sb.Append(Escape(i.Severity.ToString()));
            sb.Append(',');
            sb.Append(Escape(i.IssueType.ToString()));
            sb.Append(',');
            sb.Append(Escape(i.RawValue));
            sb.Append(',');
            sb.Append(Escape(i.Message));
            sb.Append(',');
            sb.Append(Escape(i.SuggestedAction));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains('"') || value.Contains(',') || value.Contains('\r') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return value;
    }
}
