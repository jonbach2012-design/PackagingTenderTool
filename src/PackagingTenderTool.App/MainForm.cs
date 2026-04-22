using System.ComponentModel;
using System.Globalization;
using PackagingTenderTool.Core.Dashboard;
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;
using PackagingTenderTool.Core.Services;

namespace PackagingTenderTool.App;

internal sealed class MainForm : Form
{
    private readonly DashboardSettings settings;
    private readonly BindingList<SupplierResultRow> currentRows = [];
    private readonly Dictionary<string, Label> kpis = [];
    private readonly Label titleLabel = new();
    private readonly Label contextLabel = new();
    private readonly Label statusLabel = new();
    private readonly Button dashboardViewButton = new();
    private readonly Button tableViewButton = new();
    private readonly Button settingsButton = new();
    private readonly Button clearComparisonButton = new();
    private readonly Button importButton = new();
    private readonly Label importStatusLabel = new();
    private readonly Label importSummaryLabel = new();
    private readonly ProgressBar importProgressBar = new();
    private readonly Label leadSupplierInsightLabel = InsightValueLabel();
    private readonly Label averageScoreInsightLabel = InsightValueLabel();
    private readonly Label reviewWorkloadInsightLabel = InsightValueLabel();
    private readonly Label comparedSpendInsightLabel = InsightValueLabel();
    private readonly FlowLayoutPanel importAnalyticsMetricsPanel = new();
    private readonly DataGridView spendBySiteGrid = new();
    private readonly DataGridView spendByMaterialGrid = new();
    private readonly DataGridView spendBySizeGrid = new();
    private readonly DataGridView resultsGrid = new();
    private readonly FlowLayoutPanel supplierCardsPanel = new();
    private readonly FlowLayoutPanel comparisonPanel = new();
    private readonly Label comparisonStateLabel = new();
    private readonly ScoreChartControl totalScoreChart = new() { ChartTitle = "Total score by supplier", Mode = ScoreChartMode.TotalScoreBySupplier };
    private readonly ScoreChartControl dimensionChart = new() { ChartTitle = "Score dimension comparison", Mode = ScoreChartMode.ScoreDimensions };
    private readonly Panel dashboardViewPanel = new();
    private readonly Panel tableViewPanel = new();
    private readonly TableLayoutPanel bodyLayout = new();
    private readonly Panel settingsPanel = new();

    private readonly Label detailSupplier = DetailValueLabel();
    private readonly Label detailClassification = DetailValueLabel();
    private readonly Label detailSpend = DetailValueLabel();
    private readonly Label detailCommercial = DetailValueLabel();
    private readonly Label detailTechnical = DetailValueLabel();
    private readonly Label detailRegulatory = DetailValueLabel();
    private readonly Label detailTotal = DetailValueLabel();
    private readonly Label detailFlags = DetailValueLabel();
    private readonly TextBox detailNotes = new();

    private readonly TextBox tenderNameInput = new();
    private readonly TextBox currencyInput = new();
    private readonly NumericUpDown recommendedThresholdInput = new();
    private readonly NumericUpDown conditionalThresholdInput = new();
    private readonly CheckBox missingDataManualReviewInput = new();
    private readonly CheckBox normalizeInputValuesInput = new();
    private readonly CheckBox strictModeInput = new();
    private readonly CheckBox demoModeInput = new();

    private string? selectedSupplierName;
    private bool suppressSelectionEvents;

    public MainForm(DashboardSettings settings)
    {
        this.settings = settings;

        Text = "Tender Evaluation Dashboard";
        ClientSize = new Size(1560, 920);
        MinimumSize = new Size(1360, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.PageBackground;
        Font = AppTheme.BodyFont();

        BuildLayout();
        LoadSettingsInputs();
        LoadDemoData();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
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

        titleLabel.Text = "Tender Evaluation Dashboard";
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
        bodyLayout.Dock = DockStyle.Fill;
        bodyLayout.ColumnCount = 3;
        bodyLayout.Padding = new Padding(16);
        bodyLayout.BackColor = AppTheme.PageBackground;
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
        bodyLayout.Controls.Add(BuildMainArea(), 0, 0);
        bodyLayout.Controls.Add(BuildDetailsPanel(), 1, 0);
        bodyLayout.Controls.Add(BuildSettingsPanel(), 2, 0);
        return bodyLayout;
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
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.Controls.Add(BuildToolbar(), 0, 0);
        main.Controls.Add(BuildKpiPanel(), 0, 1);
        main.Controls.Add(BuildResultArea(), 0, 2);
        return main;
    }

    private Control BuildToolbar()
    {
        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6 };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        StylePrimaryButton(importButton, "Import Excel");
        importButton.Click += ImportButton_Click;
        settingsButton.Text = "Settings";
        settingsButton.Click += (_, _) => ToggleSettingsPanel();
        StyleSecondaryButton(settingsButton);

        ConfigureSegmentButton(dashboardViewButton, "Dashboard");
        ConfigureSegmentButton(tableViewButton, "Table");
        dashboardViewButton.Click += (_, _) => ShowResultView(showDashboard: true);
        tableViewButton.Click += (_, _) => ShowResultView(showDashboard: false);

        toolbar.Controls.Add(importButton, 0, 0);
        toolbar.Controls.Add(settingsButton, 1, 0);
        toolbar.Controls.Add(new Panel(), 2, 0);
        toolbar.Controls.Add(new Label { Text = "View", AutoSize = true, ForeColor = AppTheme.MutedText, Margin = new Padding(0, 17, 8, 0) }, 3, 0);
        toolbar.Controls.Add(dashboardViewButton, 4, 0);
        toolbar.Controls.Add(tableViewButton, 5, 0);
        return toolbar;
    }

