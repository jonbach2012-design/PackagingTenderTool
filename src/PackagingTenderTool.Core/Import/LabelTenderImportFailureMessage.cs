namespace PackagingTenderTool.Core.Import;

/// <summary>
/// Maps Label tender Excel import exceptions to user-facing strings (sidebar / snackbar).
/// Keep buckets distinct: file type, workbook open, header recognition, missing columns — do not collapse into one generic message.
/// </summary>
public static class LabelTenderImportFailureMessage
{
    public const string UnsupportedFileTypeUserMessage =
        "Import failed: Unsupported file type. Please upload an .xlsx file.";

    public const string WorkbookCouldNotReadUserMessage =
        "Import failed: Excel workbook could not be read.";

    private const string HeaderNotRecognizedUserMessage =
        "Import failed: Excel workbook opened, but the Labels tender header could not be recognized.";

    private const string ImportDidNotCompleteUserMessage =
        "Import failed: The import could not be completed.";

    public static string Format(Exception ex)
    {
        foreach (var cur in SelfAndInner(ex))
        {
            if (cur is InvalidOperationException inv)
            {
                var m = inv.Message.Trim();
                if (m == LabelTenderExcelImportGuard.UnsupportedFileTypeMarker)
                    return UnsupportedFileTypeUserMessage;
                if (m == LabelsExcelImportService.WorkbookOpenFailedMarker)
                    return WorkbookCouldNotReadUserMessage;
                if (m == LabelsExcelImportService.NoWorksheetMarker)
                    return "Import failed: The Excel file does not contain any worksheet.";
                if (m == LabelsExcelImportService.HeaderNotRecognizedMarker)
                    return HeaderNotRecognizedUserMessage;
                if (m.StartsWith(LabelsExcelImportService.MissingRequiredColumnMarker + ":", StringComparison.Ordinal))
                {
                    var payload = m[(LabelsExcelImportService.MissingRequiredColumnMarker.Length + 1)..];
                    var cols = payload.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var inner = string.Join("', '", cols);
                    return $"Import failed: Missing required column '{inner}'.";
                }

                if (m.Contains("does not contain a worksheet", StringComparison.OrdinalIgnoreCase))
                    return "Import failed: The Excel file does not contain any worksheet.";
                if (m.Contains("recognizable Labels tender header", StringComparison.OrdinalIgnoreCase))
                    return HeaderNotRecognizedUserMessage;
                if (m.Contains("Missing required column", StringComparison.OrdinalIgnoreCase))
                    return $"Import failed: {m}";
            }
        }

        var root = ex.InnerException ?? ex;

        if (root is InvalidOperationException inv2)
        {
            var m = inv2.Message.Trim();
            return $"Import failed: {m}";
        }

        var msg = root.Message;
        if (msg.Contains("password", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("encrypted", StringComparison.OrdinalIgnoreCase))
            return "Import failed: The workbook appears to be encrypted or password-protected. Save an unprotected .xlsx and try again.";

        // Package / ZIP failures after extension says .xlsx (e.g. corrupt rename): treat as workbook read, not header/template.
        if (ex is ArgumentException or FormatException
            || msg.Contains("zip", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Central Directory", StringComparison.OrdinalIgnoreCase))
            return WorkbookCouldNotReadUserMessage;

        return ImportDidNotCompleteUserMessage;
    }

    private static IEnumerable<Exception> SelfAndInner(Exception ex)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
            yield return e;
    }
}
