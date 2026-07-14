using RasterEditor.WinFormsDemo.Localization;
using RasterEditor.WinFormsDemo.Operations;

namespace RasterEditor.WinFormsDemo.Forms;

public sealed class RotateForm : Form
{
    private readonly NumericUpDown _nudAngle = new() { Minimum = -360, Maximum = 360, DecimalPlaces = 1, Increment = 1, Value = 0 };
    private readonly ComboBox _cbMode = new() { DropDownStyle = ComboBoxStyle.DropDownList };

    public float AngleDegrees => (float)_nudAngle.Value;
    public ResampleMode Mode => (ResampleMode)_cbMode.SelectedItem!;

    public RotateForm()
    {
        Text = "Поворот";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 170);

        _cbMode.Items.Add(ResampleMode.Nearest);
        _cbMode.Items.Add(ResampleMode.Bilinear);
        _cbMode.Items.Add(ResampleMode.Bicubic);
        _cbMode.SelectedIndex = 1;
        _cbMode.FormattingEnabled = true;
        _cbMode.Format += (_, e) =>
        {
            if (e.ListItem is ResampleMode rm)
                e.Value = RussianUi.ResampleModeName(rm);
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(12),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        grid.Controls.Add(new Label { Text = "Угол (градусы):", AutoSize = true }, 0, 0);
        grid.Controls.Add(_nudAngle, 1, 0);
        grid.Controls.Add(new Label { Text = "Интерполяция:", AutoSize = true }, 0, 1);
        grid.Controls.Add(_cbMode, 1, 1);

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

        grid.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 2);
        grid.SetColumnSpan(grid.GetControlFromPosition(0, 2)!, 2);
        grid.Controls.Add(buttons, 0, 3);
        grid.SetColumnSpan(buttons, 2);

        Controls.Add(grid);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}

