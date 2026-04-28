using PackagingTenderTool.Blazor.Models;

namespace PackagingTenderTool.Blazor.Services;

public interface IMockDataService
{
    IReadOnlyList<TenderLine> GenerateTenderLines(int count = 120);
    IReadOnlyList<SupplierSummary> GetSupplierSummaries(int lineCount = 140);
    IReadOnlyList<LineTcoEntry> GetLineTcoEntries(int lineCount = 140);
    IReadOnlyList<AuditGridRow> GetAuditRows(int lineCount = 140);
    IReadOnlyList<NegotiationBidRow> GetNegotiationRows(string lineItem, string site, int lineCount = 140);
}

