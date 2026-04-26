using System.Reflection;
using PackagingTenderTool.Blazor.Models;

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
        var props = GetDynamicCostProperties();
        var categoryOrder = props.Select(p => p.Name).ToList();

        var rows = items
            .Select(x =>
            {
                var categories = props
                    .Select(p => new CategoryValue(p.Name, p.GetValue(x.Result)))
                    .ToList();

                var total = categories.Sum(c => c.Value);
                return new StackRow(Supplier: x.Key, Categories: categories, Total: total);
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        return new TcoDashboardViewModel(categoryOrder, rows);
    }

    private static IReadOnlyList<(string Name, Func<TcoResult, decimal> GetValue)> GetDynamicCostProperties()
    {
        var resultType = typeof(TcoResult);
        var props = resultType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(decimal))
            .Where(p => !string.Equals(p.Name, nameof(TcoResult.Total), StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(p => (Name: p.Name, GetValue: (Func<TcoResult, decimal>)(r => (decimal)p.GetValue(r)!)))
            .ToList();

        return props;
    }
}

