using RasterEditor.WinFormsDemo.Core;

namespace RasterEditor.WinFormsDemo.Forms;

public partial class PaletteManagerForm : Form
{
    private List<ColorPalette> _palettes;
    private ColorPalette? _currentPalette;
    private Color _selectedColor = Color.Black;
    private ComboBox? _comboPalettes;
    private Panel? _panelColors;

    public ColorPalette? SelectedPalette => _currentPalette;

    public PaletteManagerForm()
    {
        InitializeComponent();
        _palettes = ColorPalette.LoadPalettes();
        LoadPalettes();
    }

    private void InitializeComponent()
    {
        var labelPalettes = new Label { Text = "Палитры:", AutoSize = true, Location = new Point(10, 10) };
        _comboPalettes = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(10, 30), Width = 300 };

        var buttonNew = new Button { Text = "Новая", Location = new Point(320, 28), Width = 90 };
        var buttonDelete = new Button { Text = "Удалить", Location = new Point(418, 28), Width = 90 };

        var labelColors = new Label { Text = "Цвета палитры:", AutoSize = true, Location = new Point(10, 64) };
        _panelColors = new Panel { Location = new Point(10, 86), Width = 498, Height = 200, BorderStyle = BorderStyle.FixedSingle, AutoScroll = true };

        var buttonAddColor = new Button { Text = "Добавить цвет", Location = new Point(10, 296), Width = 244 };
        var buttonRemoveColor = new Button { Text = "Удалить цвет", Location = new Point(264, 296), Width = 244 };
        var buttonSave = new Button { Text = "Сохранить", Location = new Point(10, 334), Width = 244 };
        var buttonCancel = new Button { Text = "Отмена", Location = new Point(264, 334), Width = 244, DialogResult = DialogResult.Cancel };

        Controls.Add(labelPalettes);
        Controls.Add(_comboPalettes);
        Controls.Add(buttonNew);
        Controls.Add(buttonDelete);
        Controls.Add(labelColors);
        Controls.Add(_panelColors);
        Controls.Add(buttonAddColor);
        Controls.Add(buttonRemoveColor);
        Controls.Add(buttonSave);
        Controls.Add(buttonCancel);

        Text = "Менеджер палитр";
        Width = 540;
        Height = 440;
        StartPosition = FormStartPosition.CenterParent;
        AcceptButton = buttonSave;
        CancelButton = buttonCancel;

        _comboPalettes.SelectedIndexChanged += (_, _) => SelectPalette(_comboPalettes.SelectedIndex);
        buttonNew.Click += (_, _) => CreateNewPalette();
        buttonDelete.Click += (_, _) => DeletePalette(_comboPalettes.SelectedIndex);
        buttonAddColor.Click += (_, _) => AddColorToPalette();
        buttonRemoveColor.Click += (_, _) => RemoveColorFromPalette();
        buttonSave.Click += (_, _) => SaveAndClose();
    }

    private void SaveAndClose()
    {
        try
        {
            foreach (var p in _palettes)
                p.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка сохранения палитр", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadPalettes()
    {
        if (_comboPalettes == null) return;

        _comboPalettes.Items.Clear();
        foreach (var palette in _palettes)
            _comboPalettes.Items.Add(palette.Name);

        if (_comboPalettes.Items.Count > 0)
            _comboPalettes.SelectedIndex = 0;
    }

    private void SelectPalette(int index)
    {
        if (index < 0 || index >= _palettes.Count) return;

        _currentPalette = _palettes[index];
        RefreshColorPanel();
    }

    private void RefreshColorPanel()
    {
        if (_panelColors == null || _currentPalette == null) return;

        _panelColors.Controls.Clear();
        int x = 5, y = 5;

        foreach (var color in _currentPalette.Colors)
        {
            var colorSwatch = new Panel
            {
                BackColor = color,
                Width = 30,
                Height = 30,
                Location = new Point(x, y),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
                Tag = color
            };

            colorSwatch.Click += (_, _) => _selectedColor = color;
            _panelColors.Controls.Add(colorSwatch);

            x += 35;
            if (x > _panelColors.Width - 40)
            {
                x = 5;
                y += 35;
            }
        }
    }

    private void CreateNewPalette()
    {
        using var dlg = new TextInputForm("Имя новой палитры:", "");
        if (dlg.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.InputValue))
            return;

        var name = dlg.InputValue.Trim();
        if (_palettes.Exists(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "Палитра с таким именем уже есть.", "Палитры", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var palette = new ColorPalette(name, new List<Color> { Color.Black });
        _palettes.Add(palette);
        LoadPalettes();
        _comboPalettes!.SelectedIndex = _palettes.Count - 1;
        SelectPalette(_palettes.Count - 1);
    }

    private void DeletePalette(int index)
    {
        if (index < 0 || index >= _palettes.Count) return;
        if (_palettes.Count <= 1)
        {
            MessageBox.Show(this, "Нельзя удалить последнюю палитру", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var removed = _palettes[index];
        ColorPalette.TryDeletePaletteFile(removed.Name);
        _palettes.RemoveAt(index);
        LoadPalettes();
    }

    private void AddColorToPalette()
    {
        if (_currentPalette == null) return;

        using var dlg = new ColorDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        if (_currentPalette.Colors.Any(c => c.ToArgb() == dlg.Color.ToArgb()))
            return;

        _currentPalette.Colors.Add(dlg.Color);
        RefreshColorPanel();
    }

    private void RemoveColorFromPalette()
    {
        if (_currentPalette == null || _currentPalette.Colors.Count <= 1) return;

        _currentPalette.Colors.RemoveAll(c => c.ToArgb() == _selectedColor.ToArgb());
        RefreshColorPanel();
    }
}
