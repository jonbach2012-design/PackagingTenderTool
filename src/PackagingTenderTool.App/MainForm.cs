using System.ComponentModel;
using System.Globalization;
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.App;

public sealed class MainForm : Form
{
    private static readonly Color HeaderGreen = Color.FromArgb(16, 64, 46);
    private static readonly Color AccentGreen = Color.FromArgb(39, 126, 82);
    private static readonly Color AccentOrange = Color.FromArgb(213, 126, 33);
    private static readonly Color AccentRed = Color.FromArgb(174, 53, 53);
    private static readonly Color PageBackground = Color.FromArgb(244, 246, 244);
    private static readonly Color CardBackground = Color.White;
    private static readonly Color MutedText = Color.FromArgb(99, 108, 101);

    private readonly ComboBox tenderTypeComboBox = new();
    private readonly TextBox tenderNameTextBox = new();
    private readonly TextBox currencyTextBox = new();
    private readonly TextBox filePathTextBox = new();
    private readonly NumericUpDown recommendedThresholdInput = new();
    private readonly NumericUpDown conditionalThresholdInput = new();
    private readonly Button browseButton = new();
    private readonly Button evaluateButton = new();
    private readonly DataGridView resultsGrid = new();
    private readonly Label statusLabel = new();
    private readonly Label headerContextLabel = new();
    private readonly Dictionary<string, Label> kpis = [];

    private readonly Label detailSupplier = DetailValueLabel();
    private readonly Label detailClassification = DetailValueLabel();
    private readonly Label detailSpend = DetailValueLabel();
    private readonly Label detailCommercial = DetailValueLabel();
    private readonly Label detailTechnical = DetailValueLabel();
    private readonly Label detailRegulatory = DetailValueLabel();
    private readonly Label detailTotal = DetailValueLabel();
    private readonly Label detailFlags = DetailValueLabel();
    private readonly TextBox detailNotes = new();

    public MainForm()
    {
        Text = "Tender Evaluation Dashboard";
        MinimumSize = new Size(1280, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = PageBackground;
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        ClearDashboard();
        UpdateTenderTypeContext();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildWorkspace(), 0, 1);
        root.Controls.Add(BuildStatusBar(), 0, 2);
        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = HeaderGreen, Padding = new Padding(24, 14, 24, 14) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Tender Evaluation Dashboard\r\nImport, score, classify, and review supplier performance.",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        headerContextLabel.Dock = DockStyle.Fill;
        headerContextLabel.ForeColor = Color.FromArgb(221, 236, 228);
        headerContextLabel.TextAlign = ContentAlignment.MiddleRight;

        header.Controls.Add(title, 0, 0);
        header.Controls.Add(headerContextLabel, 1, 0);
        return header;
    }

