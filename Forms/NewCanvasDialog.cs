namespace RasterEditor.WinFormsDemo.Forms;

public sealed class NewCanvasDialog : Form
{
    private readonly NumericUpDown _nudWidth = new() { Minimum = 1, Maximum = 20000, Value = 800 };
    private readonly NumericUpDown _nudHeight = new() { Minimum = 1, Maximum = 20000, Value = 600 };
    private readonly ComboBox _cbColor = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Panel _colorPreview = new() { Width = 50, Height = 50, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };

    public int CanvasWidth => (int)_nudWidth.Value;
    public int CanvasHeight => (int)_nudHeight.Value;
    public Color BackgroundColor => _colorPreview.BackColor;

    public NewCanvasDialog()
    {
        Text = "Создать новый холст";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 250);

        // Предустановленные цвета
        _cbColor.Items.Add("Белый");
        _cbColor.Items.Add("Чёрный");
        _cbColor.Items.Add("Прозрачный");
        _cbColor.Items.Add("Серый");
        _cbColor.SelectedIndex = 0;
        _cbColor.SelectedIndexChanged += (_, _) => UpdateColorPreview();

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(12),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _nudWidth.Width = 150;
        _nudHeight.Width = 150;
        _cbColor.Width = 150;

        grid.Controls.Add(new Label { Text = "Ширина (px):", AutoSize = true }, 0, 0);
        grid.Controls.Add(_nudWidth, 1, 0);
        grid.Controls.Add(new Label { Text = "Высота (px):", AutoSize = true }, 0, 1);
        grid.Controls.Add(_nudHeight, 1, 1);
        grid.Controls.Add(new Label { Text = "Фон:", AutoSize = true }, 0, 2);
        grid.Controls.Add(_cbColor, 1, 2);
        grid.Controls.Add(new Label { Text = "Предпросмотр:", AutoSize = true }, 0, 3);
        grid.Controls.Add(_colorPreview, 1, 3);

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

        UpdateColorPreview();
    }

    private void UpdateColorPreview()
    {
        _colorPreview.BackColor = _cbColor.SelectedItem?.ToString() switch
        {
            "Белый" => Color.White,
            "Чёрный" => Color.Black,
            "Прозрачный" => Color.FromArgb(0, 0, 0, 0),
            "Серый" => Color.FromArgb(128, 128, 128),
            _ => Color.White
        };
    }
}
