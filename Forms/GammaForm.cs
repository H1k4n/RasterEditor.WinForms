namespace RasterEditor.WinFormsDemo.Forms;

public sealed class GammaForm : Form
{
    private readonly NumericUpDown _nudGamma = new() { Minimum = 0.10m, Maximum = 10.0m, DecimalPlaces = 2, Increment = 0.05m, Value = 1.00m };

    public float Gamma => (float)_nudGamma.Value;

    public GammaForm()
    {
        Text = "Гамма-коррекция";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(400, 140);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        grid.Controls.Add(new Label { Text = "Гамма:", AutoSize = true }, 0, 0);
        grid.Controls.Add(_nudGamma, 1, 0);

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

        grid.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 1);
        grid.SetColumnSpan(grid.GetControlFromPosition(0, 1)!, 2);
        grid.Controls.Add(buttons, 0, 2);
        grid.SetColumnSpan(buttons, 2);

        Controls.Add(grid);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}