    private Control BuildWorkspace()
    {
        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = PageBackground, Padding = new Padding(16) };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        workspace.Controls.Add(BuildControlPanel(), 0, 0);
        workspace.Controls.Add(BuildMainDashboardArea(), 1, 0);
        workspace.Controls.Add(BuildDetailsPanel(), 2, 0);
        return workspace;
    }

    private Control BuildControlPanel()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = PageBackground, Padding = new Padding(0, 0, 12, 0) };
        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };

        stack.Controls.Add(BuildTenderSetupSection());
        stack.Controls.Add(BuildEvaluationSettingsSection());
        stack.Controls.Add(BuildThresholdSection());
        stack.Controls.Add(BuildOptionsSection());
        stack.Controls.Add(BuildActionSection());
        scroll.Controls.Add(stack);
        return scroll;
    }

    private Control BuildTenderSetupSection()
    {
        var section = Section("Tender setup");
        tenderTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        tenderTypeComboBox.Items.AddRange(["Labels", "Trays", "Flexibles"]);
        tenderTypeComboBox.SelectedIndex = 0;
        tenderTypeComboBox.SelectedIndexChanged += (_, _) => UpdateTenderTypeContext();
        tenderNameTextBox.Text = "Labels Tender v1";
        currencyTextBox.Text = "EUR";
        currencyTextBox.MaxLength = 3;
        filePathTextBox.ReadOnly = true;
        browseButton.Text = "Browse...";
        browseButton.Click += BrowseButton_Click;

        AddField(section, "Tender type", tenderTypeComboBox);
        AddField(section, "Tender name", tenderNameTextBox);
        AddField(section, "Currency", currencyTextBox);
        AddField(section, "Excel file", filePathTextBox);
        AddWide(section, browseButton);
        return section;
    }

    private Control BuildEvaluationSettingsSection()
    {
        var section = Section("Evaluation settings");
        AddField(section, "Commercial weight", PercentBox(30));
        AddField(section, "Technical weight", PercentBox(30));
        AddField(section, "Regulatory weight", PercentBox(40));
        AddNote(section, "Labels v1 currently uses the established 30 / 30 / 40 score weighting.");
        return section;
    }

    private Control BuildThresholdSection()
    {
        var section = Section("Thresholds");
        SetupPercentBox(recommendedThresholdInput, SupplierClassificationService.DefaultRecommendedThreshold);
        SetupPercentBox(conditionalThresholdInput, SupplierClassificationService.DefaultConditionalThreshold);
        AddField(section, "Recommended", recommendedThresholdInput);
        AddField(section, "Conditional", conditionalThresholdInput);
        AddNote(section, "Provisional thresholds are used for supplier classification.");
        return section;
    }

    private Control BuildOptionsSection()
    {
        var section = Section("Options");
        AddWide(section, new CheckBox { Text = "Missing data = Manual Review", Checked = true, AutoSize = true });
        AddWide(section, new CheckBox { Text = "Normalize input values", Checked = true, AutoSize = true });
        AddWide(section, new CheckBox { Text = "Strict mode", AutoSize = true });
        AddNote(section, "Strict mode is reserved for future validation rules.");
        return section;
    }

    private Control BuildActionSection()
    {
        var section = Section("Run evaluation");
        evaluateButton.Text = "Import and evaluate";
        evaluateButton.Height = 42;
        evaluateButton.BackColor = AccentGreen;
        evaluateButton.ForeColor = Color.White;
        evaluateButton.FlatStyle = FlatStyle.Flat;
        evaluateButton.FlatAppearance.BorderSize = 0;
        evaluateButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        evaluateButton.Click += EvaluateButton_Click;
        AddWide(section, evaluateButton);
        return section;
    }

    private Control BuildMainDashboardArea()
    {
        var main = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = PageBackground, Padding = new Padding(0, 0, 16, 0) };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.Controls.Add(BuildKpiPanel(), 0, 0);
        main.Controls.Add(BuildResultsGrid(), 0, 1);
        return main;
    }

    private Control BuildKpiPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, BackColor = PageBackground, Margin = new Padding(0, 0, 0, 12) };
        for (var i = 0; i < 6; i++) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6));
        AddKpi(panel, 0, "Suppliers", "Suppliers");
        AddKpi(panel, 1, "ImportedLines", "Imported lines");
        AddKpi(panel, 2, "Recommended", "Recommended");
        AddKpi(panel, 3, "Conditional", "Conditional");
        AddKpi(panel, 4, "ManualReview", "Manual Review");
        AddKpi(panel, 5, "BestScore", "Best total score");
        return panel;
    }

    private Control BuildResultsGrid()
    {
        var card = Card();
        card.Padding = new Padding(12);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(PanelTitle("Supplier results"), 0, 0);
        ConfigureResultsGrid();
        layout.Controls.Add(resultsGrid, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private void ConfigureResultsGrid()
    {
        resultsGrid.AllowUserToAddRows = false;
        resultsGrid.AllowUserToDeleteRows = false;
        resultsGrid.AllowUserToResizeRows = false;
        resultsGrid.AutoGenerateColumns = false;
        resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        resultsGrid.BackgroundColor = CardBackground;
        resultsGrid.BorderStyle = BorderStyle.None;
        resultsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 241, 236);
        resultsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        resultsGrid.ColumnHeadersHeight = 34;
        resultsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(215, 233, 221);
        resultsGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(37, 43, 39);
        resultsGrid.Dock = DockStyle.Fill;
        resultsGrid.EnableHeadersVisualStyles = false;
        resultsGrid.MultiSelect = false;
        resultsGrid.ReadOnly = true;
        resultsGrid.RowHeadersVisible = false;
        resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        resultsGrid.DataSource = new BindingList<SupplierResultRow>();
        resultsGrid.SelectionChanged += (_, _) => UpdateDetailsPanel(SelectedRow());
        resultsGrid.CellFormatting += ResultsGrid_CellFormatting;

        AddColumn(nameof(SupplierResultRow.SupplierName), "Supplier name", 150);
        AddColumn(nameof(SupplierResultRow.TotalSpend), "Total spend", 90);
        AddColumn(nameof(SupplierResultRow.CommercialScore), "Commercial", 80);
        AddColumn(nameof(SupplierResultRow.TechnicalScore), "Technical", 80);
        AddColumn(nameof(SupplierResultRow.RegulatoryScore), "Regulatory", 80);
        AddColumn(nameof(SupplierResultRow.TotalScore), "Total", 80);
        AddColumn(nameof(SupplierResultRow.Classification), "Classification", 105);
        AddColumn(nameof(SupplierResultRow.ManualReviewRequired), "Manual review", 90);
        AddColumn(nameof(SupplierResultRow.ManualReviewFlagCount), "Flags", 60);
    }

    private Control BuildDetailsPanel()
    {
        var card = Card();
        card.Padding = new Padding(16);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(PanelTitle("Supplier details"), 0, 0);
        layout.Controls.Add(BuildDetailFields(), 0, 1);

        detailNotes.Dock = DockStyle.Fill;
        detailNotes.Multiline = true;
        detailNotes.ReadOnly = true;
        detailNotes.BackColor = Color.FromArgb(249, 250, 249);
        detailNotes.BorderStyle = BorderStyle.FixedSingle;
        detailNotes.Margin = new Padding(0, 14, 0, 0);
        layout.Controls.Add(detailNotes, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildDetailFields()
    {
        var fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        AddDetail(fields, "Supplier", detailSupplier);
        AddDetail(fields, "Classification", detailClassification);
        AddDetail(fields, "Spend", detailSpend);
        AddDetail(fields, "Commercial", detailCommercial);
        AddDetail(fields, "Technical", detailTechnical);
        AddDetail(fields, "Regulatory", detailRegulatory);
        AddDetail(fields, "Total", detailTotal);
        AddDetail(fields, "Flag count", detailFlags);
        return fields;
    }

    private Control BuildStatusBar()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(232, 237, 232), Padding = new Padding(16, 6, 16, 6) };
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.ForeColor = MutedText;
        statusLabel.Text = "Select a Labels v1 Excel file, then import and evaluate.";
        panel.Controls.Add(statusLabel);
        return panel;
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
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        filePathTextBox.Text = dialog.FileName;
        tenderNameTextBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
        SetInfo("File selected. Import and evaluate when ready.");
        UpdateTenderTypeContext();
    }

    private void EvaluateButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(filePathTextBox.Text))
        {
            SetError("Select a Labels v1 Excel file before evaluating.");
            return;
        }
        if (!File.Exists(filePathTextBox.Text))
        {
            SetError("The selected Excel file no longer exists.");
            return;
        }

        try
        {
            SetBusy(true);
            var result = CreateEvaluationService().ImportAndEvaluate(
                filePathTextBox.Text,
                TenderName(),
                CreateTenderSettingsFromInputs());
            UpdateDashboardFromResult(result);
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or UnauthorizedAccessException
            or ArgumentException)
        {
            ClearDashboard();
            SetError($"Import or evaluation failed: {exception.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private LabelsTenderEvaluationService CreateEvaluationService()
    {
        return new LabelsTenderEvaluationService(
            new LabelsExcelImportService(),
            new LineEvaluationService(),
            new SupplierAggregationService(),
            new SupplierClassificationService(
                recommendedThresholdInput.Value,
                conditionalThresholdInput.Value));
    }

    private TenderSettings CreateTenderSettingsFromInputs()
    {
        var settings = LabelsV1DemoConfiguration.CreateTenderSettings();
        settings.CurrencyCode = string.IsNullOrWhiteSpace(currencyTextBox.Text)
            ? "EUR"
            : currencyTextBox.Text.Trim().ToUpperInvariant();
        return settings;
    }

    private void UpdateDashboardFromResult(TenderEvaluationResult result)
    {
        var rows = result.SupplierEvaluations
            .Select(evaluation => SupplierResultRow.FromEvaluation(evaluation, result.Tender.Settings.CurrencyCode))
            .ToList();

        resultsGrid.DataSource = new BindingList<SupplierResultRow>(rows);
        UpdateKpis(result);
        SelectFirstRow();

        var manualReviewCount = result.SupplierEvaluations.Count(evaluation => evaluation.RequiresManualReview);
        SetInfo($"Imported {result.Tender.LabelLineItems.Count} line(s). "
            + $"Evaluated {result.SupplierEvaluations.Count} supplier(s). "
            + $"Manual review suppliers: {manualReviewCount}.");
        UpdateTenderTypeContext();
    }

    private void UpdateKpis(TenderEvaluationResult result)
    {
        SetKpi("Suppliers", result.SupplierEvaluations.Count.ToString(CultureInfo.InvariantCulture));
        SetKpi("ImportedLines", result.Tender.LabelLineItems.Count.ToString(CultureInfo.InvariantCulture));
        SetKpi("Recommended", CountClassification(result, SupplierClassification.Recommended));
        SetKpi("Conditional", CountClassification(result, SupplierClassification.Conditional));
        SetKpi("ManualReview", CountClassification(result, SupplierClassification.ManualReview));
        SetKpi("BestScore", FormatScore(result.SupplierEvaluations
            .Select(evaluation => evaluation.ScoreBreakdown.Total)
            .Where(score => score.HasValue)
            .DefaultIfEmpty()
            .Max()));
    }

    private void UpdateDetailsPanel(SupplierResultRow? row)
    {
        if (row is null)
        {
            detailSupplier.Text = "-";
            detailClassification.Text = "-";
            detailSpend.Text = "-";
            detailCommercial.Text = "-";
            detailTechnical.Text = "-";
            detailRegulatory.Text = "-";
            detailTotal.Text = "-";
            detailFlags.Text = "-";
            detailNotes.Text = "Select a supplier row to review score details, classification rationale, and manual review notes.";
            ApplyClassificationStyle(null);
            return;
        }

        var evaluation = row.Evaluation;
        detailSupplier.Text = row.SupplierName;
        detailClassification.Text = row.Classification;
        detailSpend.Text = row.TotalSpend;
        detailCommercial.Text = row.CommercialScore;
        detailTechnical.Text = row.TechnicalScore;
        detailRegulatory.Text = row.RegulatoryScore;
        detailTotal.Text = row.TotalScore;
        detailFlags.Text = row.ManualReviewFlagCount.ToString(CultureInfo.InvariantCulture);
        detailNotes.Text = SupplierNotes(evaluation);
        ApplyClassificationStyle(evaluation.Classification);
    }

    private void ClearDashboard()
    {
        resultsGrid.DataSource = new BindingList<SupplierResultRow>();
        SetKpi("Suppliers", "0");
        SetKpi("ImportedLines", "0");
        SetKpi("Recommended", "0");
        SetKpi("Conditional", "0");
        SetKpi("ManualReview", "0");
        SetKpi("BestScore", "n/a");
        UpdateDetailsPanel(null);
    }

    private static string SupplierNotes(SupplierEvaluation evaluation)
    {
        var notes = new List<string>();
        if (!string.IsNullOrWhiteSpace(evaluation.ClassificationReason))
        {
            notes.Add(evaluation.ClassificationReason);
        }
        if (evaluation.ManualReviewFlags.Count == 0)
        {
            notes.Add("No manual review flags are present for this supplier.");
        }
        else
        {
            notes.Add("Manual review flags:");
            notes.AddRange(evaluation.ManualReviewFlags
                .Take(6)
                .Select(flag => $"- {flag.FieldName ?? "General"}: {flag.Reason}"));
            if (evaluation.ManualReviewFlags.Count > 6)
            {
                notes.Add($"- {evaluation.ManualReviewFlags.Count - 6} additional flag(s).");
            }
        }
        notes.Add("No hard exclusion rules are applied in this dashboard version.");
        return string.Join(Environment.NewLine, notes);
    }

    private void UpdateTenderTypeContext()
    {
        var tenderType = tenderTypeComboBox.SelectedItem?.ToString() ?? "Labels";
        var fileContext = string.IsNullOrWhiteSpace(filePathTextBox.Text)
            ? "No file selected"
            : Path.GetFileName(filePathTextBox.Text);
        var engineContext = tenderType == "Labels"
            ? "Labels v1 engine active"
            : "Future tender type placeholder; Labels v1 engine active";
        headerContextLabel.Text = $"{tenderType} | {engineContext}{Environment.NewLine}{fileContext}";
    }

    private void SelectFirstRow()
    {
        if (resultsGrid.Rows.Count == 0)
        {
            UpdateDetailsPanel(null);
            return;
        }
        resultsGrid.ClearSelection();
        resultsGrid.Rows[0].Selected = true;
        resultsGrid.CurrentCell = resultsGrid.Rows[0].Cells[0];
        UpdateDetailsPanel(SelectedRow());
    }

    private SupplierResultRow? SelectedRow()
    {
        if (resultsGrid.CurrentRow?.DataBoundItem is SupplierResultRow currentRow)
        {
            return currentRow;
        }
        return resultsGrid.SelectedRows.Count > 0
            ? resultsGrid.SelectedRows[0].DataBoundItem as SupplierResultRow
            : null;
    }

    private void ResultsGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (resultsGrid.Columns[e.ColumnIndex].DataPropertyName != nameof(SupplierResultRow.Classification)) return;

        var row = resultsGrid.Rows[e.RowIndex].DataBoundItem as SupplierResultRow;
        e.CellStyle.ForeColor = ClassificationColor(row?.Evaluation.Classification);
        e.CellStyle.Font = new Font(resultsGrid.Font, FontStyle.Bold);
    }

    private void ApplyClassificationStyle(SupplierClassification? classification)
    {
        detailClassification.ForeColor = ClassificationColor(classification);
        detailClassification.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
    }

    private static Color ClassificationColor(SupplierClassification? classification)
    {
        return classification switch
        {
            SupplierClassification.Recommended => AccentGreen,
            SupplierClassification.Conditional => AccentOrange,
            SupplierClassification.NotRecommended => AccentRed,
            SupplierClassification.ManualReview => AccentOrange,
            _ => MutedText
        };
    }

    private void SetBusy(bool isBusy)
    {
        browseButton.Enabled = !isBusy;
        evaluateButton.Enabled = !isBusy;
        Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void SetInfo(string message)
    {
        statusLabel.ForeColor = MutedText;
        statusLabel.Text = message;
    }

    private void SetError(string message)
    {
        statusLabel.ForeColor = AccentRed;
        statusLabel.Text = message;
    }

    private string TenderName()
    {
        return string.IsNullOrWhiteSpace(tenderNameTextBox.Text)
            ? Path.GetFileNameWithoutExtension(filePathTextBox.Text)
            : tenderNameTextBox.Text.Trim();
    }

    private static string CountClassification(TenderEvaluationResult result, SupplierClassification classification)
    {
        return result.SupplierEvaluations
            .Count(evaluation => evaluation.Classification == classification)
            .ToString(CultureInfo.InvariantCulture);
    }

    private void SetKpi(string key, string value)
    {
        if (kpis.TryGetValue(key, out var label))
        {
            label.Text = value;
        }
    }

    private static Panel Card()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBackground,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static TableLayoutPanel Section(string title)
    {
        var section = new TableLayoutPanel
        {
            Width = 300,
            AutoSize = true,
            BackColor = CardBackground,
            ColumnCount = 2,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 0, 12)
        };
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            ForeColor = HeaderGreen,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 10)
        };
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.Controls.Add(titleLabel, 0, 0);
        section.SetColumnSpan(titleLabel, 2);
        return section;
    }

    private static void AddField(TableLayoutPanel section, string labelText, Control control)
    {
        var row = section.RowCount++;
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            ForeColor = Color.FromArgb(37, 43, 39),
            Margin = new Padding(0, 5, 8, 8),
            Anchor = AnchorStyles.Left
        }, 0, row);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 0, 0, 8);
        section.Controls.Add(control, 1, row);
    }

    private static void AddWide(TableLayoutPanel section, Control control)
    {
        var row = section.RowCount++;
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 4, 0, 8);
        section.Controls.Add(control, 0, row);
        section.SetColumnSpan(control, 2);
    }

    private static void AddNote(TableLayoutPanel section, string text)
    {
        var row = section.RowCount++;
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var note = new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(260, 0),
            ForeColor = MutedText,
            Margin = new Padding(0, 4, 0, 0)
        };
        section.Controls.Add(note, 0, row);
        section.SetColumnSpan(note, 2);
    }

    private static Label PanelTitle(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = HeaderGreen,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 10)
        };
    }

    private void AddKpi(TableLayoutPanel parent, int column, string key, string title)
    {
        var card = Card();
        card.Margin = new Padding(column == 0 ? 0 : 6, 0, column == 5 ? 0 : 6, 0);
        card.Padding = new Padding(12);
        var value = new Label
        {
            Text = "0",
            Dock = DockStyle.Fill,
            ForeColor = HeaderGreen,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
        };
        var caption = new Label
        {
            Text = title,
            Dock = DockStyle.Bottom,
            Height = 24,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.TopLeft
        };
        card.Controls.Add(value);
        card.Controls.Add(caption);
        parent.Controls.Add(card, column, 0);
        kpis[key] = value;
    }

    private void AddColumn(string propertyName, string headerText, int minimumWidth)
    {
        resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            MinimumWidth = minimumWidth,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
    }

    private static void AddDetail(TableLayoutPanel fields, string labelText, Label valueLabel)
    {
        var row = fields.RowCount++;
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            ForeColor = MutedText,
            Margin = new Padding(0, 3, 8, 8)
        }, 0, row);
        valueLabel.Margin = new Padding(0, 3, 0, 8);
        fields.Controls.Add(valueLabel, 1, row);
    }

    private static Label DetailValueLabel()
    {
        return new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(37, 43, 39),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
        };
    }

    private static NumericUpDown PercentBox(decimal value)
    {
        var input = new NumericUpDown();
        SetupPercentBox(input, value);
        return input;
    }

    private static void SetupPercentBox(NumericUpDown input, decimal value)
    {
        input.Minimum = 0;
        input.Maximum = 100;
        input.Increment = 5;
        input.Value = value;
        input.TextAlign = HorizontalAlignment.Right;
    }

    private static string FormatMoney(decimal amount, string currencyCode)
    {
        return $"{amount.ToString("0.00", CultureInfo.InvariantCulture)} {currencyCode}";
    }

    private static string FormatScore(decimal? score)
    {
        return score.HasValue
            ? score.Value.ToString("0.##", CultureInfo.InvariantCulture)
            : "n/a";
    }

    private sealed class SupplierResultRow
    {
        private SupplierResultRow()
        {
        }

        [Browsable(false)]
        public SupplierEvaluation Evaluation { get; private init; } = new();

        public string SupplierName { get; private init; } = string.Empty;

        public string TotalSpend { get; private init; } = string.Empty;

        public string CommercialScore { get; private init; } = string.Empty;

        public string TechnicalScore { get; private init; } = string.Empty;

        public string RegulatoryScore { get; private init; } = string.Empty;

        public string TotalScore { get; private init; } = string.Empty;

        public string Classification { get; private init; } = string.Empty;

        public string ManualReviewRequired { get; private init; } = string.Empty;

        public int ManualReviewFlagCount { get; private init; }

        public static SupplierResultRow FromEvaluation(SupplierEvaluation supplierEvaluation, string currencyCode)
        {
            return new SupplierResultRow
            {
                Evaluation = supplierEvaluation,
                SupplierName = string.IsNullOrWhiteSpace(supplierEvaluation.SupplierName)
                    ? "(missing supplier)"
                    : supplierEvaluation.SupplierName,
                TotalSpend = FormatMoney(supplierEvaluation.TotalSpend, currencyCode),
                CommercialScore = FormatScore(supplierEvaluation.ScoreBreakdown.Commercial),
                TechnicalScore = FormatScore(supplierEvaluation.ScoreBreakdown.Technical),
                RegulatoryScore = FormatScore(supplierEvaluation.ScoreBreakdown.Regulatory),
                TotalScore = FormatScore(supplierEvaluation.ScoreBreakdown.Total),
                Classification = supplierEvaluation.Classification?.ToString() ?? "Unclassified",
                ManualReviewRequired = supplierEvaluation.RequiresManualReview ? "Yes" : "No",
                ManualReviewFlagCount = supplierEvaluation.ManualReviewFlags.Count
            };
        }
    }
}
