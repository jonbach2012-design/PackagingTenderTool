namespace PackagingTenderTool.Core.Import;

/// <summary>Domain classification for an import validation finding.</summary>
public enum ImportValidationIssueType
{
    UnsupportedFileType = 1,
    WorkbookOpenFailed = 2,
    HeaderNotRecognized = 3,
    MissingRequiredColumn = 4,
    InvalidCellValue = 5,
    EmptyRequiredCell = 6,
    NormalizedValue = 7,
    ManualReviewRequired = 8,
    /// <summary>Non-data row skipped (e.g. summary band).</summary>
    RowSkipped = 9
}
