namespace PackagingTenderTool.App;

internal sealed class SettingsDialog : Form
{
    private readonly TextBox tenderNameTextBox = new();
    private readonly TextBox currencyTextBox = new();
    private readonly NumericUpDown recommendedThresholdInput = new();
    private readonly NumericUpDown conditionalThresholdInput = new();
    private readonly CheckBox missingDataManualReviewCheckBox = new();
    private readonly CheckBox normalizeInputValuesCheckBox = new();
    private readonly CheckBox strictModeCheckBox = new();

    public SettingsDialog(DashboardSettings settings)
    {
        Settings = settings;
        Text = "Dashboard settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 390);
        BackColor = AppTheme.PageBackground;
        Font = AppTheme.BodyFont();

        BuildLayout();
        LoadSettings(settings);
    }

    public DashboardSettings Settings { get; }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(18),
            BackColor = AppTheme.PageBackground
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));

        AddField(root, "Tender name", tenderNameTextBox);
        AddField(root, "Currency", currencyTextBox);
        AddField(root, "Recommended threshold", ConfigurePercentInput(recommendedThresholdInput));
        AddField(root, "Conditional threshold", ConfigurePercentInput(conditionalThresholdInput));

        missingDataManualReviewCheckBox.Text = "Missing data = Manual Review";
        normalizeInputValuesCheckBox.Text = "Normalize input values";
        strictModeCheckBox.Text = "Strict mode";
        AddWide(root, missingDataManualReviewCheckBox);
        AddWide(root, normalizeInputValuesCheckBox);
        AddWide(root, strictModeCheckBox);

        var actions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        var okButton = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 92 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 92 };
        okButton.Click += (_, _) => SaveSettings();
        actions.Controls.Add(okButton);
        actions.Controls.Add(cancelButton);
        AddWide(root, actions);

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.Add(root);
    }

    private void LoadSettings(DashboardSettings settings)
    {
        tenderNameTextBox.Text = settings.TenderName;
        currencyTextBox.Text = settings.CurrencyCode;
        recommendedThresholdInput.Value = settings.RecommendedThreshold;
        conditionalThresholdInput.Value = settings.ConditionalThreshold;
        missingDataManualReviewCheckBox.Checked = settings.MissingDataManualReview;
        normalizeInputValuesCheckBox.Checked = settings.NormalizeInputValues;
        strictModeCheckBox.Checked = settings.StrictMode;
    }

    private void SaveSettings()
    {
        if (recommendedThresholdInput.Value < conditionalThresholdInput.Value)
        {
            DialogResult = DialogResult.None;
            MessageBox.Show(
                this,
                "Recommended threshold must be greater than or equal to the conditional threshold.",
                "Invalid thresholds",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Settings.TenderName = string.IsNullOrWhiteSpace(tenderNameTextBox.Text)
            ? Settings.TenderName
            : tenderNameTextBox.Text.Trim();
        Settings.CurrencyCode = string.IsNullOrWhiteSpace(currencyTextBox.Text)
            ? "EUR"
            : currencyTextBox.Text.Trim().ToUpperInvariant();
        Settings.RecommendedThreshold = recommendedThresholdInput.Value;
        Settings.ConditionalThreshold = conditionalThresholdInput.Value;
        Settings.MissingDataManualReview = missingDataManualReviewCheckBox.Checked;
        Settings.NormalizeInputValues = normalizeInputValuesCheckBox.Checked;
        Settings.StrictMode = strictModeCheckBox.Checked;
    }

    private static NumericUpDown ConfigurePercentInput(NumericUpDown input)
    {
        input.Minimum = 0;
        input.Maximum = 100;
        input.Increment = 5;
        input.TextAlign = HorizontalAlignment.Right;
        return input;
    }

    private static void AddField(TableLayoutPanel root, string labelText, Control control)
    {
        var row = root.RowCount++;
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            ForeColor = AppTheme.MainText,
            Margin = new Padding(0, 6, 10, 10)
        }, 0, row);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 2, 0, 10);
        root.Controls.Add(control, 1, row);
    }

    private static void AddWide(TableLayoutPanel root, Control control)
    {
        var row = root.RowCount++;
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 4, 0, 8);
        root.Controls.Add(control, 0, row);
        root.SetColumnSpan(control, 2);
    }
}
