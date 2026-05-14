namespace PackagingTenderTool.Core.Models;

public sealed record CostComponentDefinition(
    string Key,
    string DisplayName,
    int Order,
    string Group,
    bool IncludeInTotal,
    bool VisibleInDashboard,
    bool VisibleInExport,
    Func<TcoResult, decimal> GetValue);
