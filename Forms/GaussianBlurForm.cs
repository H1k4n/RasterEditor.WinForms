namespace RasterEditor.WinFormsDemo.Forms;

public sealed class GaussianBlurForm : Form
{
    private readonly NumericUpDown _nudRadius = new() { Minimum = 1, Maximum = 50, Value = 3 };
    private readonly NumericUpDown _nudSigma = new() { Minimum = 0.10m, Maximum = 50m, DecimalPlaces = 2, Increment = 0.10m, Value = 1.00m };
    private readonly CheckBox _cbAutoSigma = new() { Text = "Сигма автоматически (радиус/3)", AutoSize = true, Checked = true };

    public int Radius => (int)_nudRadius.Value;
    public float? Sigma => _cbAutoSigma.Checked ? null : (float)_nudSigma.Value;

    public GaussianBlurForm()
    {
        Text = "Размытие по Гауссу";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 200);

        _cbAutoSigma.CheckedChanged += (_, _) => _nudSigma.Enabled = !_cbAutoSigma.Checked;
        _nudSigma.Enabled = false;

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(12),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        grid.Controls.Add(new Label { Text = "Радиус:", AutoSize = true }, 0, 0);
        grid.Controls.Add(_nudRadius, 1, 0);
        grid.Controls.Add(_cbAutoSigma, 0, 1);
        grid.SetColumnSpan(_cbAutoSigma, 2);
        grid.Controls.Add(new Label { Text = "Сигма:", AutoSize = true }, 0, 2);
        grid.Controls.Add(_nudSigma, 1, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };
        var ok = new Button { Text = "Готово", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 90 };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        grid.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 3);
        grid.SetColumnSpan(grid.GetControlFromPosition(0, 3)!, 2);
        grid.Controls.Add(buttons, 0, 4);
        grid.SetColumnSpan(buttons, 2);

        Controls.Add(grid);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}

