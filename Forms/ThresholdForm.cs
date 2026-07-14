namespace RasterEditor.WinFormsDemo.Forms;

public sealed class ThresholdForm : Form
{
    private readonly TrackBar _tb = new() { Minimum = 0, Maximum = 255, TickFrequency = 16, LargeChange = 16, SmallChange = 1, Value = 128 };
    private readonly Label _lb = new() { AutoSize = true };

    public byte Threshold => (byte)_tb.Value;

    public ThresholdForm()
    {
        Text = "Пороговая обработка";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 160);

        _tb.Dock = DockStyle.Fill;
        _tb.ValueChanged += (_, _) => _lb.Text = $"Порог: {_tb.Value}";
        _lb.Text = $"Порог: {_tb.Value}";

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
        };
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        grid.Controls.Add(_lb, 0, 0);
        grid.Controls.Add(_tb, 0, 1);

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
        grid.Controls.Add(buttons, 0, 2);

        Controls.Add(grid);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}

