namespace RasterEditor.WinFormsDemo.Forms;

public sealed class HslForm : Form
{
    private readonly TrackBar _tbHue = new() { Minimum = -180, Maximum = 180, TickFrequency = 30, LargeChange = 15, SmallChange = 1, Value = 0 };
    private readonly TrackBar _tbSat = new() { Minimum = -100, Maximum = 100, TickFrequency = 20, LargeChange = 10, SmallChange = 1, Value = 0 };
    private readonly TrackBar _tbLight = new() { Minimum = -100, Maximum = 100, TickFrequency = 20, LargeChange = 10, SmallChange = 1, Value = 0 };

    private readonly Label _lbHue = new() { AutoSize = true };
    private readonly Label _lbSat = new() { AutoSize = true };
    private readonly Label _lbLight = new() { AutoSize = true };

    public float HueShiftDegrees => _tbHue.Value;
    public float SaturationDelta => _tbSat.Value / 100f;
    public float LightnessDelta => _tbLight.Value / 100f;

    public HslForm()
    {
        Text = "Коррекция цвета (HSL)";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 320);

        _tbHue.Dock = DockStyle.Fill;
        _tbSat.Dock = DockStyle.Fill;
        _tbLight.Dock = DockStyle.Fill;

        _tbHue.ValueChanged += (_, _) => UpdateLabels();
        _tbSat.ValueChanged += (_, _) => UpdateLabels();
        _tbLight.ValueChanged += (_, _) => UpdateLabels();

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(12),
        };

        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        grid.Controls.Add(_lbHue, 0, 0);
        grid.Controls.Add(_tbHue, 0, 1);
        grid.Controls.Add(_lbSat, 0, 2);
        grid.Controls.Add(_tbSat, 0, 3);
        grid.Controls.Add(_lbLight, 0, 4);
        grid.Controls.Add(_tbLight, 0, 5);

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
            _tbHue.Value = 0;
            _tbSat.Value = 0;
            _tbLight.Value = 0;
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(reset);

        grid.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 6);
        grid.Controls.Add(buttons, 0, 7);

        Controls.Add(grid);
        AcceptButton = ok;
        CancelButton = cancel;

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        _lbHue.Text = $"Оттенок (сдвиг): {_tbHue.Value}°";
        _lbSat.Text = $"Насыщенность (сдвиг): {_tbSat.Value}%";
        _lbLight.Text = $"Яркость (сдвиг): {_tbLight.Value}%";
    }
}

