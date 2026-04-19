using System.ComponentModel;
using System.Globalization;
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.App;

internal sealed class MainForm : Form
{
    private readonly DashboardSettings settings;
    private readonly Label titleLabel = new();
    private readonly Label contextLabel = new();
    private readonly Label statusLabel = new();
    private readonly DataGridView resultsGrid = new();
    private readonly FlowLayoutPanel supplierCardsPanel = new();
    private readonly Panel dashboardViewPanel = new();
    private readonly Panel tableViewPanel = new();
    private readonly Button dashboardViewButton = new();
    private readonly Button tableViewButton = new();
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
    private readonly BindingList<SupplierResultRow> currentRows = [];

    private string? selectedSupplierName;

    public MainForm(DashboardSettings settings)
    {
        this.settings = settings;

        Text = "Tender Evaluation Dashboard";
        MinimumSize = new Size(1260, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.PageBackground;
        Font = AppTheme.BodyFont();

        BuildLayout();
        LoadDemoData();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildBody(), 0, 1);
        root.Controls.Add(BuildStatusBar(), 0, 2);
        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(24, 14, 24, 14),
            BackColor = AppTheme.PrimaryDark
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));

        titleLabel.Dock = DockStyle.Fill;
        titleLabel.ForeColor = Color.White;
        titleLabel.Font = AppTheme.TitleFont(18F);
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;

        contextLabel.Dock = DockStyle.Fill;
        contextLabel.ForeColor = AppTheme.PrimaryLight;
        contextLabel.Font = AppTheme.BodyFont(9.5F);
        contextLabel.TextAlign = ContentAlignment.MiddleRight;

        header.Controls.Add(titleLabel, 0, 0);
        header.Controls.Add(contextLabel, 1, 0);
        return header;
    }

    private Control BuildBody()
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(16),
            BackColor = AppTheme.PageBackground
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
        body.Controls.Add(BuildMainArea(), 0, 0);
        body.Controls.Add(BuildDetailsPanel(), 1, 0);
        return body;
    }

    private Control BuildMainArea()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(0, 0, 16, 0)
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.Controls.Add(BuildToolbar(), 0, 0);
        main.Controls.Add(BuildKpiPanel(), 0, 1);
        main.Controls.Add(BuildResultArea(), 0, 2);
        return main;
    }

    private Control BuildToolbar()
    {
        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5 };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var importButton = CreatePrimaryButton("Import Excel");
        importButton.Click += ImportButton_Click;
        var settingsButton = CreateSecondaryButton("Settings");
        settingsButton.Click += SettingsButton_Click;

        ConfigureToggleButton(dashboardViewButton, "Dashboard");
        ConfigureToggleButton(tableViewButton, "Table");
        dashboardViewButton.Click += (_, _) => ShowResultView(showDashboard: true);
        tableViewButton.Click += (_, _) => ShowResultView(showDashboard: false);

        toolbar.Controls.Add(importButton, 0, 0);
        toolbar.Controls.Add(settingsButton, 1, 0);
        toolbar.Controls.Add(new Panel(), 2, 0);
        toolbar.Controls.Add(dashboardViewButton, 3, 0);
        toolbar.Controls.Add(tableViewButton, 4, 0);
        return toolbar;
    }

    private Control BuildKpiPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, Margin = new Padding(0, 0, 0, 12) };
        for (var index = 0; index < 6; index++)
        {
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6f));
        }

        AddKpi(panel, 0, "Suppliers", "Suppliers");
        AddKpi(panel, 1, "ImportedLines", "Imported lines");
        AddKpi(panel, 2, "Recommended", "Recommended");
        AddKpi(panel, 3, "Conditional", "Conditional");
        AddKpi(panel, 4, "ManualReview", "Manual Review");
        AddKpi(panel, 5, "BestScore", "Best total score");
        return panel;
    }

    private Control BuildResultArea()
    {
        var host = Card();
        host.Padding = new Padding(14);
        dashboardViewPanel.Dock = DockStyle.Fill;
        tableViewPanel.Dock = DockStyle.Fill;
        dashboardViewPanel.Controls.Add(BuildDashboardView());
        tableViewPanel.Controls.Add(BuildTableView());
        host.Controls.Add(dashboardViewPanel);
        host.Controls.Add(tableViewPanel);
        return host;
    }

    private Control BuildDashboardView()
    {
        supplierCardsPanel.Dock = DockStyle.Fill;
        supplierCardsPanel.AutoScroll = true;
        supplierCardsPanel.FlowDirection = FlowDirection.LeftToRight;
        supplierCardsPanel.WrapContents = true;
        supplierCardsPanel.BackColor = AppTheme.CardBackground;
        return supplierCardsPanel;
    }

    private Control BuildTableView()
    {
        resultsGrid.Dock = DockStyle.Fill;
        resultsGrid.AllowUserToAddRows = false;
        resultsGrid.AllowUserToDeleteRows = false;
        resultsGrid.AutoGenerateColumns = false;
        resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        resultsGrid.BackgroundColor = AppTheme.CardBackground;
        resultsGrid.BorderStyle = BorderStyle.None;
        resultsGrid.ReadOnly = true;
        resultsGrid.RowHeadersVisible = false;
        resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        resultsGrid.MultiSelect = false;
        resultsGrid.EnableHeadersVisualStyles = false;
        resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.PrimaryLight;
        resultsGrid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.MainText;
        resultsGrid.ColumnHeadersDefaultCellStyle.Font = AppTheme.TitleFont(9F);
        resultsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(229, 235, 217);
        resultsGrid.DefaultCellStyle.SelectionForeColor = AppTheme.MainText;
        resultsGrid.SelectionChanged += (_, _) => SelectRow(SelectedTableRow());
        resultsGrid.CellFormatting += ResultsGrid_CellFormatting;

        AddColumn(nameof(SupplierResultRow.SupplierName), "Supplier", 150);
        AddColumn(nameof(SupplierResultRow.TotalSpendDisplay), "Spend", 90);
        AddColumn(nameof(SupplierResultRow.CommercialScoreDisplay), "Commercial", 80);
        AddColumn(nameof(SupplierResultRow.TechnicalScoreDisplay), "Technical", 80);
        AddColumn(nameof(SupplierResultRow.RegulatoryScoreDisplay), "Regulatory", 80);
        AddColumn(nameof(SupplierResultRow.TotalScoreDisplay), "Total", 80);
        AddColumn(nameof(SupplierResultRow.Classification), "Classification", 105);
        AddColumn(nameof(SupplierResultRow.ManualReviewFlagCount), "Flags", 60);
        resultsGrid.DataSource = currentRows;

        return resultsGrid;
    }

    private Control BuildDetailsPanel()
    {
        var card = Card();
        card.Padding = new Padding(18);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 11,
            ColumnCount = 1,
            BackColor = AppTheme.CardBackground
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(new Label
        {
            Text = "Supplier details",
            AutoSize = true,
            Font = AppTheme.TitleFont(13F),
            ForeColor = AppTheme.MainText,
            Margin = new Padding(0, 0, 0, 12)
        });
        AddDetail(panel, "Supplier", detailSupplier);
        AddDetail(panel, "Classification", detailClassification);
        AddDetail(panel, "Spend", detailSpend);
        AddDetail(panel, "Commercial", detailCommercial);
        AddDetail(panel, "Technical", detailTechnical);
        AddDetail(panel, "Regulatory", detailRegulatory);
        AddDetail(panel, "Total", detailTotal);
        AddDetail(panel, "Manual review flags", detailFlags);

        detailNotes.Dock = DockStyle.Fill;
        detailNotes.Multiline = true;
        detailNotes.ReadOnly = true;
        detailNotes.BorderStyle = BorderStyle.None;
        detailNotes.BackColor = AppTheme.PageBackground;
        detailNotes.ForeColor = AppTheme.MutedText;
        detailNotes.Margin = new Padding(0, 14, 0, 0);
        panel.Controls.Add(detailNotes, 0, 9);

        card.Controls.Add(panel);
        return card;
    }

    private Control BuildStatusBar()
    {
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.BackColor = AppTheme.PrimaryLight;
        statusLabel.ForeColor = AppTheme.MainText;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.Padding = new Padding(18, 0, 18, 0);
        return statusLabel;
    }

    private void LoadDemoData()
    {
        UpdateDashboardFromResult(
            DemoSupplierDataProvider.Create(settings),
            "Demo supplier data loaded. Import an Excel file when ready.");
    }

    private void UpdateDashboardFromResult(TenderEvaluationResult result, string statusText)
    {
        var rows = result.SupplierEvaluations
            .OrderByDescending(supplier => supplier.ScoreBreakdown.Total ?? -1)
            .Select(supplier => SupplierResultRow.FromSupplier(supplier, result.Tender.Settings.CurrencyCode))
            .ToList();

        currentRows.RaiseListChangedEvents = false;
        currentRows.Clear();
        foreach (var row in rows)
        {
            currentRows.Add(row);
        }

        currentRows.RaiseListChangedEvents = true;
        currentRows.ResetBindings();

        BuildSupplierCards();
        UpdateKpis(result, rows);
        UpdateHeader(result.Tender.Name, result.Tender.Settings.CurrencyCode);
        statusLabel.Text = statusText;
        SelectRow(rows.FirstOrDefault(row => row.SupplierName == selectedSupplierName) ?? rows.FirstOrDefault());
        ShowResultView(dashboardViewPanel.Visible || !tableViewPanel.Visible);
    }

    private void BuildSupplierCards()
    {
        supplierCardsPanel.SuspendLayout();
        supplierCardsPanel.Controls.Clear();

        foreach (var row in currentRows)
        {
            supplierCardsPanel.Controls.Add(CreateSupplierCard(row));
        }

        supplierCardsPanel.ResumeLayout();
    }

    private Control CreateSupplierCard(SupplierResultRow row)
    {
        var card = Card();
        card.Width = 270;
        card.Height = 182;
        card.Margin = new Padding(0, 0, 14, 14);
        card.Padding = new Padding(14);
        card.Cursor = Cursors.Hand;
        card.Tag = row;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            BackColor = AppTheme.CardBackground
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = row.SupplierName,
            AutoSize = true,
            Font = AppTheme.TitleFont(11F),
            ForeColor = AppTheme.MainText,
            Margin = new Padding(0, 0, 0, 8)
        });
        layout.Controls.Add(new Label
        {
            Text = row.Classification.ToString(),
            AutoSize = true,
            Font = AppTheme.TitleFont(9.5F),
            ForeColor = ClassificationColor(row.Classification),
            Margin = new Padding(0, 0, 0, 10)
        });
        layout.Controls.Add(new Label
        {
            Text = $"Spend: {row.TotalSpendDisplay}",
            AutoSize = true,
            ForeColor = AppTheme.MutedText,
            Margin = new Padding(0, 0, 0, 4)
        });
        layout.Controls.Add(new Label
        {
            Text = $"Total score: {row.TotalScoreDisplay}",
            AutoSize = true,
            Font = AppTheme.TitleFont(10F),
            ForeColor = AppTheme.MainText,
            Margin = new Padding(0, 0, 0, 4)
        });
        layout.Controls.Add(new Label
        {
            Text = $"Commercial {row.CommercialScoreDisplay} | Technical {row.TechnicalScoreDisplay} | Regulatory {row.RegulatoryScoreDisplay}",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.MutedText
        });

        foreach (Control control in layout.Controls)
        {
            control.Click += (_, _) => SelectRow(row);
        }

        card.Click += (_, _) => SelectRow(row);
        card.Controls.Add(layout);
        return card;
    }

    private void UpdateKpis(TenderEvaluationResult result, IReadOnlyCollection<SupplierResultRow> rows)
    {
        kpis["Suppliers"].Text = rows.Count.ToString(CultureInfo.InvariantCulture);
        kpis["ImportedLines"].Text = result.LineEvaluations.Count > 0
            ? result.LineEvaluations.Count.ToString(CultureInfo.InvariantCulture)
            : "Demo";
        kpis["Recommended"].Text = rows.Count(row => row.Classification == SupplierClassification.Recommended).ToString(CultureInfo.InvariantCulture);
        kpis["Conditional"].Text = rows.Count(row => row.Classification == SupplierClassification.Conditional).ToString(CultureInfo.InvariantCulture);
        kpis["ManualReview"].Text = rows.Count(row => row.Classification == SupplierClassification.ManualReview).ToString(CultureInfo.InvariantCulture);
        kpis["BestScore"].Text = rows.MaxBy(row => row.TotalScore)?.TotalScoreDisplay ?? "-";
    }

    private void UpdateHeader(string tenderName, string currencyCode)
    {
        titleLabel.Text = "Tender Evaluation Dashboard";
        contextLabel.Text = $"{settings.TenderType} mode | {tenderName} | Currency {currencyCode}";
    }

    private void SelectRow(SupplierResultRow? row)
    {
        selectedSupplierName = row?.SupplierName;
        UpdateDetailsPanel(row);

        foreach (DataGridViewRow gridRow in resultsGrid.Rows)
        {
            gridRow.Selected = row is not null &&
                gridRow.DataBoundItem is SupplierResultRow bound &&
                bound.SupplierName == row.SupplierName;
        }
    }

    private void UpdateDetailsPanel(SupplierResultRow? row)
    {
        if (row is null)
        {
            detailSupplier.Text = "Select a supplier";
            detailClassification.Text = "-";
            detailSpend.Text = "-";
            detailCommercial.Text = "-";
            detailTechnical.Text = "-";
            detailRegulatory.Text = "-";
            detailTotal.Text = "-";
            detailFlags.Text = "-";
            detailNotes.Text = "Choose a supplier from the dashboard or table to inspect the score breakdown and manual review status.";
            detailClassification.ForeColor = AppTheme.MutedText;
            return;
        }

        detailSupplier.Text = row.SupplierName;
        detailClassification.Text = row.Classification.ToString();
        detailClassification.ForeColor = ClassificationColor(row.Classification);
        detailSpend.Text = row.TotalSpendDisplay;
        detailCommercial.Text = row.CommercialScoreDisplay;
        detailTechnical.Text = row.TechnicalScoreDisplay;
        detailRegulatory.Text = row.RegulatoryScoreDisplay;
        detailTotal.Text = row.TotalScoreDisplay;
        detailFlags.Text = row.ManualReviewFlagCount.ToString(CultureInfo.InvariantCulture);
        detailNotes.Text = row.Notes;
    }

    private void ShowResultView(bool showDashboard)
    {
        dashboardViewPanel.Visible = showDashboard;
        tableViewPanel.Visible = !showDashboard;
        StyleToggleButton(dashboardViewButton, showDashboard);
        StyleToggleButton(tableViewButton, !showDashboard);
    }

    private void ImportButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Labels v1 Excel file",
            Filter = "Excel files (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!settings.TenderType.Equals("Labels", StringComparison.OrdinalIgnoreCase))
        {
            statusLabel.Text = $"{settings.TenderType} is configured for presentation only. Labels import logic is used in this prototype.";
        }

        try
        {
            var tenderSettings = LabelsV1DemoConfiguration.CreateTenderSettings();
            tenderSettings.CurrencyCode = settings.CurrencyCode;
            var service = CreateEvaluationService();
            var result = service.ImportAndEvaluate(dialog.FileName, settings.TenderName, tenderSettings);
            UpdateDashboardFromResult(result, $"Imported and evaluated {Path.GetFileName(dialog.FileName)}.");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException or ArgumentException)
        {
            statusLabel.Text = $"Import failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SettingsDialog(settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        LoadDemoData();
    }

    private LabelsTenderEvaluationService CreateEvaluationService()
    {
        return new LabelsTenderEvaluationService(
            new LabelsExcelImportService(),
            new LineEvaluationService(),
            new SupplierAggregationService(),
            new SupplierClassificationService(settings.RecommendedThreshold, settings.ConditionalThreshold));
    }

    private SupplierResultRow? SelectedTableRow()
    {
        return resultsGrid.SelectedRows.Count == 0
            ? null
            : resultsGrid.SelectedRows[0].DataBoundItem as SupplierResultRow;
    }

    private void ResultsGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || resultsGrid.Rows[e.RowIndex].DataBoundItem is not SupplierResultRow row)
        {
            return;
        }

        if (resultsGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(SupplierResultRow.Classification))
        {
            e.CellStyle.ForeColor = ClassificationColor(row.Classification);
            e.CellStyle.Font = AppTheme.TitleFont(9F);
        }
    }

    private void AddKpi(TableLayoutPanel panel, int column, string key, string label)
    {
        var card = Card();
        card.Margin = new Padding(0, 0, 10, 0);
        card.Padding = new Padding(12);

        var valueLabel = new Label
        {
            Text = "-",
            Dock = DockStyle.Top,
            Height = 36,
            Font = AppTheme.TitleFont(17F),
            ForeColor = AppTheme.MainText,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var title = new Label
        {
            Text = label,
            Dock = DockStyle.Bottom,
            Height = 26,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.BottomLeft
        };

        card.Controls.Add(valueLabel);
        card.Controls.Add(title);
        panel.Controls.Add(card, column, 0);
        kpis[key] = valueLabel;
    }

    private static void AddDetail(TableLayoutPanel panel, string labelText, Label valueLabel)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        row.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            ForeColor = AppTheme.MutedText
        }, 0, 0);
        row.Controls.Add(valueLabel, 1, 0);
        panel.Controls.Add(row);
    }

    private void AddColumn(string propertyName, string header, int width)
    {
        resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = header,
            MinimumWidth = width,
            FillWeight = width
        });
    }

    private static Panel Card()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CardBackground,
            Margin = new Padding(0)
        };
    }

    private static Label DetailValueLabel()
    {
        return new Label
        {
            AutoSize = true,
            ForeColor = AppTheme.MainText,
            Font = AppTheme.TitleFont(9F)
        };
    }

    private static Button CreatePrimaryButton(string text)
    {
        return new Button
        {
            Text = text,
            Width = 128,
            Height = 38,
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 8, 10, 8)
        };
    }

    private static Button CreateSecondaryButton(string text)
    {
        return new Button
        {
            Text = text,
            Width = 104,
            Height = 38,
            BackColor = AppTheme.CardBackground,
            ForeColor = AppTheme.MainText,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 8, 10, 8)
        };
    }

    private static void ConfigureToggleButton(Button button, string text)
    {
        button.Text = text;
        button.Width = 104;
        button.Height = 34;
        button.FlatStyle = FlatStyle.Flat;
        button.Margin = new Padding(0, 10, 8, 8);
    }

    private static void StyleToggleButton(Button button, bool selected)
    {
        button.BackColor = selected ? AppTheme.PrimaryDark : AppTheme.CardBackground;
        button.ForeColor = selected ? Color.White : AppTheme.MainText;
    }

    private static Color ClassificationColor(SupplierClassification? classification)
    {
        return classification switch
        {
            SupplierClassification.Recommended => AppTheme.PrimaryDark,
            SupplierClassification.Conditional => AppTheme.Warning,
            SupplierClassification.ManualReview => AppTheme.Warning,
            SupplierClassification.NotRecommended => AppTheme.Error,
            _ => AppTheme.MutedText
        };
    }

    private sealed class SupplierResultRow
    {
        private SupplierResultRow()
        {
        }

        public string SupplierName { get; private init; } = string.Empty;

        public decimal TotalSpend { get; private init; }

        public decimal? CommercialScore { get; private init; }

        public decimal? TechnicalScore { get; private init; }

        public decimal? RegulatoryScore { get; private init; }

        public decimal? TotalScore { get; private init; }

        public SupplierClassification? Classification { get; private init; }

        public int ManualReviewFlagCount { get; private init; }

        public string CurrencyCode { get; private init; } = "EUR";

        public string Notes { get; private init; } = string.Empty;

        public string TotalSpendDisplay => $"{TotalSpend:N2} {CurrencyCode}";

        public string CommercialScoreDisplay => FormatScore(CommercialScore);

        public string TechnicalScoreDisplay => FormatScore(TechnicalScore);

        public string RegulatoryScoreDisplay => FormatScore(RegulatoryScore);

        public string TotalScoreDisplay => FormatScore(TotalScore);

        public static SupplierResultRow FromSupplier(SupplierEvaluation supplier, string currencyCode)
        {
            return new SupplierResultRow
            {
                SupplierName = supplier.SupplierName,
                TotalSpend = supplier.TotalSpend,
                CommercialScore = supplier.ScoreBreakdown.Commercial,
                TechnicalScore = supplier.ScoreBreakdown.Technical,
                RegulatoryScore = supplier.ScoreBreakdown.Regulatory,
                TotalScore = supplier.ScoreBreakdown.Total,
                Classification = supplier.Classification,
                ManualReviewFlagCount = supplier.ManualReviewFlags.Count,
                CurrencyCode = currencyCode,
                Notes = BuildNotes(supplier)
            };
        }

        private static string BuildNotes(SupplierEvaluation supplier)
        {
            if (supplier.ManualReviewFlags.Count > 0)
            {
                var firstFlag = supplier.ManualReviewFlags[0];
                return $"Manual review is required. First flag: {firstFlag.FieldName ?? "Source data"} - {firstFlag.Reason}";
            }

            return supplier.ClassificationReason ??
                "No manual review flags. Classification is based on the provisional total score thresholds.";
        }

        private static string FormatScore(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("N2", CultureInfo.CurrentCulture) : "-";
        }
    }
}
