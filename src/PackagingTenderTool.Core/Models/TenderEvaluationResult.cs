using PackagingTenderTool.Core.Analytics;
using PackagingTenderTool.Core.Import;

namespace PackagingTenderTool.Core.Models;

public sealed class TenderEvaluationResult
{
    public Tender Tender { get; set; } = new();

    public List<LineEvaluation> LineEvaluations { get; set; } = [];

    public List<SupplierEvaluation> SupplierEvaluations { get; set; } = [];

    public LabelsImportSummary? ImportSummary { get; set; }

    public List<ImportValidationIssue> ImportIssues { get; set; } = [];

    public List<CleanedLabelLineItem> CleanedLineItems { get; set; } = [];

    public TenderAnalyticsSummary? Analytics { get; set; }

    /// <summary>
    /// Lowest offered price per ItemNo across all suppliers in this tender.
    /// Key = ItemNo (trimmed, case-insensitive). Value = lowest Price or PricePerThousand found.
    /// Empty if no line items have a price. Used as baseline reference in deviation UI.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> BestBidBaseline { get; set; } =
        new Dictionary<string, decimal>();

    /// <summary>
    /// Current contract price per ItemNo, from the current_price import column.
    /// Key = ItemNo (trimmed, case-insensitive). Value = first non-null CurrentContractPrice found for that ItemNo.
    /// Null entries are excluded. Empty if no current_price column was present in the import file.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> CurrentContractPriceBaseline { get; set; } =
        new Dictionary<string, decimal>();
}
