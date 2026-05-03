using PackagingTenderTool.Core.Import;

namespace PackagingTenderTool.Core.Tests;

/// <summary>
/// Resolved bucket names for import outcomes (tests + manual QA matrix).
/// </summary>
public enum LabelTenderImportResolvedBucket
{
    UnsupportedFileType,
    WorkbookOpenFailed,
    HeaderNotRecognized,
    MissingRequiredColumn,
    RowValidationError,
    Success
}

/// <summary>
/// Separates user-facing error buckets: extension, workbook open, header identity, required columns, success.
/// </summary>
public sealed class LabelTenderImportBucketTests
{
    [Fact]
    public void Bucket_UnsupportedFileType_guard_and_format_match_spec()
    {
        Assert.Throws<InvalidOperationException>(() =>
            LabelTenderExcelImportGuard.EnsureXlsxExtension("report.pdf"));

        var pathEx = Assert.Throws<InvalidOperationException>(() =>
            new LabelsExcelImportService().ImportTenderWithReport(@"C:\fake\tender.pdf"));

        Assert.Equal(LabelTenderExcelImportGuard.UnsupportedFileTypeMarker, pathEx.Message);

        var formatted = LabelTenderImportFailureMessage.Format(pathEx);
        Assert.Equal(LabelTenderImportFailureMessage.UnsupportedFileTypeUserMessage, formatted);
    }

    [Fact]
    public void Bucket_WorkbookOpenFailed_corrupt_xlsx_stream_maps_workbook_read_message()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new LabelsExcelImportService().ImportTenderWithReport(ms));

        Assert.Equal(LabelsExcelImportService.WorkbookOpenFailedMarker, ex.Message);

        var msg = LabelTenderImportFailureMessage.Format(ex);
        Assert.Equal(LabelTenderImportFailureMessage.WorkbookCouldNotReadUserMessage, msg);
    }

    [Fact]
    public void Bucket_WorkbookOpenFailed_corrupt_xlsx_file_on_disk_maps_workbook_read_message()
    {
        var path = Path.Combine(Path.GetTempPath(), $"corrupt-{Guid.NewGuid():N}.xlsx");
        try
        {
            File.WriteAllBytes(path, new byte[] { 0xDE, 0xAD });

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new LabelsExcelImportService().ImportTenderWithReport(path));

            Assert.Equal(LabelsExcelImportService.WorkbookOpenFailedMarker, ex.Message);

            var msg = LabelTenderImportFailureMessage.Format(ex);
            Assert.Equal(LabelTenderImportFailureMessage.WorkbookCouldNotReadUserMessage, msg);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Bucket_HeaderNotRecognized_fixture_resolves_to_distinct_message()
    {
        var path = ResolveImportVerificationFixture("bad-unrecognizable-labels-header.xlsx");
        var importer = new LabelsExcelImportService();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var stream = File.OpenRead(path);
            _ = importer.ImportTender(stream, "Fixture");
        });

        Assert.Equal(LabelsExcelImportService.HeaderNotRecognizedMarker, ex.Message);

        var msg = LabelTenderImportFailureMessage.Format(ex);
        Assert.Contains("workbook opened", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("header could not be recognized", msg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("could not be read", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bucket_MissingRequiredColumn_fixture_resolves_to_supplier_name_message()
    {
        var path = ResolveImportVerificationFixture("bad-missing-required-supplier.xlsx");
        var importer = new LabelsExcelImportService();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var stream = File.OpenRead(path);
            _ = importer.ImportTender(stream, "Fixture");
        });

        Assert.StartsWith(LabelsExcelImportService.MissingRequiredColumnMarker + ":", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Supplier name", ex.Message, StringComparison.Ordinal);

        var msg = LabelTenderImportFailureMessage.Format(ex);
        Assert.Equal("Import failed: Missing required column 'Supplier name'.", msg);
    }

    [Fact]
    public void Bucket_Success_min_valid_labels_fixture_imports_without_file_or_header_bucket_errors()
    {
        var path = ResolveImportVerificationFixture("min-valid-labels.xlsx");
        var importer = new LabelsExcelImportService();

        LabelsTenderImportResult result;
        using (var stream = File.OpenRead(path))
        {
            result = importer.ImportTenderWithReport(stream, "Fixture success");
        }

        Assert.NotEmpty(result.Tender.LabelLineItems);
    }

    private static string ResolveImportVerificationFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var path = Path.Combine(dir.FullName, "testdata", "import-verification", fileName);
            if (File.Exists(path))
                return path;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate testdata/import-verification/{fileName} (search upward from {AppContext.BaseDirectory}).");
    }
}
