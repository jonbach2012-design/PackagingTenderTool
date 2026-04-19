namespace PackagingTenderTool.App;

internal sealed class StartForm : Form
{
    private readonly Dictionary<string, Button> tenderTypeButtons = [];
    private string selectedTenderType = "Labels";

    public StartForm()
    {
        Text = "Tender Evaluation Dashboard";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(920, 560);
        MinimumSize = new Size(920, 560);
        BackColor = AppTheme.PageBackground;
        Font = AppTheme.BodyFont();

        BuildLayout();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(42),
            BackColor = AppTheme.PageBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CardBackground,
            Padding = new Padding(34),
            BorderStyle = BorderStyle.FixedSingle
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label
        {
            Text = "Tender Evaluation Dashboard",
            AutoSize = true,
            ForeColor = AppTheme.PrimaryDark,
            Font = AppTheme.TitleFont(23F)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text = "Select packaging tender type to enter dashboard overview.",
            AutoSize = true,
            ForeColor = AppTheme.MutedText,
            Font = AppTheme.BodyFont(11F),
            Margin = new Padding(0, 8, 0, 0)
        }, 0, 1);

        layout.Controls.Add(BuildTenderTypeSelector(), 0, 3);

        var continueButton = new Button
        {
            Text = "Continue",
            Width = 148,
            Height = 44,
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.TitleFont(10F),
            Anchor = AnchorStyles.Right
        };
        continueButton.FlatAppearance.BorderSize = 0;
        continueButton.Click += ContinueButton_Click;
        layout.Controls.Add(continueButton, 0, 5);

        card.Controls.Add(layout);
        root.Controls.Add(card, 0, 0);
        Controls.Add(root);
        UpdateTenderTypeSelection();
    }

    private Control BuildTenderTypeSelector()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 150,
            ColumnCount = 3,
            Margin = new Padding(0, 0, 0, 18)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3f));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3f));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3f));

        AddTenderTypeButton(panel, "Labels", 0);
        AddTenderTypeButton(panel, "Trays", 1);
        AddTenderTypeButton(panel, "Flexibles", 2);
        return panel;
    }

    private void AddTenderTypeButton(TableLayoutPanel panel, string tenderType, int column)
    {
        var button = new Button
        {
            Text = tenderType,
            Dock = DockStyle.Fill,
            Margin = new Padding(column == 0 ? 0 : 8, 0, column == 2 ? 0 : 8, 0),
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(18),
            Font = AppTheme.TitleFont(16F),
            Tag = tenderType
        };
        button.Click += (_, _) =>
        {
            selectedTenderType = tenderType;
            UpdateTenderTypeSelection();
        };

        tenderTypeButtons[tenderType] = button;
        panel.Controls.Add(button, column, 0);
    }

    private void UpdateTenderTypeSelection()
    {
        foreach (var (tenderType, button) in tenderTypeButtons)
        {
            var selected = tenderType == selectedTenderType;
            button.BackColor = selected ? AppTheme.PrimaryLight : AppTheme.CardBackground;
            button.ForeColor = selected ? AppTheme.PrimaryDark : AppTheme.MainText;
            button.FlatAppearance.BorderColor = selected ? AppTheme.PrimaryDark : AppTheme.PrimaryLight;
            button.FlatAppearance.BorderSize = selected ? 2 : 1;
        }
    }

    private void ContinueButton_Click(object? sender, EventArgs e)
    {
        var settings = new DashboardSettings
        {
            TenderType = selectedTenderType,
            TenderName = $"{selectedTenderType} Tender"
        };

        Hide();
        using var dashboard = new MainForm(settings);
        dashboard.ShowDialog(this);
        Close();
    }
}
