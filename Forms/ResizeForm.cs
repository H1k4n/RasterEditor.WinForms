using RasterEditor.WinFormsDemo.Localization;
using RasterEditor.WinFormsDemo.Operations;

namespace RasterEditor.WinFormsDemo.Forms;

public sealed class ResizeForm : Form
{
    private readonly NumericUpDown _nudW = new() { Minimum = 1, Maximum = 20000, Value = 800 };
    private readonly NumericUpDown _nudH = new() { Minimum = 1, Maximum = 20000, Value = 600 };
    private readonly CheckBox _cbLockAspect = new() { Text = "Сохранять пропорции", Checked = true, AutoSize = true };
    private readonly ComboBox _cbMode = new() { DropDownStyle = ComboBoxStyle.DropDownList };

    private readonly int _srcW;
    private readonly int _srcH;

    public int TargetWidth => (int)_nudW.Value;
    public int TargetHeight => (int)_nudH.Value;
    public ResampleMode Mode => (ResampleMode)_cbMode.SelectedItem!;

    public ResizeForm(int srcWidth, int srcHeight)
    {
        _srcW = srcWidth;
        _srcH = srcHeight;

        Text = "Изменить размер";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 220);

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

        _nudW.Value = srcWidth;
        _nudH.Value = srcHeight;

        _nudW.ValueChanged += (_, _) => OnSizeChanged(changedW: true);
        _nudH.ValueChanged += (_, _) => OnSizeChanged(changedW: false);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(12),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        grid.Controls.Add(new Label { Text = "Ширина (px):", AutoSize = true }, 0, 0);
        grid.Controls.Add(_nudW, 1, 0);
        grid.Controls.Add(new Label { Text = "Высота (px):", AutoSize = true }, 0, 1);
        grid.Controls.Add(_nudH, 1, 1);
        grid.Controls.Add(_cbLockAspect, 1, 2);
        grid.Controls.Add(new Label { Text = "Интерполяция:", AutoSize = true }, 0, 3);
        grid.Controls.Add(_cbMode, 1, 3);

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

        grid.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 4);
        grid.SetColumnSpan(grid.GetControlFromPosition(0, 4)!, 2);
        grid.Controls.Add(buttons, 0, 5);
        grid.SetColumnSpan(buttons, 2);

        Controls.Add(grid);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void OnSizeChanged(bool changedW)
    {
        if (!_cbLockAspect.Checked) return;
        if (_srcW <= 0 || _srcH <= 0) return;

        if (changedW)
        {
            var w = (int)_nudW.Value;
            var h = (int)Math.Round(w * (_srcH / (double)_srcW));
            h = Math.Max(1, h);
            if (_nudH.Value != h) _nudH.Value = h;
        }
        else
        {
            var h = (int)_nudH.Value;
            var w = (int)Math.Round(h * (_srcW / (double)_srcH));
            w = Math.Max(1, w);
            if (_nudW.Value != w) _nudW.Value = w;
        }
    }
}

