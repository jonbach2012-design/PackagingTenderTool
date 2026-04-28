using PackagingTenderTool.Blazor.Models;

namespace PackagingTenderTool.Blazor.Services;

public interface IExportService
{
    byte[] ExportAudit(IReadOnlyList<AuditGridRow> rows, IScenarioStateService scenario, DateTimeOffset timestamp);
}

