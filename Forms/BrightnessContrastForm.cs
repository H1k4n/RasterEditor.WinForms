namespace RasterEditor.WinFormsDemo.Forms;

public sealed class BrightnessContrastForm : Form
{
    private readonly TrackBar _tbBrightness = new() { Minimum = -100, Maximum = 100, TickFrequency = 10, LargeChange = 10, SmallChange = 1 };
    private readonly TrackBar _tbContrast = new() { Minimum = -100, Maximum = 100, TickFrequency = 10, LargeChange = 10, SmallChange = 1 };
    private readonly Label _lbBrightness = new() { AutoSize = true };
    private readonly Label _lbContrast = new() { AutoSize = true };

    public float Brightness => _tbBrightness.Value / 100f; // -1..1
    public float Contrast => _tbContrast.Value / 100f;     // -1..1

    public BrightnessContrastForm()
    {
        Text = "Яркость / Контраст";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 260);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(12),
        };

        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _tbBrightness.Dock = DockStyle.Fill;
        _tbContrast.Dock = DockStyle.Fill;
        _tbBrightness.ValueChanged += (_, _) => UpdateLabels();
        _tbContrast.ValueChanged += (_, _) => UpdateLabels();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var ok = new Button { Text = "Готово", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 90 };
        var reset = new Button { Text = "Сброс", Width = 90 };
        reset.Click += (_, _) =>
        {
            _tbBrightness.Value = 0;
            _tbContrast.Value = 0;
        };

        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(reset);

        panel.Controls.Add(_lbBrightness, 0, 0);
        panel.Controls.Add(_tbBrightness, 0, 1);
        panel.Controls.Add(_lbContrast, 0, 2);
        panel.Controls.Add(_tbContrast, 0, 3);
        panel.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 4);
        panel.Controls.Add(buttons, 0, 5);

        Controls.Add(panel);

        AcceptButton = ok;
        CancelButton = cancel;

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        _lbBrightness.Text = $"Яркость: {_tbBrightness.Value}";
        _lbContrast.Text = $"Контраст: {_tbContrast.Value}";
    }
}

