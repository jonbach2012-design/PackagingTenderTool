using System.ComponentModel;
using System.Globalization;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.App;

public sealed class MainForm : Form
{
    private readonly TextBox filePathTextBox = new();
    private readonly Button browseButton = new();
    private readonly Button evaluateButton = new();
    private readonly DataGridView resultsGrid = new();
    private readonly Label statusLabel = new();
    private readonly LabelsTenderEvaluationService evaluationService = new();

    public MainForm()
    {
        Text = "PackagingTenderTool - Labels v1";
        MinimumSize = new Size(980, 560);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        ConfigureResultsGrid();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var filePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 4
        };
        filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var fileLabel = new Label
        {
            Text = "Labels v1 Excel file",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 8, 0)
        };

        filePathTextBox.Dock = DockStyle.Fill;
        filePathTextBox.ReadOnly = true;

        browseButton.Text = "Browse...";
        browseButton.AutoSize = true;
        browseButton.Margin = new Padding(8, 0, 0, 0);
        browseButton.Click += BrowseButton_Click;

        evaluateButton.Text = "Import and evaluate";
        evaluateButton.AutoSize = true;
        evaluateButton.Margin = new Padding(8, 0, 0, 0);
        evaluateButton.Click += EvaluateButton_Click;

        filePanel.Controls.Add(fileLabel, 0, 0);
        filePanel.Controls.Add(filePathTextBox, 1, 0);
        filePanel.Controls.Add(browseButton, 2, 0);
        filePanel.Controls.Add(evaluateButton, 3, 0);

        resultsGrid.Dock = DockStyle.Fill;
        resultsGrid.Margin = new Padding(0, 12, 0, 12);

        statusLabel.AutoSize = true;
        statusLabel.Text = "Select a Labels v1 Excel file, then import and evaluate.";

        root.Controls.Add(filePanel, 0, 0);
        root.Controls.Add(resultsGrid, 0, 1);
        root.Controls.Add(statusLabel, 0, 2);

        Controls.Add(root);
    }

    private void ConfigureResultsGrid()
    {
        resultsGrid.AllowUserToAddRows = false;
        resultsGrid.AllowUserToDeleteRows = false;
        resultsGrid.AutoGenerateColumns = false;
        resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        resultsGrid.BackgroundColor = SystemColors.Window;
        resultsGrid.BorderStyle = BorderStyle.FixedSingle;
        resultsGrid.DataSource = new BindingList<SupplierResultRow>();
        resultsGrid.ReadOnly = true;
        resultsGrid.RowHeadersVisible = false;
        resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        AddTextColumn(nameof(SupplierResultRow.SupplierName), "Supplier name", 150);
        AddTextColumn(nameof(SupplierResultRow.TotalSpend), "Total spend", 90);
        AddTextColumn(nameof(SupplierResultRow.CommercialScore), "Commercial", 80);
        AddTextColumn(nameof(SupplierResultRow.TechnicalScore), "Technical", 80);
        AddTextColumn(nameof(SupplierResultRow.RegulatoryScore), "Regulatory", 80);
        AddTextColumn(nameof(SupplierResultRow.TotalScore), "Total", 80);
        AddTextColumn(nameof(SupplierResultRow.Classification), "Classification", 100);
        AddTextColumn(nameof(SupplierResultRow.ManualReviewRequired), "Manual review", 90);
        AddTextColumn(nameof(SupplierResultRow.ManualReviewFlagCount), "Flags", 60);
    }

    private void AddTextColumn(string propertyName, string headerText, int minimumWidth)
    {
        resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            MinimumWidth = minimumWidth,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Labels v1 Excel file",
            Filter = "Excel workbooks (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        filePathTextBox.Text = dialog.FileName;
        statusLabel.ForeColor = SystemColors.ControlText;
        statusLabel.Text = "File selected. Import and evaluate when ready.";
    }

    private void EvaluateButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(filePathTextBox.Text))
        {
            SetErrorStatus("Select a Labels v1 Excel file before evaluating.");
            return;
        }

        try
        {
            SetBusyState(true);

            var result = evaluationService.ImportAndEvaluate(
                filePathTextBox.Text,
                Path.GetFileNameWithoutExtension(filePathTextBox.Text),
                LabelsV1DemoConfiguration.CreateTenderSettings());
            var rows = result.SupplierEvaluations
                .Select(evaluation => SupplierResultRow.FromEvaluation(
                    evaluation,
                    result.Tender.Settings.CurrencyCode))
                .ToList();

            resultsGrid.DataSource = new BindingList<SupplierResultRow>(rows);

            var manualReviewSupplierCount = result.SupplierEvaluations.Count(evaluation => evaluation.RequiresManualReview);
            statusLabel.ForeColor = SystemColors.ControlText;
            statusLabel.Text = $"Imported {result.Tender.LabelLineItems.Count} line(s). "
                + $"Evaluated {result.SupplierEvaluations.Count} supplier(s). "
                + $"Manual review suppliers: {manualReviewSupplierCount}.";
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or UnauthorizedAccessException
            or ArgumentException)
        {
            resultsGrid.DataSource = new BindingList<SupplierResultRow>();
            SetErrorStatus($"Import or evaluation failed: {exception.Message}");
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void SetBusyState(bool isBusy)
    {
        browseButton.Enabled = !isBusy;
        evaluateButton.Enabled = !isBusy;
        Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void SetErrorStatus(string message)
    {
        statusLabel.ForeColor = Color.DarkRed;
        statusLabel.Text = message;
    }

    private sealed class SupplierResultRow
    {
        public string SupplierName { get; private init; } = string.Empty;

        public string TotalSpend { get; private init; } = string.Empty;

        public string CommercialScore { get; private init; } = string.Empty;

        public string TechnicalScore { get; private init; } = string.Empty;

        public string RegulatoryScore { get; private init; } = string.Empty;

        public string TotalScore { get; private init; } = string.Empty;

        public string Classification { get; private init; } = string.Empty;

        public string ManualReviewRequired { get; private init; } = string.Empty;

        public int ManualReviewFlagCount { get; private init; }

        public static SupplierResultRow FromEvaluation(
            SupplierEvaluation supplierEvaluation,
            string currencyCode)
        {
            return new SupplierResultRow
            {
                SupplierName = string.IsNullOrWhiteSpace(supplierEvaluation.SupplierName)
                    ? "(missing supplier)"
                    : supplierEvaluation.SupplierName,
                TotalSpend = $"{supplierEvaluation.TotalSpend.ToString("0.00", CultureInfo.InvariantCulture)} {currencyCode}",
                CommercialScore = FormatScore(supplierEvaluation.ScoreBreakdown.Commercial),
                TechnicalScore = FormatScore(supplierEvaluation.ScoreBreakdown.Technical),
                RegulatoryScore = FormatScore(supplierEvaluation.ScoreBreakdown.Regulatory),
                TotalScore = FormatScore(supplierEvaluation.ScoreBreakdown.Total),
                Classification = supplierEvaluation.Classification?.ToString() ?? "Unclassified",
                ManualReviewRequired = supplierEvaluation.RequiresManualReview ? "Yes" : "No",
                ManualReviewFlagCount = supplierEvaluation.ManualReviewFlags.Count
            };
        }

        private static string FormatScore(decimal? score)
        {
            return score.HasValue
                ? score.Value.ToString("0.##", CultureInfo.InvariantCulture)
                : "n/a";
        }
    }
}
