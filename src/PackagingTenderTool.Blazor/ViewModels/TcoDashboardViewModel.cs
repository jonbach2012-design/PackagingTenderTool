using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.Blazor.ViewModels;

public sealed class TcoDashboardViewModel
{
    public sealed record CategoryValue(string Name, decimal Value);
    public sealed record StackRow(string Supplier, IReadOnlyList<CategoryValue> Categories, decimal Total);

    public IReadOnlyList<string> CategoryOrder { get; }
    public IReadOnlyList<StackRow> Rows { get; }

    private TcoDashboardViewModel(IReadOnlyList<string> categoryOrder, IReadOnlyList<StackRow> rows)
    {
        CategoryOrder = categoryOrder;
        Rows = rows;
    }

    public static TcoDashboardViewModel Build(IEnumerable<(string Key, TcoResult Result)> items)
    {
        var defs = CostComponentRegistry.Default.GetDashboardComponents();
        var categoryOrder = defs.Select(d => d.DisplayName).ToList();

        var rows = items
            .Select(x =>
            {
                var categories = defs
                    .Select(d => new CategoryValue(d.DisplayName, d.GetValue(x.Result)))
                    .ToList();

                var total = defs.Where(d => d.IncludeInTotal).Sum(d => d.GetValue(x.Result));
                return new StackRow(Supplier: x.Key, Categories: categories, Total: total);
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        return new TcoDashboardViewModel(categoryOrder, rows);
    }
}
