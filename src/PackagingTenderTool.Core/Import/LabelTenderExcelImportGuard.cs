namespace PackagingTenderTool.Core.Import;

/// <summary>
/// First-line validation before reading Excel streams (browser MIME types are unreliable).
/// </summary>
public static class LabelTenderExcelImportGuard
{
    public const string UnsupportedFileTypeMarker = "UNSUPPORTED_FILE_TYPE";

    /// <summary>
    /// Only <c>.xlsx</c> is supported for Labels tender import (Open XML spreadsheet).
    /// </summary>
    public static void EnsureXlsxExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException(UnsupportedFileTypeMarker);

        var ext = Path.GetExtension(fileName);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(UnsupportedFileTypeMarker);
    }
}