    private Control BuildKpiPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, Margin = new Padding(0, 0, 0, 10) };
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
        var dashboard = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute, 300));
        dashboard.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute, 186));
        dashboard.Controls.Add(BuildInsightPanel(), 0, 0);
        dashboard.Controls.Add(BuildAnalyticsPanel(), 0, 1);
        dashboard.Controls.Add(BuildSupplierCardsPanel(), 0, 2);
        dashboard.Controls.Add(BuildComparisonPanel(), 0, 3);
        return dashboard;
    }

    private Control BuildInsightPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Margin = new Padding(0, 0, 0, 12) };
        for (var index = 0; index < 4; index++)
        {
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        panel.Controls.Add(InsightCard("Lead supplier", leadSupplierInsightLabel), 0, 0);
        panel.Controls.Add(InsightCard("Average total score", averageScoreInsightLabel), 1, 0);
        panel.Controls.Add(InsightCard("Review workload", reviewWorkloadInsightLabel), 2, 0);
        panel.Controls.Add(InsightCard("Compared spend", comparedSpendInsightLabel), 3, 0);
        return panel;
    }

    private Control BuildChartsPanel()
    {
        var charts = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0, 0, 0, 12) };
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        totalScoreChart.Dock = DockStyle.Fill;
        dimensionChart.Dock = DockStyle.Fill;
        charts.Controls.Add(ChartCard(totalScoreChart), 0, 0);
        charts.Controls.Add(ChartCard(dimensionChart), 1, 0);
        return charts;
    }

    private Control BuildAnalyticsPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = new Padding(0, 0, 0, 12) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var metricsCard = Card();
        metricsCard.Padding = new Padding(12, 8, 12, 8);
        var metricsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = AppTheme.CardBackground };
        metricsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        metricsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        metricsLayout.Controls.Add(new Label
        {
            Text = "Import and analytics summary",
            Dock = DockStyle.Fill,
            Font = AppTheme.TitleFont(10F),
            ForeColor = AppTheme.MainText,
            AutoEllipsis = true
        }, 0, 0);

        importAnalyticsMetricsPanel.Dock = DockStyle.Fill;
        importAnalyticsMetricsPanel.AutoScroll = true;
        importAnalyticsMetricsPanel.WrapContents = true;
        importAnalyticsMetricsPanel.FlowDirection = FlowDirection.LeftToRight;
        importAnalyticsMetricsPanel.BackColor = AppTheme.CardBackground;
        metricsLayout.Controls.Add(importAnalyticsMetricsPanel, 0, 1);
        metricsCard.Controls.Add(metricsLayout);
        panel.Controls.Add(metricsCard, 0, 0);

        var breakdowns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        breakdowns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        breakdowns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        breakdowns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        breakdowns.Controls.Add(BreakdownCard("Spend by site", spendBySiteGrid), 0, 0);
        breakdowns.Controls.Add(BreakdownCard("Spend by material", spendByMaterialGrid), 1, 0);
        breakdowns.Controls.Add(BreakdownCard("Spend by label size", spendBySizeGrid), 2, 0);
        panel.Controls.Add(breakdowns, 0, 1);

        return panel;
    }

    private Control BuildSupplierCardsPanel()
    {
        var host = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(0, 4, 0, 0) };
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.Controls.Add(new Label
        {
            Text = "Supplier overview",
            AutoSize = true,
            Font = AppTheme.TitleFont(10.5F),
            ForeColor = AppTheme.MainText,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);
        supplierCardsPanel.Dock = DockStyle.Fill;
        supplierCardsPanel.AutoScroll = true;
        supplierCardsPanel.FlowDirection = FlowDirection.LeftToRight;
        supplierCardsPanel.WrapContents = true;
        supplierCardsPanel.BackColor = AppTheme.CardBackground;
        host.Controls.Add(supplierCardsPanel, 0, 1);
        return host;
    }

    private Control BuildComparisonPanel()
    {
        var host = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(0, 8, 0, 0) };
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(new Label
        {
            Text = "Selected supplier comparison",
            AutoSize = true,
            Font = AppTheme.TitleFont(10.5F),
            ForeColor = AppTheme.MainText,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);
        comparisonStateLabel.AutoSize = true;
        comparisonStateLabel.ForeColor = AppTheme.MutedText;
        comparisonStateLabel.Margin = new Padding(12, 3, 0, 0);
        header.Controls.Add(comparisonStateLabel, 1, 0);
        clearComparisonButton.Text = "Clear";
        clearComparisonButton.Width = 72;
        clearComparisonButton.Height = 28;
        clearComparisonButton.FlatStyle = FlatStyle.Flat;
        clearComparisonButton.BackColor = AppTheme.CardBackground;
        clearComparisonButton.ForeColor = AppTheme.MainText;
        clearComparisonButton.FlatAppearance.BorderColor = AppTheme.PrimaryLight;
        clearComparisonButton.Click += (_, _) => ClearComparisonSelection();
        header.Controls.Add(clearComparisonButton, 2, 0);
        host.Controls.Add(header, 0, 0);

        comparisonPanel.Dock = DockStyle.Fill;
        comparisonPanel.AutoScroll = true;
        comparisonPanel.WrapContents = false;
        comparisonPanel.FlowDirection = FlowDirection.LeftToRight;
        comparisonPanel.BackColor = AppTheme.CardBackground;
        host.Controls.Add(comparisonPanel, 0, 1);
        return host;
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
        resultsGrid.ReadOnly = false;
        resultsGrid.RowHeadersVisible = false;
        resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        resultsGrid.MultiSelect = false;
        resultsGrid.EnableHeadersVisualStyles = false;
        resultsGrid.GridColor = AppTheme.CardBorder;
        resultsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        resultsGrid.ColumnHeadersHeight = 38;
        resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.PrimaryLight;
        resultsGrid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.MainText;
        resultsGrid.ColumnHeadersDefaultCellStyle.Font = AppTheme.TitleFont(9F);
        resultsGrid.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);
        resultsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(229, 235, 217);
        resultsGrid.DefaultCellStyle.SelectionForeColor = AppTheme.MainText;
        resultsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 248);
        resultsGrid.SelectionChanged += (_, _) => SelectRow(SelectedTableRow());
        resultsGrid.CellFormatting += ResultsGrid_CellFormatting;
        resultsGrid.CellValueChanged += ResultsGrid_CellValueChanged;
        resultsGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (resultsGrid.IsCurrentCellDirty)
            {
                resultsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };

        resultsGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SupplierResultRow.Compare),
            HeaderText = "Compare",
            MinimumWidth = 76,
            FillWeight = 76
        });
        AddColumn(nameof(SupplierResultRow.SupplierName), "Supplier", 150);
        AddColumn(nameof(SupplierResultRow.TotalSpendDisplay), "Spend", 95);
        AddColumn(nameof(SupplierResultRow.CommercialScoreDisplay), "Commercial", 82);
        AddColumn(nameof(SupplierResultRow.TechnicalScoreDisplay), "Technical", 82);
        AddColumn(nameof(SupplierResultRow.RegulatoryScoreDisplay), "Regulatory", 82);
        AddColumn(nameof(SupplierResultRow.TotalScoreDisplay), "Total", 76);
        AddColumn(nameof(SupplierResultRow.Classification), "Classification", 105);
        AddColumn(nameof(SupplierResultRow.ManualReviewFlagCount), "Flags", 62);
        resultsGrid.DataSource = currentRows;
        return resultsGrid;
    }

    private Control BuildDetailsPanel()
    {
        var card = Card();
        card.Padding = new Padding(18);
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 10, ColumnCount = 1, BackColor = AppTheme.CardBackground };
        for (var index = 0; index < 9; index++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(SectionTitle("Supplier details"));
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

    private Control BuildSettingsPanel()
    {
        settingsPanel.Dock = DockStyle.Fill;
        settingsPanel.BackColor = AppTheme.PageBackground;
        settingsPanel.Padding = new Padding(0, 0, 0, 0);
        settingsPanel.Visible = false;

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, BackColor = AppTheme.PageBackground, Padding = new Padding(16, 0, 0, 0) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(BuildSettingsHeader(), 0, 0);
        panel.Controls.Add(BuildSettingsGroup("General", group =>
        {
            AddInput(group, "Tender name", tenderNameInput);
            AddInput(group, "Currency", currencyInput);
            group.Controls.Add(new Label { Text = $"Tender type: {settings.TenderType}", AutoSize = true, ForeColor = AppTheme.MutedText, Margin = new Padding(0, 6, 0, 8) });
            demoModeInput.Text = "Demo mode";
            group.Controls.Add(demoModeInput);
        }), 0, 1);

        missingDataManualReviewInput.Text = "Missing data = Manual Review";
        normalizeInputValuesInput.Text = "Normalize input values";
        strictModeInput.Text = "Strict mode";
        panel.Controls.Add(BuildSettingsGroup("Evaluation behavior", group =>
        {
            group.Controls.Add(missingDataManualReviewInput);
            group.Controls.Add(normalizeInputValuesInput);
            group.Controls.Add(strictModeInput);
        }), 0, 2);

        panel.Controls.Add(BuildSettingsGroup("Advanced", group =>
        {
            group.Controls.Add(new Label
            {
                Text = "Provisional classification thresholds. Keep Recommended greater than or equal to Conditional.",
                AutoSize = true,
                MaximumSize = new Size(320, 0),
                ForeColor = AppTheme.MutedText,
                Margin = new Padding(0, 0, 0, 8)
            });
            AddInput(group, "Recommended threshold", ConfigurePercentInput(recommendedThresholdInput));
            AddInput(group, "Conditional threshold", ConfigurePercentInput(conditionalThresholdInput));
        }), 0, 3);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, BackColor = AppTheme.PageBackground };
        var applyButton = PrimaryButton("Apply settings");
        applyButton.Click += (_, _) => ApplySettings();
        var closeButton = SecondaryButton("Back");
        closeButton.Click += (_, _) => ToggleSettingsPanel(forceOpen: false);
        actions.Controls.Add(applyButton);
        actions.Controls.Add(closeButton);
        panel.Controls.Add(actions, 0, 5);

        settingsPanel.Controls.Add(panel);
        return settingsPanel;
    }

    private Control BuildStatusBar()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = AppTheme.PrimaryLight,
            Padding = new Padding(18, 8, 18, 6)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        importStatusLabel.Text = "Import ready";
        importStatusLabel.Dock = DockStyle.Fill;
        importStatusLabel.Font = AppTheme.TitleFont(9.5F);
        importStatusLabel.ForeColor = AppTheme.PrimaryDark;
        importStatusLabel.TextAlign = ContentAlignment.MiddleLeft;

        importSummaryLabel.Text = "No file imported in this session.";
        importSummaryLabel.Dock = DockStyle.Fill;
        importSummaryLabel.ForeColor = AppTheme.MutedText;
        importSummaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        importSummaryLabel.AutoEllipsis = true;

        statusLabel.Text = "Dashboard ready.";
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.ForeColor = AppTheme.MainText;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.AutoEllipsis = true;

        importProgressBar.Dock = DockStyle.Fill;
        importProgressBar.Style = ProgressBarStyle.Marquee;
        importProgressBar.MarqueeAnimationSpeed = 0;
        importProgressBar.Visible = false;

        panel.Controls.Add(importStatusLabel, 0, 0);
        panel.SetRowSpan(importStatusLabel, 2);
        panel.Controls.Add(statusLabel, 1, 0);
        panel.SetColumnSpan(statusLabel, 2);
        panel.Controls.Add(importSummaryLabel, 1, 1);
        panel.SetColumnSpan(importSummaryLabel, 2);
        panel.Controls.Add(importProgressBar, 3, 0);
        panel.SetRowSpan(importProgressBar, 2);
        return panel;
    }

    private Control BuildSettingsHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.PrimaryDark, Padding = new Padding(16, 10, 16, 10) };
        header.Controls.Add(new Label
        {
            Text = "Settings environment\r\nAdjust prototype behavior, then return to dashboard.",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = AppTheme.TitleFont(10F)
        });
        return header;
    }

    private static Control BuildSettingsGroup(string title, Action<TableLayoutPanel> buildContent)
    {
        var card = Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Margin = new Padding(0, 0, 0, 12);
        card.Padding = new Padding(14);

        var group = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = AppTheme.CardBackground };
        group.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = AppTheme.TitleFont(10.5F),
            ForeColor = AppTheme.MainText,
            Margin = new Padding(0, 0, 0, 8)
        });
        buildContent(group);
        card.Controls.Add(group);
        return card;
    }

    private void LoadDemoData()
    {
        try
        {
            var result = DemoSupplierDataProvider.Create(settings);
            UpdateDashboardFromResult(result, "Demo supplier data loaded. Import an Excel file when ready.");
            SetImportFeedback(
                "Demo data",
                BuildResultSummary(result, "Generated sample data"),
                AppTheme.PrimaryDark,
                isBusy: false);
        }
        catch (Exception ex)
        {
            AppExceptionReporter.Handle(ex);
            UpdateDashboardFromResult(new TenderEvaluationResult(), "Demo data could not be loaded. Dashboard is in a safe empty state.");
            SetImportFeedback("Demo failed", ex.Message, AppTheme.Error, isBusy: false);
        }
    }

    private void UpdateDashboardFromResult(TenderEvaluationResult result, string statusText)
    {
        result ??= new TenderEvaluationResult();
        result.Tender ??= new Tender();
        result.Tender.Settings ??= new TenderSettings();
        result.SupplierEvaluations ??= [];
        result.LineEvaluations ??= [];

        var existingCompare = currentRows.Where(row => row.Compare).Select(row => row.SupplierName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = result.SupplierEvaluations
            .Where(supplier => supplier is not null)
            .OrderByDescending(supplier => supplier.ScoreBreakdown.Total ?? -1)
            .Select(supplier => SupplierResultRow.FromSupplier(supplier, SafeCurrency(result.Tender.Settings.CurrencyCode)))
            .ToList();
        foreach (var row in rows.Where(row => existingCompare.Contains(row.SupplierName)).Take(4))
        {
            row.Compare = true;
        }

        try
        {
            suppressSelectionEvents = true;
            currentRows.RaiseListChangedEvents = false;
            currentRows.Clear();
            foreach (var row in rows)
            {
                currentRows.Add(row);
            }

            currentRows.RaiseListChangedEvents = true;
            currentRows.ResetBindings();
        }
        finally
        {
            suppressSelectionEvents = false;
        }

        BuildSupplierCards();
        UpdateKpis(result, rows);
        UpdateInsights(rows);
        UpdateAnalyticsPresentation(new TenderDashboardViewModelFactory().Create(result));
        UpdateHeader(string.IsNullOrWhiteSpace(result.Tender.Name) ? settings.TenderName : result.Tender.Name, SafeCurrency(result.Tender.Settings.CurrencyCode));
        UpdateChartsAndComparison();
        statusLabel.Text = string.IsNullOrWhiteSpace(statusText) ? "Dashboard updated." : statusText;
        SelectRow(rows.FirstOrDefault(row => row.SupplierName == selectedSupplierName) ?? rows.FirstOrDefault());
        ShowResultView(dashboardViewPanel.Visible || !tableViewPanel.Visible);
    }

    private void BuildSupplierCards()
    {
        supplierCardsPanel.SuspendLayout();
        supplierCardsPanel.Controls.Clear();
        if (currentRows.Count == 0)
        {
            supplierCardsPanel.Controls.Add(new Label
            {
                Text = "No supplier results are available. Load demo data or import an Excel file.",
                AutoSize = true,
                ForeColor = AppTheme.MutedText,
                Margin = new Padding(0, 12, 0, 0)
            });
        }
        else
        {
            foreach (var row in currentRows)
            {
                supplierCardsPanel.Controls.Add(CreateSupplierCard(row));
            }
        }

        supplierCardsPanel.ResumeLayout();
    }

    private Control CreateSupplierCard(SupplierResultRow row)
    {
        var card = Card();
        card.Width = 260;
        card.Height = 178;
        card.Margin = new Padding(0, 0, 14, 14);
        card.Padding = new Padding(14);
        card.Cursor = Cursors.Hand;
        card.BackColor = row.Compare ? AppTheme.PrimaryLight : AppTheme.CardBackground;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, BackColor = card.BackColor };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = row.SupplierName, AutoSize = false, Height = 24, Dock = DockStyle.Top, AutoEllipsis = true, Font = AppTheme.TitleFont(11F), ForeColor = AppTheme.MainText });
        layout.Controls.Add(new Label { Text = row.Classification.ToString(), AutoSize = true, Font = AppTheme.TitleFont(9.5F), ForeColor = ClassificationColor(row.Classification), Margin = new Padding(0, 4, 0, 8) });
        layout.Controls.Add(new Label { Text = $"Spend: {row.TotalSpendDisplay}", AutoSize = true, ForeColor = AppTheme.MutedText });
        layout.Controls.Add(new Label { Text = $"Total score: {row.TotalScoreDisplay}", AutoSize = true, Font = AppTheme.TitleFont(10F), ForeColor = AppTheme.MainText });
        layout.Controls.Add(new Label { Text = $"C {row.CommercialScoreDisplay} | T {row.TechnicalScoreDisplay} | R {row.RegulatoryScoreDisplay}", Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = AppTheme.MutedText });

        var compare = new CheckBox { Text = "Compare", Checked = row.Compare, AutoSize = true, ForeColor = AppTheme.MainText };
        compare.CheckedChanged += (_, _) => SetCompare(row, compare.Checked);
        layout.Controls.Add(compare);

        foreach (Control control in layout.Controls)
        {
            if (control is not CheckBox)
            {
                control.Click += (_, _) => SelectRow(row);
            }
        }

        card.Click += (_, _) => SelectRow(row);
        card.Controls.Add(layout);
        return card;
    }

    private void UpdateChartsAndComparison()
    {
        var compared = ComparedRows().ToList();
        totalScoreChart.SetRows(currentRows.ToList());
        dimensionChart.SetRows(compared.Count >= 2 ? compared : currentRows.ToList());
        BuildComparisonCards(compared);
        UpdateComparedSpendInsight(compared);
    }

    private void BuildComparisonCards(IReadOnlyList<SupplierResultRow> compared)
    {
        comparisonPanel.SuspendLayout();
        comparisonPanel.Controls.Clear();
        comparisonStateLabel.Text = $"{compared.Count}/4 selected";
        clearComparisonButton.Enabled = compared.Count > 0;

        if (compared.Count < 2)
        {
            comparisonPanel.Controls.Add(new Label
            {
                Text = "Select 2 to 4 suppliers with Compare to build a side-by-side comparison.",
                AutoSize = true,
                ForeColor = AppTheme.MutedText,
                Margin = new Padding(0, 8, 0, 0)
            });
        }
        else
        {
            foreach (var row in compared)
            {
                comparisonPanel.Controls.Add(CreateComparisonCard(row));
            }
        }

        comparisonPanel.ResumeLayout();
    }

    private void ClearComparisonSelection()
    {
        foreach (var row in currentRows)
        {
            row.Compare = false;
        }

        currentRows.ResetBindings();
        BuildSupplierCards();
        UpdateChartsAndComparison();
        statusLabel.Text = "Comparison selection cleared.";
    }

    private Control CreateComparisonCard(SupplierResultRow row)
    {
        var panel = new TableLayoutPanel
        {
            Width = 238,
            Height = 112,
            RowCount = 5,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(10),
            BackColor = AppTheme.PageBackground
        };
        panel.Controls.Add(new Label { Text = row.SupplierName, AutoSize = false, Height = 22, Dock = DockStyle.Top, AutoEllipsis = true, Font = AppTheme.TitleFont(9.5F), ForeColor = AppTheme.MainText });
        panel.Controls.Add(new Label { Text = row.Classification.ToString(), AutoSize = true, ForeColor = ClassificationColor(row.Classification) });
        panel.Controls.Add(new Label { Text = $"Spend {row.TotalSpendDisplay}", AutoSize = true, ForeColor = AppTheme.MutedText });
        panel.Controls.Add(new Label { Text = $"Total {row.TotalScoreDisplay} | Flags {row.ManualReviewFlagCount}", AutoSize = true, ForeColor = AppTheme.MainText });
        panel.Controls.Add(new Label { Text = $"C {row.CommercialScoreDisplay} / T {row.TechnicalScoreDisplay} / R {row.RegulatoryScoreDisplay}", AutoSize = true, ForeColor = AppTheme.MutedText });
        return panel;
    }

    private void UpdateKpis(TenderEvaluationResult result, IReadOnlyCollection<SupplierResultRow> rows)
    {
        SetKpi("Suppliers", rows.Count.ToString(CultureInfo.InvariantCulture));
        SetKpi("ImportedLines", result.LineEvaluations.Count > 0 ? result.LineEvaluations.Count.ToString(CultureInfo.InvariantCulture) : settings.DemoMode ? "Demo" : "0");
        SetKpi("Recommended", rows.Count(row => row.Classification == SupplierClassification.Recommended).ToString(CultureInfo.InvariantCulture));
        SetKpi("Conditional", rows.Count(row => row.Classification == SupplierClassification.Conditional).ToString(CultureInfo.InvariantCulture));
        SetKpi("ManualReview", rows.Count(row => row.Classification == SupplierClassification.ManualReview).ToString(CultureInfo.InvariantCulture));
        SetKpi("BestScore", rows.MaxBy(row => row.TotalScore)?.TotalScoreDisplay ?? "-");
    }

    private void SetKpi(string key, string value)
    {
        if (kpis.TryGetValue(key, out var label))
        {
            label.Text = value;
        }
    }

    private void UpdateInsights(IReadOnlyCollection<SupplierResultRow> rows)
    {
        if (rows.Count == 0)
        {
            leadSupplierInsightLabel.Text = "-";
            averageScoreInsightLabel.Text = "-";
            reviewWorkloadInsightLabel.Text = "No results";
            comparedSpendInsightLabel.Text = "-";
            return;
        }

        var leadSupplier = rows.MaxBy(row => row.TotalScore ?? -1);
        leadSupplierInsightLabel.Text = leadSupplier is null
            ? "-"
            : $"{leadSupplier.SupplierName} ({leadSupplier.TotalScoreDisplay})";

        var scoredRows = rows.Where(row => row.TotalScore.HasValue).ToList();
        averageScoreInsightLabel.Text = scoredRows.Count == 0
            ? "-"
            : scoredRows.Average(row => row.TotalScore!.Value).ToString("N1", CultureInfo.CurrentCulture);

        var manualReviewCount = rows.Count(row => row.Classification == SupplierClassification.ManualReview || row.ManualReviewFlagCount > 0);
        reviewWorkloadInsightLabel.Text = manualReviewCount == 0
            ? "No manual review"
            : $"{manualReviewCount} supplier(s)";

        UpdateComparedSpendInsight(ComparedRows().ToList());
    }

    private void UpdateAnalyticsPresentation(TenderDashboardViewModel viewModel)
    {
        importAnalyticsMetricsPanel.SuspendLayout();
        importAnalyticsMetricsPanel.Controls.Clear();
        foreach (var metric in viewModel.ImportMetrics.Concat(viewModel.AnalyticsMetrics))
        {
            importAnalyticsMetricsPanel.Controls.Add(MetricPill(metric));
        }

        importAnalyticsMetricsPanel.ResumeLayout();

        BindBreakdownGrid(spendBySiteGrid, viewModel.SpendBySite);
        BindBreakdownGrid(spendByMaterialGrid, viewModel.SpendByMaterial);
        BindBreakdownGrid(spendBySizeGrid, viewModel.SpendByLabelSize);
    }

    private void UpdateComparedSpendInsight(IReadOnlyCollection<SupplierResultRow> compared)
    {
        if (compared.Count < 2)
        {
            comparedSpendInsightLabel.Text = "Select 2+";
            return;
        }

        var spend = compared.Sum(row => row.TotalSpend);
        var currency = compared.FirstOrDefault()?.CurrencyCode ?? settings.CurrencyCode;
        comparedSpendInsightLabel.Text = $"{spend:N0} {currency}";
    }

    private void UpdateHeader(string tenderName, string currencyCode)
    {
        contextLabel.Text = $"{settings.TenderType} mode | {tenderName} | Currency {currencyCode}";
    }

    private void SelectRow(SupplierResultRow? row)
    {
        if (suppressSelectionEvents)
        {
            return;
        }

        selectedSupplierName = row?.SupplierName;
        UpdateDetailsPanel(row);
        try
        {
            suppressSelectionEvents = true;
            foreach (DataGridViewRow gridRow in resultsGrid.Rows)
            {
                gridRow.Selected = row is not null && gridRow.DataBoundItem is SupplierResultRow bound && bound.SupplierName == row.SupplierName;
            }
        }
        finally
        {
            suppressSelectionEvents = false;
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
            detailNotes.Text = "Choose a supplier from the dashboard or table to inspect scores, flags, and rationale.";
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
        if (dashboardViewPanel.IsDisposed || tableViewPanel.IsDisposed)
        {
            return;
        }

        try
        {
            dashboardViewPanel.Visible = showDashboard;
            tableViewPanel.Visible = !showDashboard;
            StyleSegmentButton(dashboardViewButton, showDashboard);
            StyleSegmentButton(tableViewButton, !showDashboard);
        }
        catch (ObjectDisposedException)
        {
            // Ignore late events during form shutdown.
        }
    }

    private void ToggleSettingsPanel(bool? forceOpen = null)
    {
        if (settingsPanel.IsDisposed || bodyLayout.ColumnStyles.Count < 3)
        {
            return;
        }

        var open = forceOpen ?? !settingsPanel.Visible;
        settingsPanel.Visible = open;
        bodyLayout.ColumnStyles[2].Width = open ? 430 : 0;
        settingsButton.Text = open ? "Hide settings" : "Settings";
        statusLabel.Text = open ? "Settings environment open. Apply changes or return to dashboard." : "Returned to dashboard.";
    }

    private void ApplySettings()
    {
        if (recommendedThresholdInput.Value < conditionalThresholdInput.Value)
        {
            statusLabel.Text = "Settings not applied: recommended threshold must be greater than or equal to conditional threshold.";
            return;
        }

        settings.TenderName = string.IsNullOrWhiteSpace(tenderNameInput.Text) ? settings.TenderName : tenderNameInput.Text.Trim();
        settings.CurrencyCode = string.IsNullOrWhiteSpace(currencyInput.Text) ? "EUR" : currencyInput.Text.Trim().ToUpperInvariant();
        settings.RecommendedThreshold = recommendedThresholdInput.Value;
        settings.ConditionalThreshold = conditionalThresholdInput.Value;
        settings.MissingDataManualReview = missingDataManualReviewInput.Checked;
        settings.NormalizeInputValues = normalizeInputValuesInput.Checked;
        settings.StrictMode = strictModeInput.Checked;
        settings.DemoMode = demoModeInput.Checked;
        if (settings.DemoMode)
        {
            LoadDemoData();
            statusLabel.Text = "Settings applied. Demo data refreshed with current thresholds.";
        }
        else
        {
            UpdateDashboardFromResult(new TenderEvaluationResult(), "Settings applied. Demo mode is off; import an Excel file to evaluate suppliers.");
        }
    }

    private void LoadSettingsInputs()
    {
        tenderNameInput.MaxLength = 80;
        currencyInput.MaxLength = 3;
        tenderNameInput.Text = settings.TenderName;
        currencyInput.Text = settings.CurrencyCode;
        recommendedThresholdInput.Value = settings.RecommendedThreshold;
        conditionalThresholdInput.Value = settings.ConditionalThreshold;
        missingDataManualReviewInput.Checked = settings.MissingDataManualReview;
        normalizeInputValuesInput.Checked = settings.NormalizeInputValues;
        strictModeInput.Checked = settings.StrictMode;
        demoModeInput.Checked = settings.DemoMode;
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
            SetImportFeedback("Import canceled", "No file selected.", AppTheme.MutedText, isBusy: false);
            return;
        }

        try
        {
            SetImportFeedback(
                "Import running",
                $"Reading {Path.GetFileName(dialog.FileName)} and evaluating supplier results...",
                AppTheme.Warning,
                isBusy: true);
            UseWaitCursor = true;
            importButton.Enabled = false;
            Application.DoEvents();

            if (!settings.TenderType.Equals("Labels", StringComparison.OrdinalIgnoreCase))
            {
                statusLabel.Text = $"{settings.TenderType} is presentation-only in this prototype. Labels v1 import logic will be used.";
            }

            var tenderSettings = LabelsV1DemoConfiguration.CreateTenderSettings();
            tenderSettings.CurrencyCode = settings.CurrencyCode;
            var result = CreateEvaluationService().ImportAndEvaluate(dialog.FileName, settings.TenderName, tenderSettings);
            var fileName = Path.GetFileName(dialog.FileName);
            UpdateDashboardFromResult(result, $"Imported and evaluated {fileName}.");
            SetImportFeedback("Import succeeded", BuildResultSummary(result, fileName), AppTheme.PrimaryDark, isBusy: false);
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Import failed: {ex.Message}";
            SetImportFeedback("Import failed", ex.Message, AppTheme.Error, isBusy: false);
            AppExceptionReporter.LogSilently(ex);
            MessageBox.Show(this, ex.Message, "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
            importButton.Enabled = true;
            importProgressBar.Visible = false;
            importProgressBar.MarqueeAnimationSpeed = 0;
        }
    }

    private LabelsTenderEvaluationService CreateEvaluationService()
    {
        return new LabelsTenderEvaluationService(
            new LabelsExcelImportService(),
            new LineEvaluationService(new LabelsEvaluationStrategy(new EprFeeService())),
            new SupplierAggregationService(),
            new SupplierClassificationService(settings.RecommendedThreshold, settings.ConditionalThreshold));
    }

    private void SetImportFeedback(string state, string summary, Color stateColor, bool isBusy)
    {
        importStatusLabel.Text = state;
        importStatusLabel.ForeColor = stateColor;
        importSummaryLabel.Text = string.IsNullOrWhiteSpace(summary) ? "No import details available." : summary;
        importProgressBar.Visible = isBusy;
        importProgressBar.MarqueeAnimationSpeed = isBusy ? 30 : 0;
    }

    private static string BuildResultSummary(TenderEvaluationResult result, string sourceName)
    {
        var supplierCount = result.SupplierEvaluations?.Count ?? 0;
        var lineCount = result.ImportSummary?.ImportedRows
            ?? result.LineEvaluations?.Count
            ?? result.Tender?.LabelLineItems?.Count
            ?? 0;
        var invalidRows = result.ImportSummary?.InvalidRows ?? 0;
        var skippedRows = result.ImportSummary?.SkippedRows ?? 0;
        var flagCount = result.ImportSummary?.ManualReviewFlagCount
            ?? result.SupplierEvaluations?.Sum(supplier => supplier.ManualReviewFlags?.Count ?? 0)
            ?? 0;
        var spend = result.Analytics?.TotalSpend;
        var validation = invalidRows == 0 && flagCount == 0
            ? "No import validation issues"
            : $"{invalidRows} invalid rows / {flagCount} flags";
        var spendText = spend.HasValue ? $" | Spend: {spend.Value:N0}" : string.Empty;

        return $"{sourceName} | Rows: {lineCount} | Suppliers: {supplierCount} | Skipped: {skippedRows} | {validation}{spendText}";
    }

    private void ResultsGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (suppressSelectionEvents)
        {
            return;
        }

        if (e.RowIndex < 0 || resultsGrid.Columns[e.ColumnIndex].DataPropertyName != nameof(SupplierResultRow.Compare))
        {
            return;
        }

        if (resultsGrid.Rows[e.RowIndex].DataBoundItem is SupplierResultRow row)
        {
            SetCompare(row, row.Compare);
        }
    }

    private void SetCompare(SupplierResultRow row, bool compare)
    {
        if (row is null)
        {
            return;
        }

        if (compare && ComparedRows().Count(comparedRow => comparedRow != row) >= 4)
        {
            row.Compare = false;
            statusLabel.Text = "Comparison supports up to 4 suppliers.";
            currentRows.ResetBindings();
            return;
        }

        row.Compare = compare;
        currentRows.ResetBindings();
        BuildSupplierCards();
        UpdateChartsAndComparison();
    }

    private IEnumerable<SupplierResultRow> ComparedRows()
    {
        return currentRows.Where(row => row.Compare);
    }

    private SupplierResultRow? SelectedTableRow()
    {
        if (suppressSelectionEvents || resultsGrid.IsDisposed || resultsGrid.SelectedRows.Count == 0)
        {
            return null;
        }

        return resultsGrid.SelectedRows[0].DataBoundItem as SupplierResultRow;
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
        card.Padding = new Padding(0);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = AppTheme.CardBackground };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38));

        var accent = new Panel { Dock = DockStyle.Fill, BackColor = KpiAccentColor(key) };
        layout.Controls.Add(accent, 0, 0);
        layout.SetRowSpan(accent, 2);

        var valueLabel = new Label
        {
            Text = "-",
            Dock = DockStyle.Fill,
            Font = AppTheme.TitleFont(18F),
            ForeColor = AppTheme.MainText,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(12, 0, 8, 0)
        };
        var title = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(12, 0, 8, 0),
            AutoEllipsis = true
        };
        layout.Controls.Add(valueLabel, 1, 0);
        layout.Controls.Add(title, 1, 1);
        card.Controls.Add(layout);
        panel.Controls.Add(card, column, 0);
        kpis[key] = valueLabel;
    }

    private static Control ChartCard(Control chart)
    {
        var card = Card();
        card.Margin = new Padding(0, 0, 12, 0);
        card.Padding = new Padding(12);
        card.Controls.Add(chart);
        return card;
    }

    private static Control BreakdownCard(string title, DataGridView grid)
    {
        var card = Card();
        card.Margin = new Padding(0, 0, 12, 0);
        card.Padding = new Padding(12);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = AppTheme.CardBackground };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = AppTheme.TitleFont(10F),
            ForeColor = AppTheme.MainText,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);
        ConfigureBreakdownGrid(grid);
        layout.Controls.Add(grid, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static void ConfigureBreakdownGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = AppTheme.CardBackground;
        grid.BorderStyle = BorderStyle.None;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = AppTheme.CardBorder;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersHeight = 32;
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.PrimaryLight;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.MainText;
        grid.ColumnHeadersDefaultCellStyle.Font = AppTheme.TitleFont(8.5F);
        grid.DefaultCellStyle.Padding = new Padding(4, 1, 4, 1);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 248);

        grid.Columns.Clear();
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(DashboardSpendBreakdownRow.Name),
            HeaderText = "Name",
            FillWeight = 130
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(DashboardSpendBreakdownRow.Spend),
            HeaderText = "Spend",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" },
            FillWeight = 80
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(DashboardSpendBreakdownRow.ItemCount),
            HeaderText = "Items",
            FillWeight = 50
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(DashboardSpendBreakdownRow.ShareOfTotal),
            HeaderText = "%",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" },
            FillWeight = 50
        });
    }

    private static Control MetricPill(DashboardMetric metric)
    {
        var panel = new TableLayoutPanel
        {
            Width = 122,
            Height = 38,
            RowCount = 2,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 8, 8),
            BackColor = AppTheme.PageBackground,
            Padding = new Padding(8, 3, 8, 3)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        panel.Controls.Add(new Label
        {
            Text = metric.Name,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.MutedText,
            Font = AppTheme.BodyFont(7.8F),
            AutoEllipsis = true
        }, 0, 0);
        panel.Controls.Add(new Label
        {
            Text = metric.Value,
            Dock = DockStyle.Fill,
            Font = AppTheme.TitleFont(9.2F),
            ForeColor = AppTheme.MainText,
            AutoEllipsis = true
        }, 0, 1);
        return panel;
    }

    private static void BindBreakdownGrid(DataGridView grid, IReadOnlyList<DashboardSpendBreakdownRow> rows)
    {
        grid.DataSource = rows
            .OrderByDescending(row => row.Spend)
            .Take(12)
            .ToList();
    }

    private static Control InsightCard(string title, Label valueLabel)
    {
        var card = Card();
        card.Margin = new Padding(0, 0, 10, 0);
        card.Padding = new Padding(12, 8, 12, 8);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = AppTheme.CardBackground };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        layout.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.BottomLeft,
            AutoEllipsis = true
        }, 0, 0);
        layout.Controls.Add(valueLabel, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Label InsightValueLabel()
    {
        return new Label
        {
            Text = "-",
            Dock = DockStyle.Fill,
            Font = AppTheme.TitleFont(10.5F),
            ForeColor = AppTheme.MainText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
    }

    private static Color KpiAccentColor(string key)
    {
        return key switch
        {
            "Recommended" => AppTheme.PrimaryDark,
            "Conditional" => AppTheme.Warning,
            "ManualReview" => AppTheme.Error,
            "BestScore" => AppTheme.Primary,
            _ => AppTheme.PrimaryLight
        };
    }

    private static Label SectionTitle(string text)
    {
        return new Label { Text = text, AutoSize = true, Font = AppTheme.TitleFont(13F), ForeColor = AppTheme.MainText, Margin = new Padding(0, 0, 0, 12) };
    }

    private static void AddDetail(TableLayoutPanel panel, string labelText, Label valueLabel)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        row.Controls.Add(new Label { Text = labelText, AutoSize = true, ForeColor = AppTheme.MutedText }, 0, 0);
        row.Controls.Add(valueLabel, 1, 0);
        panel.Controls.Add(row);
    }

    private static void AddInput(TableLayoutPanel panel, string labelText, Control input)
    {
        panel.Controls.Add(new Label { Text = labelText, AutoSize = true, ForeColor = AppTheme.MutedText, Margin = new Padding(0, 8, 0, 3) });
        input.Dock = DockStyle.Top;
        input.Margin = new Padding(0, 0, 0, 8);
        panel.Controls.Add(input);
    }

    private void AddColumn(string propertyName, string header, int width)
    {
        resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = header,
            MinimumWidth = width,
            FillWeight = width,
            ReadOnly = true
        });
    }

    private static Panel Card()
    {
        return new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.CardBackground, Margin = new Padding(0) };
    }

    private static Label DetailValueLabel()
    {
        return new Label { AutoSize = true, ForeColor = AppTheme.MainText, Font = AppTheme.TitleFont(9F) };
    }

    private static Button PrimaryButton(string text)
    {
        var button = new Button();
        StylePrimaryButton(button, text);
        return button;
    }

    private static void StylePrimaryButton(Button button, string text)
    {
        button.Text = text;
        button.Width = 128;
        button.Height = 38;
        button.BackColor = AppTheme.Primary;
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.Margin = new Padding(0, 8, 10, 8);
        button.FlatAppearance.BorderSize = 0;
    }

    private static Button SecondaryButton(string text)
    {
        var button = new Button { Text = text, Width = 128, Height = 36 };
        StyleSecondaryButton(button);
        return button;
    }

    private static void StyleSecondaryButton(Button button)
    {
        button.Width = 124;
        button.Height = 38;
        button.BackColor = AppTheme.CardBackground;
        button.ForeColor = AppTheme.MainText;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = AppTheme.PrimaryLight;
        button.Margin = new Padding(0, 8, 10, 8);
    }

    private static void ConfigureSegmentButton(Button button, string text)
    {
        button.Text = text;
        button.Width = 102;
        button.Height = 34;
        button.FlatStyle = FlatStyle.Flat;
        button.Margin = new Padding(0, 10, 0, 8);
    }

    private static void StyleSegmentButton(Button button, bool selected)
    {
        button.BackColor = selected ? AppTheme.PrimaryDark : AppTheme.PrimaryLight;
        button.ForeColor = selected ? Color.White : AppTheme.MainText;
        button.FlatAppearance.BorderColor = AppTheme.PrimaryDark;
    }

    private static NumericUpDown ConfigurePercentInput(NumericUpDown input)
    {
        input.Minimum = 0;
        input.Maximum = 100;
        input.Increment = 5;
        input.TextAlign = HorizontalAlignment.Right;
        return input;
    }

    private static string SafeCurrency(string? currencyCode)
    {
        return string.IsNullOrWhiteSpace(currencyCode) ? "EUR" : currencyCode.Trim().ToUpperInvariant();
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
}
