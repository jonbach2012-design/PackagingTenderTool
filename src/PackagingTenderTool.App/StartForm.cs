namespace PackagingTenderTool.App;

internal sealed class StartForm : Form
{
    private readonly ComboBox tenderTypeComboBox = new();

    public StartForm()
    {
        Text = "Tender Evaluation Dashboard";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 460);
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

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
            Text = "Choose the packaging tender context before entering the evaluation workspace.",
            AutoSize = true,
            ForeColor = AppTheme.MutedText,
            Font = AppTheme.BodyFont(11F),
            Margin = new Padding(0, 8, 0, 0)
        }, 0, 1);

        layout.Controls.Add(new Label
        {
            Text = "Tender type",
            AutoSize = true,
            ForeColor = AppTheme.MainText,
            Font = AppTheme.TitleFont(11F)
        }, 0, 3);

        tenderTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        tenderTypeComboBox.Items.AddRange(["Labels", "Trays", "Flexibles"]);
        tenderTypeComboBox.SelectedIndex = 0;
        tenderTypeComboBox.Width = 260;
        tenderTypeComboBox.Margin = new Padding(0, 8, 0, 0);
        layout.Controls.Add(tenderTypeComboBox, 0, 4);

        var continueButton = new Button
        {
            Text = "Continue",
            Width = 132,
            Height = 40,
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.TitleFont(10F),
            Anchor = AnchorStyles.Right
        };
        continueButton.FlatAppearance.BorderSize = 0;
        continueButton.Click += ContinueButton_Click;
        layout.Controls.Add(continueButton, 0, 6);

        card.Controls.Add(layout);
        root.Controls.Add(card, 0, 0);
        Controls.Add(root);
    }

    private void ContinueButton_Click(object? sender, EventArgs e)
    {
        var settings = new DashboardSettings
        {
            TenderType = tenderTypeComboBox.SelectedItem?.ToString() ?? "Labels",
            TenderName = $"{tenderTypeComboBox.SelectedItem} Tender"
        };

        Hide();
        using var dashboard = new MainForm(settings);
        dashboard.ShowDialog(this);
        Close();
    }
}
