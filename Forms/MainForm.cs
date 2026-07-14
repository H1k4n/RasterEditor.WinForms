using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using RasterEditor.WinFormsDemo.Controls;
using RasterEditor.WinFormsDemo.Core;
using RasterEditor.WinFormsDemo.History;
using RasterEditor.WinFormsDemo.Localization;
using RasterEditor.WinFormsDemo.Operations;
using RasterEditor.WinFormsDemo.Services;

namespace RasterEditor.WinFormsDemo.Forms;

public sealed class MainForm : Form
{
    private enum EditorTool
    {
        Brush,
        Eraser,
        Eyedropper,
        Bucket,
        Hand,
        Selection
    }

    private enum AreaSelectionShape
    {
        Rectangle,
        Ellipse,
        Freehand
    }

    private const int CanvasViewportPadding = 24;
    /// <summary>Ширина основных контролов в правой панели (русские подписи длиннее английских).</summary>
    private const int SidebarInnerWidth = 228;

    private readonly ImageDocument _doc = new();
    private readonly UndoRedoStack<EditorStateSnapshot> _history = new(capacity: 25);
    private readonly PaintingEngine _painting = new();
    private LayerStack? _layers;

    private readonly MenuStrip _menu = new();
    private readonly ToolStrip _toolbar = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _statusLeft = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _statusRight = new() { TextAlign = ContentAlignment.MiddleRight };

    private readonly SplitContainer _mainSplit = new() { Dock = DockStyle.None, FixedPanel = FixedPanel.Panel2, IsSplitterFixed = false };
    private readonly Panel _scrollPanel = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(30, 30, 30) };
    private readonly PictureBox _picture = new() { SizeMode = PictureBoxSizeMode.Normal };
    private readonly Panel _layersPanel = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(236, 236, 236), Padding = new Padding(8), AutoScroll = true };
    private readonly GroupBox _layerEffectGroup = new() { Text = "Параметры слоя", Dock = DockStyle.Top, Height = 228 };
    private readonly ComboBox _layerModeCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TrackBar _layerOpacityTrack = new() { Minimum = 0, Maximum = 100, TickStyle = TickStyle.None, SmallChange = 1, LargeChange = 10 };
    private readonly Label _layerOpacityLabel = new() { AutoSize = true, Text = "100%" };
    private readonly FlowLayoutPanel _layerButtons = new() { Dock = DockStyle.Top, Height = 34, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
    private readonly ListBox _layersList = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly CheckBox _layerVisibleCheck = new() { Text = "Видимый", AutoSize = true };
    private readonly GroupBox _paintGroup = new() { Text = "Рисование", Dock = DockStyle.Bottom, Height = 110 };
    private readonly Label _paintToolLabel = new() { AutoSize = true, Text = "Инструмент: кисть" };
    private readonly Label _paintSizeLabel = new() { AutoSize = true, Text = "Кисть: 12 px" };
    private readonly Panel _paintColorSwatch = new() { Width = 22, Height = 22, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Black };
    private readonly FlowLayoutPanel _palettePanel = new() { AutoSize = false, Width = SidebarInnerWidth, Height = 48 };
    private readonly BufferedPanel _hueStripPanel = new() { Width = 22, Height = 120, BorderStyle = BorderStyle.FixedSingle, TabStop = false };
    private readonly BufferedPanel _svSquarePanel = new() { Width = 192, Height = 120, BorderStyle = BorderStyle.FixedSingle, TabStop = false };
    private Bitmap? _svGradientBitmap;
    private float _pickerHue;
    private float _pickerSat = 1f;
    private float _pickerVal = 1f;
    private readonly ComboBox _paletteCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = SidebarInnerWidth };
    private readonly Button _paletteAddColorButton = new() { Width = SidebarInnerWidth, Height = 24, Text = "Добавить цвет" };
    private readonly Button _paletteRemoveColorButton = new() { Width = SidebarInnerWidth, Height = 24, Text = "Убрать цвет" };
    private readonly Button _paletteManageButton = new() { Width = SidebarInnerWidth, Height = 26, Text = "Палитры…" };
    private readonly TrackBar _stabilizerTrack = new() { Minimum = 0, Maximum = 100, TickStyle = TickStyle.None, SmallChange = 5, LargeChange = 10 };
    private readonly Label _stabilizerValueLabel = new() { AutoSize = true, Text = "55%" };
    private readonly TrackBar _brushOpacityTrack = new() { Minimum = 1, Maximum = 100, TickStyle = TickStyle.None, SmallChange = 5, LargeChange = 10, Value = 100 };
    private readonly Label _brushOpacityValueLabel = new() { AutoSize = true, Text = "100%" };
    private bool _updatingLayerUi;

    private ToolStripButton? _tbSave;
    private ToolStripButton? _tbUndo;
    private ToolStripButton? _tbRedo;
    private ToolStripButton? _tbCrop;
    private ToolStripButton? _tbBrush;
    private ToolStripButton? _tbEraser;
    private ToolStripButton? _tbEyedropper;
    private ToolStripButton? _tbBucket;
    private ToolStripButton? _tbHand;
    private ToolStripButton? _tbSelect;
    private ToolStripComboBox? _tbAreaShape;
    private ToolStripComboBox? _tbBrushSize;

    private bool _isBusy;

    private float _zoom = 1f;
    private bool _cropMode;
    private bool _isSelecting;
    private Point _selStartImg;
    private Rectangle _selectionImg;
    private GraphicsPath? _areaSelectionPath;
    private bool _areaSelecting;
    private AreaSelectionShape _areaSelectionShape = AreaSelectionShape.Rectangle;
    private readonly List<Point> _lassoPoints = new();
    private EditorTool _currentTool = EditorTool.Brush;
    private Color _selectedColor = Color.Black;
    private int _brushSize = 12;
    private bool _isDrawingBrush;
    private PointF _lastBrushRawImg;
    private PointF _lastSmoothedBrushPoint;
    private bool _isPanning;
    private Point _panStartMouse;
    private Point _panStartScroll;
    private ColorPalette? _currentPalette;
    
    private float _brushStabilization = 0.45f;
    private float _brushOpacity = 1f;
    private readonly List<ColorPalette> _palettes = new();
    private Color _canvasBackgroundColor = Color.White;
    // Вставка из буфера: превью до ЛКМ (сливается в активный слой). Esc — отмена.
    private Bitmap? _pastePlacementBitmap;
    private int _pastePlacementOx;
    private int _pastePlacementOy;

    // Menu items we need to enable/disable.
    private ToolStripMenuItem? _miSave;
    private ToolStripMenuItem? _miSaveAs;
    private ToolStripMenuItem? _miUndo;
    private ToolStripMenuItem? _miRedo;
    private ToolStripMenuItem? _miCopy;
    private ToolStripMenuItem? _miPaste;
    private ToolStripMenuItem? _miCropMode;
    private ToolStripMenuItem? _miApplyCrop;
    private ToolStripMenuItem? _miRotate90;
    private ToolStripMenuItem? _miRotateAny;
    private ToolStripMenuItem? _miFlipH;
    private ToolStripMenuItem? _miFlipV;
    private ToolStripMenuItem? _miBrightnessContrast;
    private ToolStripMenuItem? _miGamma;
    private ToolStripMenuItem? _miThreshold;
    private ToolStripMenuItem? _miHsl;
    private ToolStripMenuItem? _miResize;
    private ToolStripMenuItem? _miFiltersMenu;
    private ToolStripMenuItem? _miGaussian;
    private ToolStripMenuItem? _miMedian;
    private ToolStripMenuItem? _miUnsharp;
    private ToolStripMenuItem? _miLaplace;
    private ToolStripMenuItem? _miZoomIn;
    private ToolStripMenuItem? _miZoomOut;
    private ToolStripMenuItem? _miZoom100;
    private ToolStripMenuItem? _miZoomFit;
    private ToolStripMenuItem? _miLayersMenu;
    private ToolStripMenuItem? _miLayerAdd;
    private ToolStripMenuItem? _miLayerDuplicate;
    private ToolStripMenuItem? _miLayerDelete;
    private ToolStripMenuItem? _miLayerMoveUp;
    private ToolStripMenuItem? _miLayerMoveDown;
    private ToolStripMenuItem? _miLayerVisible;
    private ToolStripMenuItem? _miLayerList;
    private ToolStripMenuItem? _miLayerBlendMode;
    private ToolStripMenuItem? _miLayerOpacity;

    public MainForm()
    {
        Text = "Растровый редактор";
        Width = 1200;
        Height = 800;
        MinimumSize = new Size(1024, 600);
        StartPosition = FormStartPosition.CenterScreen;

        KeyPreview = true;
        KeyDown += MainForm_KeyDown;

        BuildMenu();
        BuildToolbar();
        BuildCanvas();
        BuildStatusbar();

        this.Controls.Add(_mainSplit); // Центр (SplitContainer)
        this.Controls.Add(_toolbar);   // Верхняя панель
        this.Controls.Add(_status);    // Нижняя строка состояния
        this.Controls.Add(_menu);

        Layout += (_, _) => LayoutMainArea();

        UpdateUiState();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _svGradientBitmap?.Dispose();
            _areaSelectionPath?.Dispose();
            _pastePlacementBitmap?.Dispose();
            _layers?.Dispose();
            _history.Dispose();
            _doc.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildMenu()
    {
        var file = new ToolStripMenuItem("Файл");
        var edit = new ToolStripMenuItem("Правка");
        var image = new ToolStripMenuItem("Изображение");
        _miLayersMenu = new ToolStripMenuItem("Слои");
        var view = new ToolStripMenuItem("Вид");
        _miFiltersMenu = new ToolStripMenuItem("Фильтры");

        var miNew = new ToolStripMenuItem("Создать холст…", null, (_, _) => CreateNewCanvas()) { ShortcutKeys = Keys.Control | Keys.N };
        var miOpen = new ToolStripMenuItem("Открыть…", null, (_, _) => OpenImage()) { ShortcutKeys = Keys.Control | Keys.O };
        _miSave = new ToolStripMenuItem("Сохранить", null, (_, _) => SaveImage()) { ShortcutKeys = Keys.Control | Keys.S };
        _miSaveAs = new ToolStripMenuItem("Сохранить как…", null, (_, _) => SaveImageAs());
        var miExit = new ToolStripMenuItem("Выход", null, (_, _) => Close());

        file.DropDownItems.Add(miNew);
        file.DropDownItems.Add(miOpen);
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(_miSave);
        file.DropDownItems.Add(_miSaveAs);
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(miExit);

        _miUndo = new ToolStripMenuItem("Отменить", null, (_, _) => Undo()) { ShortcutKeys = Keys.Control | Keys.Z };
        _miRedo = new ToolStripMenuItem("Повторить", null, (_, _) => Redo()) { ShortcutKeys = Keys.Control | Keys.Y };
        edit.DropDownItems.Add(_miUndo);
        edit.DropDownItems.Add(_miRedo);
        edit.DropDownItems.Add(new ToolStripSeparator());
        _miCopy = new ToolStripMenuItem("Копировать выделение", null, (_, _) => CopySelectionToClipboard()) { ShortcutKeys = Keys.Control | Keys.C };
        _miPaste = new ToolStripMenuItem("Вставить в активный слой", null, (_, _) => PasteFromClipboard()) { ShortcutKeys = Keys.Control | Keys.V };
        edit.DropDownItems.Add(_miCopy);
        edit.DropDownItems.Add(_miPaste);

        _miCropMode = new ToolStripMenuItem("Режим обрезки (выделение)", null, (_, _) => ToggleCropMode())
        {
            CheckOnClick = true
        };
        _miApplyCrop = new ToolStripMenuItem("Обрезать по выделению", null, (_, _) => ApplyCrop());
        image.DropDownItems.Add(_miCropMode);
        image.DropDownItems.Add(_miApplyCrop);
        image.DropDownItems.Add(new ToolStripSeparator());

        _miResize = new ToolStripMenuItem("Изменить размер…", null, (_, _) => ShowResizeDialog());
        _miRotateAny = new ToolStripMenuItem("Поворот…", null, (_, _) => ShowRotateDialog());
        _miRotate90 = new ToolStripMenuItem("Поворот 90° вправо", null, (_, _) => ApplyOperation(src => ImageOperations.RotateFlip(src, RotateFlipType.Rotate90FlipNone)));
        _miFlipH = new ToolStripMenuItem("Отразить по горизонтали", null, (_, _) => ApplyOperation(src => ImageOperations.RotateFlip(src, RotateFlipType.RotateNoneFlipX)));
        _miFlipV = new ToolStripMenuItem("Отразить по вертикали", null, (_, _) => ApplyOperation(src => ImageOperations.RotateFlip(src, RotateFlipType.RotateNoneFlipY)));
        image.DropDownItems.Add(_miResize);
        image.DropDownItems.Add(_miRotateAny);
        image.DropDownItems.Add(_miRotate90);
        image.DropDownItems.Add(_miFlipH);
        image.DropDownItems.Add(_miFlipV);
        image.DropDownItems.Add(new ToolStripSeparator());

        _miBrightnessContrast = new ToolStripMenuItem("Яркость/Контраст…", null, (_, _) => ShowBrightnessContrastDialog());
        image.DropDownItems.Add(_miBrightnessContrast);

        _miGamma = new ToolStripMenuItem("Гамма…", null, (_, _) => ShowGammaDialog());
        _miThreshold = new ToolStripMenuItem("Порог…", null, (_, _) => ShowThresholdDialog());
        _miHsl = new ToolStripMenuItem("Коррекция HSL…", null, (_, _) => ShowHslDialog());
        image.DropDownItems.Add(_miGamma);
        image.DropDownItems.Add(_miThreshold);
        image.DropDownItems.Add(_miHsl);

        _miZoomIn = new ToolStripMenuItem("Увеличить", null, (_, _) => SetZoom(_zoom * 1.25f)) { ShortcutKeys = Keys.Control | Keys.Oemplus };
        _miZoomOut = new ToolStripMenuItem("Уменьшить", null, (_, _) => SetZoom(_zoom / 1.25f)) { ShortcutKeys = Keys.Control | Keys.OemMinus };
        _miZoom100 = new ToolStripMenuItem("100%", null, (_, _) => SetZoom(1f)) { ShortcutKeys = Keys.Control | Keys.D0 };
        _miZoomFit = new ToolStripMenuItem("Вписать", null, (_, _) => ZoomToFit()) { ShortcutKeys = Keys.Control | Keys.F };
        view.DropDownItems.Add(_miZoomIn);
        view.DropDownItems.Add(_miZoomOut);
        view.DropDownItems.Add(new ToolStripSeparator());
        view.DropDownItems.Add(_miZoom100);
        view.DropDownItems.Add(_miZoomFit);

        _miLayerAdd = new ToolStripMenuItem("Новый прозрачный слой", null, (_, _) => AddLayer());
        _miLayerDuplicate = new ToolStripMenuItem("Дублировать активный", null, (_, _) => DuplicateActiveLayer());
        _miLayerDelete = new ToolStripMenuItem("Удалить активный", null, (_, _) => DeleteActiveLayer());
        _miLayerMoveUp = new ToolStripMenuItem("Сдвинуть выше", null, (_, _) => MoveActiveLayerUp());
        _miLayerMoveDown = new ToolStripMenuItem("Сдвинуть ниже", null, (_, _) => MoveActiveLayerDown());
        _miLayerVisible = new ToolStripMenuItem("Активный слой видим", null, (_, _) => ToggleActiveLayerVisibility()) { CheckOnClick = true };
        _miLayerList = new ToolStripMenuItem("Выбрать активный слой");
        _miLayerBlendMode = new ToolStripMenuItem("Режим наложения");
        _miLayerOpacity = new ToolStripMenuItem("Непрозрачность");
        _miLayersMenu.DropDownItems.Add(_miLayerAdd);
        _miLayersMenu.DropDownItems.Add(_miLayerDuplicate);
        _miLayersMenu.DropDownItems.Add(_miLayerDelete);
        _miLayersMenu.DropDownItems.Add(new ToolStripSeparator());
        _miLayersMenu.DropDownItems.Add(_miLayerMoveUp);
        _miLayersMenu.DropDownItems.Add(_miLayerMoveDown);
        _miLayersMenu.DropDownItems.Add(new ToolStripSeparator());
        _miLayersMenu.DropDownItems.Add(_miLayerVisible);
        _miLayersMenu.DropDownItems.Add(_miLayerList);
        _miLayersMenu.DropDownItems.Add(new ToolStripSeparator());
        _miLayersMenu.DropDownItems.Add(_miLayerBlendMode);
        _miLayersMenu.DropDownItems.Add(_miLayerOpacity);

        _menu.Items.Add(file);
        _menu.Items.Add(edit);
        _menu.Items.Add(image);
        _menu.Items.Add(_miLayersMenu);
        _menu.Items.Add(_miFiltersMenu);
        _menu.Items.Add(view);
        MainMenuStrip = _menu;
        Controls.Add(_menu);

        BuildFiltersMenu();
        RebuildLayerMenus();
    }

    private void BuildFiltersMenu()
    {
        _miGaussian = new ToolStripMenuItem("Размытие по Гауссу…", null, (_, _) => ShowGaussianDialog());
        _miMedian = new ToolStripMenuItem("Медианный фильтр…", null, (_, _) => ShowMedianDialog());
        _miUnsharp = new ToolStripMenuItem("Резкость…", null, (_, _) => ShowUnsharpDialog());
        _miLaplace = new ToolStripMenuItem("Лаплас (контуры)", null, (_, _) => _ = ApplyOperationAsync(src => Filters.Laplace(src)));

        _miFiltersMenu!.DropDownItems.Add(_miGaussian);
        _miFiltersMenu.DropDownItems.Add(_miMedian);
        _miFiltersMenu.DropDownItems.Add(new ToolStripSeparator());
        _miFiltersMenu.DropDownItems.Add(_miUnsharp);
        _miFiltersMenu.DropDownItems.Add(_miLaplace);
    }

    private void BuildToolbar()
    {
        _toolbar.GripStyle = ToolStripGripStyle.Hidden;
        _toolbar.Dock = DockStyle.Top;

        var bOpen = new ToolStripButton("Открыть") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        bOpen.Click += (_, _) => OpenImage();

        _tbSave = new ToolStripButton("Сохранить") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _tbSave.Click += (_, _) => SaveImage();

        _tbUndo = new ToolStripButton("Отменить") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _tbUndo.Click += (_, _) => Undo();

        _tbRedo = new ToolStripButton("Повторить") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _tbRedo.Click += (_, _) => Redo();

        _tbCrop = new ToolStripButton("Обрезка") { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true };
        _tbCrop.CheckedChanged += (_, _) =>
        {
            if (_tbCrop is null) return;
            _miCropMode!.Checked = _tbCrop.Checked;
            ToggleCropMode(force: _tbCrop.Checked);
        };

        _toolbar.Items.Add(bOpen);
        _toolbar.Items.Add(_tbSave);
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(_tbUndo);
        _toolbar.Items.Add(_tbRedo);
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(_tbCrop);
        _toolbar.Items.Add(new ToolStripSeparator());

        _tbBrush = new ToolStripButton("Кисть") { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true };
        _tbEraser = new ToolStripButton("Ластик") { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true };
        _tbEyedropper = new ToolStripButton("Пипетка") { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true };
        _tbBucket = new ToolStripButton("Ведро") { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true };
        _tbHand = new ToolStripButton("Рука") { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true };
        _tbSelect = new ToolStripButton("Выделение") { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true };
        _tbAreaShape = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        _tbAreaShape.Items.AddRange(new object[] { "Прямоугольник", "Овал", "Лассо" });
        _tbAreaShape.SelectedIndex = 0;
        _tbBrushSize = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 76 };
        _tbBrushSize.Items.AddRange(new object[] { "2", "4", "8", "12", "16", "24", "32", "48", "64" });
        _tbBrushSize.SelectedItem = _brushSize.ToString();

        _tbBrush.Click += (_, _) => SetTool(EditorTool.Brush);
        _tbEraser.Click += (_, _) => SetTool(EditorTool.Eraser);
        _tbEyedropper.Click += (_, _) => SetTool(EditorTool.Eyedropper);
        _tbBucket.Click += (_, _) => SetTool(EditorTool.Bucket);
        _tbHand.Click += (_, _) => SetTool(EditorTool.Hand);
        _tbSelect!.Click += (_, _) => SetTool(EditorTool.Selection);
        _tbAreaShape!.SelectedIndexChanged += (_, _) => SyncAreaShapeFromToolbar();
        _tbBrushSize.SelectedIndexChanged += (_, _) =>
        {
            if (int.TryParse(_tbBrushSize.SelectedItem?.ToString(), out var size))
            {
                _brushSize = Math.Clamp(size, 1, 256);
                _paintSizeLabel.Text = $"Кисть: {_brushSize} px";
            }
        };
        _toolbar.Items.Add(_tbBrush);
        _toolbar.Items.Add(_tbEraser);
        _toolbar.Items.Add(_tbEyedropper);
        _toolbar.Items.Add(_tbBucket);
        _toolbar.Items.Add(_tbHand);
        _toolbar.Items.Add(_tbSelect);
        _toolbar.Items.Add(new ToolStripLabel("Форма:"));
        _toolbar.Items.Add(_tbAreaShape);
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(new ToolStripLabel("Размер"));
        _toolbar.Items.Add(_tbBrushSize);

        Controls.Add(_toolbar);
        SetTool(_currentTool);
        SyncAreaShapeFromToolbar();
        LoadPalettes();
    }

    private void BuildCanvas()
    {
        _picture.BackColor = Color.FromArgb(45, 45, 45);
        _picture.TabStop = true;
        _picture.Paint += Picture_Paint;
        _picture.MouseDown += Picture_MouseDown;
        _picture.MouseMove += Picture_MouseMove;
        _picture.MouseUp += Picture_MouseUp;

        _scrollPanel.Controls.Add(_picture);
        _scrollPanel.Padding = new Padding(CanvasViewportPadding);
        BuildLayersSidebar();

        Controls.Add(_mainSplit);

        _mainSplit.Panel1MinSize = 0;
        _mainSplit.Panel2MinSize = 0;

        _mainSplit.Panel1.Controls.Add(_scrollPanel);
        _mainSplit.Panel2.Controls.Add(_layersPanel);

        _scrollPanel.MouseWheel += Canvas_MouseWheel;
        _picture.MouseWheel += Canvas_MouseWheel;
        _scrollPanel.MouseEnter += (_, _) => _scrollPanel.Focus();
        _picture.MouseEnter += (_, _) => _picture.Focus();
        _scrollPanel.Resize += (_, _) => UpdateCanvasPlacement();
    }

    private void LayoutMainArea()
    {
        int top = _menu.Height + _toolbar.Height;
        int bottom = _status.Height;
        int width = Math.Max(1, ClientSize.Width);
        int height = Math.Max(1, ClientSize.Height - top - bottom);
        _mainSplit.Bounds = new Rectangle(0, top, width, height);

        int preferredPanel1Min = 950;
        int preferredPanel2Min = 264;
        int availableWidth = Math.Max(1, _mainSplit.Width);

        // Keep constraints always valid, even when window is very narrow.
        int effectivePanel1Min = Math.Min(preferredPanel1Min, Math.Max(0, availableWidth - 1));
        int effectivePanel2Min = Math.Min(preferredPanel2Min, Math.Max(0, availableWidth - effectivePanel1Min));
        int maxPanel1 = Math.Max(0, availableWidth - effectivePanel2Min);
        if (effectivePanel1Min > maxPanel1)
            effectivePanel1Min = maxPanel1;

        _mainSplit.Panel1MinSize = effectivePanel1Min;
        _mainSplit.Panel2MinSize = effectivePanel2Min;

        int desired = _mainSplit.SplitterDistance > 0 ? _mainSplit.SplitterDistance : (availableWidth - 280);
        _mainSplit.SplitterDistance = Math.Clamp(desired, effectivePanel1Min, maxPanel1);
        UpdateCanvasPlacement();
    }

    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (!_doc.HasImage) return;

        float factor = e.Delta > 0 ? 1.1f : 1f / 1.1f;
        SetZoom(_zoom * factor);

        if (e is HandledMouseEventArgs handled)
            handled.Handled = true;
    }

    private void BuildLayersSidebar()
    {
        const int x = 10;
        const int innerW = SidebarInnerWidth;

        var modeLabel = new Label { Text = "Режим наложения", AutoSize = true, Location = new Point(x, 22) };
        _layerModeCombo.Location = new Point(x, 40);
        _layerModeCombo.Width = innerW;
        _layerModeCombo.Height = 26;
        _layerModeCombo.Items.AddRange(Enum.GetValues<BlendMode>().Cast<object>().ToArray());
        _layerModeCombo.FormattingEnabled = true;
        _layerModeCombo.Format += (_, e) =>
        {
            if (e.ListItem is BlendMode bm)
                e.Value = RussianUi.BlendModeName(bm);
        };
        _layerModeCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingLayerUi) return;
            if (_layerModeCombo.SelectedItem is BlendMode mode) SetActiveBlendMode(mode);
        };

        var opacityTextLabel = new Label { Text = "Непрозрачность слоя", AutoSize = true, Location = new Point(x, 74) };
        const int layerOpacityTrackWidth = SidebarInnerWidth - 12;
        _layerOpacityTrack.Location = new Point(x, 94);
        _layerOpacityTrack.Width = layerOpacityTrackWidth;
        _layerOpacityTrack.ValueChanged += (_, _) =>
        {
            if (_updatingLayerUi) return;
            _layerOpacityLabel.Text = $"{_layerOpacityTrack.Value}%";
            SetActiveOpacity((byte)Math.Clamp((int)Math.Round(_layerOpacityTrack.Value * 255f / 100f), 0, 255));
        };
        _layerOpacityLabel.AutoSize = true;
        _layerOpacityLabel.Location = new Point(x, 142);
        _layerVisibleCheck.Location = new Point(x, 168);
        _layerVisibleCheck.CheckedChanged += (_, _) =>
        {
            if (_updatingLayerUi) return;
            var active = _layers?.ActiveLayer;
            if (active is null) return;
            active.IsVisible = _layerVisibleCheck.Checked;
            RenderFromLayers();
        };

        _layerEffectGroup.Controls.Add(modeLabel);
        _layerEffectGroup.Controls.Add(_layerModeCombo);
        _layerEffectGroup.Controls.Add(opacityTextLabel);
        _layerEffectGroup.Controls.Add(_layerOpacityTrack);
        _layerEffectGroup.Controls.Add(_layerOpacityLabel);
        _layerEffectGroup.Controls.Add(_layerVisibleCheck);

        var bAdd = new Button { Text = "+", Width = 32, Height = 24 };
        var bDup = new Button { Text = "Д", Width = 32, Height = 24 };
        var bDel = new Button { Text = "-", Width = 32, Height = 24 };
        var bUp = new Button { Text = "↑", Width = 32, Height = 24 };
        var bDown = new Button { Text = "↓", Width = 32, Height = 24 };
        bAdd.Click += (_, _) => AddLayer();
        bDup.Click += (_, _) => DuplicateActiveLayer();
        bDel.Click += (_, _) => DeleteActiveLayer();
        bUp.Click += (_, _) => MoveActiveLayerUp();
        bDown.Click += (_, _) => MoveActiveLayerDown();
        _layerButtons.Controls.Add(bAdd);
        _layerButtons.Controls.Add(bDup);
        _layerButtons.Controls.Add(bDel);
        _layerButtons.Controls.Add(bUp);
        _layerButtons.Controls.Add(bDown);

        _layersList.Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        _layersList.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingLayerUi || _layers is null) return;
            var visualIndex = _layersList.SelectedIndex;
            if (visualIndex < 0) return;
            var stackIndex = (_layers.Count - 1) - visualIndex;
            _layers.SetActiveLayer(stackIndex);
            RefreshLayersSidebar();
        };

        _paintToolLabel.Location = new Point(x, 24);
        _paintSizeLabel.Location = new Point(x, 44);
        var colorCaption = new Label { AutoSize = true, Text = "Цвет:", Location = new Point(x, 68) };
        _paintColorSwatch.Location = new Point(x + 48, 65);
        _paintColorSwatch.BackColor = _selectedColor;
        _paletteCombo.Location = new Point(x, 92);
        _paletteCombo.SelectedIndexChanged += (_, _) => SelectPaletteByIndex(_paletteCombo.SelectedIndex);
        _paletteManageButton.Location = new Point(x, 120);
        _paletteManageButton.Click += (_, _) => ShowPaletteManager();
        _paletteAddColorButton.Location = new Point(x, 152);
        _paletteRemoveColorButton.Location = new Point(x, 180);
        _paletteAddColorButton.Click += (_, _) => AddCurrentColorToPalette();
        _paletteRemoveColorButton.Click += (_, _) => RemoveCurrentColorFromPalette();
        _palettePanel.Location = new Point(x, 212);
        _palettePanel.FlowDirection = FlowDirection.LeftToRight;
        _palettePanel.WrapContents = true;
        _palettePanel.Margin = Padding.Empty;

        var stabilizerCaption = new Label { AutoSize = true, Text = "Стабилизатор", Location = new Point(x, 394) };
        const int paintSliderWidth = SidebarInnerWidth - 52;
        _stabilizerTrack.Location = new Point(x, 416);
        _stabilizerTrack.Width = paintSliderWidth;
        _stabilizerTrack.Height = 6;
        _stabilizerTrack.Value = (int)Math.Round(_brushStabilization * 100f);
        _brushStabilization = _stabilizerTrack.Value / 100f;
        _stabilizerValueLabel.Text = $"{_stabilizerTrack.Value}%";
        _stabilizerTrack.ValueChanged += (_, _) =>
        {
            _brushStabilization = _stabilizerTrack.Value / 100f;
            _stabilizerValueLabel.Text = $"{_stabilizerTrack.Value}%";
        };
        _stabilizerValueLabel.Location = new Point(x + paintSliderWidth + 6, 412);

        var opacityBrushCaption = new Label { AutoSize = true, Text = "Прозрачность мазка", Location = new Point(x, 446) };
        _brushOpacityTrack.Location = new Point(x, 466);
        _brushOpacityTrack.Width = paintSliderWidth;
        _brushOpacityTrack.Value = (int)Math.Round(_brushOpacity * 100f);
        _brushOpacityTrack.ValueChanged += (_, _) =>
        {
            _brushOpacity = _brushOpacityTrack.Value / 100f;
            _brushOpacityValueLabel.Text = $"{_brushOpacityTrack.Value}%";
        };
        _brushOpacityValueLabel.Location = new Point(x + paintSliderWidth + 6, 462);

        _hueStripPanel.Location = new Point(x, 266);
        _hueStripPanel.Cursor = Cursors.SizeNS;
        _hueStripPanel.Paint += HueStripPanel_Paint;
        _hueStripPanel.MouseDown += HueStripPanel_MouseDown;
        _hueStripPanel.MouseMove += HueStripPanel_MouseMove;

        _svSquarePanel.Location = new Point(x + 26, 266);
        _svSquarePanel.Cursor = Cursors.Cross;
        _svSquarePanel.Paint += SvSquarePanel_Paint;
        _svSquarePanel.MouseDown += SvSquarePanel_MouseDown;
        _svSquarePanel.MouseMove += SvSquarePanel_MouseMove;
        _svSquarePanel.Resize += (_, _) =>
        {
            if (_svSquarePanel.ClientSize.Width < 2 || _svSquarePanel.ClientSize.Height < 2) return;
            RebuildSvGradientBitmap();
            _svSquarePanel.Invalidate();
        };

        BuildPaletteButtons();
        _paintGroup.Height = 520;
        _paintGroup.Controls.Add(_paintToolLabel);
        _paintGroup.Controls.Add(_paintSizeLabel);
        _paintGroup.Controls.Add(colorCaption);
        _paintGroup.Controls.Add(_paintColorSwatch);
        _paintGroup.Controls.Add(_paletteCombo);
        _paintGroup.Controls.Add(_paletteManageButton);
        _paintGroup.Controls.Add(_paletteAddColorButton);
        _paintGroup.Controls.Add(_paletteRemoveColorButton);
        _paintGroup.Controls.Add(_palettePanel);
        _paintGroup.Controls.Add(_hueStripPanel);
        _paintGroup.Controls.Add(_svSquarePanel);
        _paintGroup.Controls.Add(stabilizerCaption);
        _paintGroup.Controls.Add(_stabilizerTrack);
        _paintGroup.Controls.Add(_stabilizerValueLabel);
        _paintGroup.Controls.Add(opacityBrushCaption);
        _paintGroup.Controls.Add(_brushOpacityTrack);
        _paintGroup.Controls.Add(_brushOpacityValueLabel);

        SyncPickerFromSelectedColor();

        _layersPanel.Controls.Add(_layersList);
        _layersPanel.Controls.Add(_layerButtons);
        _layersPanel.Controls.Add(_paintGroup);
        _layersPanel.Controls.Add(_layerEffectGroup);
    }

    private void BuildStatusbar()
    {
        _status.Dock = DockStyle.Bottom;
        _status.Items.Add(_statusLeft);
        _status.Items.Add(_statusRight);
        Controls.Add(_status);
    }

    private void CreateNewCanvas()
    {
        using var dlg = new NewCanvasDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            int width = dlg.CanvasWidth;
            int height = dlg.CanvasHeight;
            Color bgColor = dlg.BackgroundColor;
            _canvasBackgroundColor = bgColor;

            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                using (var brush = new SolidBrush(bgColor))
                {
                    g.FillRectangle(brush, 0, 0, width, height);
                }
            }

            _history.Clear();
            _doc.SetImage(bmp, null);
            ResetLayersFromDocument();
            _selectionImg = Rectangle.Empty;
            _cropMode = false;
            if (_miCropMode is not null) _miCropMode.Checked = false;
            if (_tbCrop is not null) _tbCrop.Checked = false;
            SetZoom(1f);
            ZoomToFit();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка создания холста", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        UpdateUiState();
        Redraw();
    }

    private void OpenImage()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp|PNG|*.png|JPEG|*.jpg;*.jpeg|BMP|*.bmp|Все файлы|*.*",
            Title = "Открыть изображение"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var bmp = ImageIO.Load(dlg.FileName);
            _canvasBackgroundColor = Color.White;
            _history.Clear();
            _doc.SetImage(bmp, dlg.FileName);
            ResetLayersFromDocument();
            _selectionImg = Rectangle.Empty;
            _cropMode = false;
            if (_miCropMode is not null) _miCropMode.Checked = false;
            if (_tbCrop is not null) _tbCrop.Checked = false;
            SetZoom(1f);
            ZoomToFit();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка открытия", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        UpdateUiState();
        Redraw();
    }

    private void SaveImage()
    {
        if (!_doc.HasImage) return;
        if (!string.IsNullOrWhiteSpace(_doc.FilePath))
        {
            try
            {
                ImageIO.Save(_doc.Bitmap!, _doc.FilePath!);
                UpdateUiState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }

        SaveImageAs();
    }

    private void SaveImageAs()
    {
        if (!_doc.HasImage) return;
        using var dlg = new SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|BMP (*.bmp)|*.bmp",
            Title = "Сохранить изображение"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            ImageIO.Save(_doc.Bitmap!, dlg.FileName);
            _doc.SetImage(_doc.CloneBitmap(), dlg.FileName);
            SyncPictureToDocument();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        UpdateUiState();
    }

    private void ApplyOperation(Func<Bitmap, Bitmap> op)
    {
        if (!_doc.HasImage) return;
        if (_isBusy) return;

        try
        {
            PushUndoSnapshot();
            var activeLayer = _layers?.ActiveLayer;
            if (activeLayer?.Bitmap is not null)
            {
                var newBmp = op(activeLayer.Bitmap);
                activeLayer.SetBitmap(newBmp);
                newBmp.Dispose();
                RenderFromLayers();
            }
            else
            {
                var newBmp = op(_doc.Bitmap!);
                _doc.SetImage(newBmp, _doc.FilePath);
                ResetLayersFromDocument();
                SyncPictureToDocument();
            }
            _selectionImg = Rectangle.Empty;
            UpdateUiState();
            Redraw();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка операции", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ApplyOperationAsync(Func<Bitmap, Bitmap> op)
    {
        if (!_doc.HasImage) return;
        if (_isBusy) return;

        SetBusy(true);
        try
        {
            PushUndoSnapshot();
            var activeLayer = _layers?.ActiveLayer;
            if (activeLayer?.Bitmap is not null)
            {
                using var sourceCopy = new Bitmap(activeLayer.Bitmap);
                using var newBmp = await Task.Run(() => op(sourceCopy));
                activeLayer.SetBitmap(newBmp);
                RenderFromLayers();
            }
            else
            {
                using var sourceCopy = _doc.CloneBitmap();
                var newBmp = await Task.Run(() => op(sourceCopy));
                _doc.SetImage(newBmp, _doc.FilePath);
                ResetLayersFromDocument();
                SyncPictureToDocument();
            }
            _selectionImg = Rectangle.Empty;
            UpdateUiState();
            Redraw();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка операции", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        UseWaitCursor = busy;
        _menu.Enabled = !busy;
        _toolbar.Enabled = !busy;
        _mainSplit.Enabled = !busy;
        _statusLeft.Text = busy ? "Выполняется операция…" : _statusLeft.Text;
    }

    private void Undo()
    {
        if (!_doc.HasImage) return;
        if (_isBusy) return;
        CancelPastePlacement();
        if (!_history.TryPopUndo(out var prev) || prev is null) return;

        _history.PushRedo(CaptureEditorState());
        RestoreEditorState(prev);
        prev.Dispose();
        _selectionImg = Rectangle.Empty;
        UpdateUiState();
        Redraw();
    }

    private void Redo()
    {
        if (!_doc.HasImage) return;
        if (_isBusy) return;
        CancelPastePlacement();
        if (!_history.TryPopRedo(out var next) || next is null) return;

        _history.PushUndo(CaptureEditorState());
        RestoreEditorState(next);
        next.Dispose();
        _selectionImg = Rectangle.Empty;
        UpdateUiState();
        Redraw();
    }

    private void ResetLayersFromDocument()
    {
        _layers?.Dispose();
        _layers = null;

        if (!_doc.HasImage) return;

        _layers = new LayerStack(_doc.Bitmap!.Width, _doc.Bitmap.Height);
        _layers.LayersChanged += (_, _) => RenderFromLayers();
        _layers.ActiveLayerChanged += (_, _) => RebuildLayerMenus();
        _layers.AddLayer(new Layer(_doc.CloneBitmap(), "Слой 1"));
        RebuildLayerMenus();
    }

    private EditorStateSnapshot CaptureEditorState()
    {
        if (_layers is null || _layers.Count == 0)
        {
            var fallbackLayer = _doc.HasImage
                ? new Layer(_doc.CloneBitmap(), "Слой 1")
                : new Layer(1, 1, "Слой 1");
            using (fallbackLayer)
            {
                return new EditorStateSnapshot(new[] { fallbackLayer }, 0, _doc.FilePath, _canvasBackgroundColor);
            }
        }

        return new EditorStateSnapshot(_layers.Layers, _layers.ActiveLayerIndex, _doc.FilePath, _canvasBackgroundColor);
    }

    private void RestoreEditorState(EditorStateSnapshot snapshot)
    {
        if (snapshot.Layers.Count == 0) return;

        _canvasBackgroundColor = snapshot.CanvasBackgroundColor;
        _layers?.Dispose();
        var first = snapshot.Layers[0];
        _layers = new LayerStack(first.Width, first.Height);
        _layers.LayersChanged += (_, _) => RenderFromLayers();
        _layers.ActiveLayerChanged += (_, _) => RebuildLayerMenus();

        foreach (var layer in snapshot.Layers)
            _layers.AddLayer(layer.Clone(layer.Name));

        var activeIndex = Math.Clamp(snapshot.ActiveLayerIndex, 0, _layers.Count - 1);
        _layers.SetActiveLayer(activeIndex);

        var composed = _layers.Composite();
        _doc.SetImage(composed, snapshot.FilePath);
        SyncPictureToDocument();
        RebuildLayerMenus();
        RefreshLayersSidebar();
    }

    private void PushUndoSnapshot()
    {
        if (!_doc.HasImage || _layers is null || _layers.Count == 0) return;
        _history.PushUndo(CaptureEditorState());
    }

    private void RenderFromLayers(bool refreshSidebar = true, bool refreshUi = true, bool refreshMenus = true)
    {
        if (_layers is null || _layers.Count == 0) return;

        var filePath = _doc.FilePath;
        var composed = _layers.Composite();
        _doc.SetImage(composed, filePath);
        SyncPictureToDocument();
        if (refreshMenus)
            RebuildLayerMenus();
        else if (refreshSidebar)
            RefreshLayersSidebar();
        if (refreshUi) UpdateUiState();
        Redraw();
    }

    private void AddLayer()
    {
        if (_layers is null) return;
        _layers.AddLayer(new Layer(_layers.Width, _layers.Height, $"Слой {_layers.Count + 1}"));
    }

    private void DuplicateActiveLayer()
    {
        var active = _layers?.ActiveLayer;
        if (active is null || _layers is null) return;
        _layers.AddLayer(active.Clone($"{active.Name} (копия)"));
    }

    private void DeleteActiveLayer()
    {
        if (_layers is null || _layers.Count <= 1 || _layers.ActiveLayerIndex < 0) return;
        _layers.RemoveLayer(_layers.ActiveLayerIndex);
    }

    private void MoveActiveLayerUp()
    {
        if (_layers is null) return;
        var i = _layers.ActiveLayerIndex;
        if (i < 0 || i >= _layers.Count - 1) return;
        _layers.MoveLayer(i, i + 1);
        _layers.SetActiveLayer(i + 1);
    }

    private void MoveActiveLayerDown()
    {
        if (_layers is null) return;
        var i = _layers.ActiveLayerIndex;
        if (i <= 0) return;
        _layers.MoveLayer(i, i - 1);
        _layers.SetActiveLayer(i - 1);
    }

    private void ToggleActiveLayerVisibility()
    {
        var active = _layers?.ActiveLayer;
        if (active is null || _layers is null) return;
        active.IsVisible = !active.IsVisible;
        RenderFromLayers();
    }

    private void SetActiveBlendMode(BlendMode mode)
    {
        var active = _layers?.ActiveLayer;
        if (active is null) return;
        active.BlendMode = mode;
        RenderFromLayers();
    }

    private void SetActiveOpacity(byte opacity)
    {
        var active = _layers?.ActiveLayer;
        if (active is null) return;
        active.Opacity = opacity;
        RenderFromLayers();
    }

    private void SetTool(EditorTool tool)
    {
        CommitPastePlacement();

        if (tool == EditorTool.Selection)
        {
            if (_cropMode)
            {
                _cropMode = false;
                if (_tbCrop is not null) _tbCrop.Checked = false;
                if (_miCropMode is not null) _miCropMode.Checked = false;
            }
        }
        else if (tool != EditorTool.Selection && _currentTool == EditorTool.Selection)
        {
            _areaSelecting = false;
            _picture.Capture = false;
        }

        _currentTool = tool;
        if (_tbBrush is not null) _tbBrush.Checked = tool == EditorTool.Brush;
        if (_tbEraser is not null) _tbEraser.Checked = tool == EditorTool.Eraser;
        if (_tbEyedropper is not null) _tbEyedropper.Checked = tool == EditorTool.Eyedropper;
        if (_tbBucket is not null) _tbBucket.Checked = tool == EditorTool.Bucket;
        if (_tbHand is not null) _tbHand.Checked = tool == EditorTool.Hand;
        if (_tbSelect is not null) _tbSelect.Checked = tool == EditorTool.Selection;
        if (_tbAreaShape is not null) _tbAreaShape.Enabled = tool == EditorTool.Selection;
        _statusLeft.Text = tool switch
        {
            EditorTool.Brush => "Инструмент: Кисть",
            EditorTool.Eraser => "Инструмент: Ластик",
            EditorTool.Eyedropper => "Инструмент: Пипетка",
            EditorTool.Bucket => "Инструмент: Ведро",
            EditorTool.Hand => "Инструмент: Рука",
            EditorTool.Selection => "Инструмент: Выделение (Ctrl+C / Ctrl+V)",
            _ => _statusLeft.Text
        };
        _paintToolLabel.Text = tool switch
        {
            EditorTool.Brush => "Инструмент: кисть",
            EditorTool.Eraser => "Инструмент: ластик",
            EditorTool.Eyedropper => "Инструмент: пипетка",
            EditorTool.Bucket => "Инструмент: заливка",
            EditorTool.Hand => "Инструмент: рука",
            EditorTool.Selection => "Инструмент: выделение",
            _ => "Инструмент: кисть"
        };
    }

    private void SyncAreaShapeFromToolbar()
    {
        if (_tbAreaShape is null || _tbAreaShape.SelectedIndex < 0) return;
        _areaSelectionShape = _tbAreaShape.SelectedIndex switch
        {
            1 => AreaSelectionShape.Ellipse,
            2 => AreaSelectionShape.Freehand,
            _ => AreaSelectionShape.Rectangle
        };
    }

    private void ClearAreaSelection()
    {
        _areaSelecting = false;
        _lassoPoints.Clear();
        _areaSelectionPath?.Dispose();
        _areaSelectionPath = null;
        _selectionImg = Rectangle.Empty;
    }

    private void FinalizeAreaSelection()
    {
        _areaSelectionPath?.Dispose();
        _areaSelectionPath = null;

        if (_areaSelectionShape == AreaSelectionShape.Freehand)
        {
            if (_lassoPoints.Count < 3)
                return;
            var path = new GraphicsPath();
            path.AddLines(_lassoPoints.Select(p => new PointF(p.X, p.Y)).ToArray());
            path.CloseFigure();
            _areaSelectionPath = path;
            return;
        }

        if (_selectionImg.Width < 2 || _selectionImg.Height < 2)
            return;

        var p = new GraphicsPath();
        if (_areaSelectionShape == AreaSelectionShape.Rectangle)
            p.AddRectangle(_selectionImg);
        else
            p.AddEllipse(_selectionImg);
        _areaSelectionPath = p;
    }

    private bool HasAreaSelection => _areaSelectionPath is not null && _areaSelectionPath.PointCount > 0;

    private void CopySelectionToClipboard()
    {
        if (!_doc.HasImage || _doc.Bitmap is null || !HasAreaSelection)
            return;
        try
        {
            using var clipped = ImageSelectionOps.ExtractClipped(_doc.Bitmap, _areaSelectionPath!);
            if (clipped is null)
                return;
            ImageSelectionOps.SetClipboardImageWithAlpha(clipped);
        }
        catch (ExternalException)
        {
            MessageBox.Show(this, "Не удалось записать в буфер обмена.", "Буфер обмена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PasteFromClipboard()
    {
        if (_layers is null || !_doc.HasImage) return;
        var active = _layers.ActiveLayer;
        if (active?.Bitmap is null)
        {
            MessageBox.Show(this, "Нет активного слоя для вставки.", "Вставка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        CommitPastePlacement();

        var pasted = ImageSelectionOps.TryGetBitmapFromClipboard();
        if (pasted is null)
        {
            MessageBox.Show(this, "В буфере нет изображения.", "Вставка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using (pasted)
        {
            _pastePlacementBitmap = new Bitmap(pasted.Width, pasted.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(_pastePlacementBitmap))
                g.DrawImageUnscaled(pasted, 0, 0);
        }

        int w = _pastePlacementBitmap!.Width;
        int h = _pastePlacementBitmap.Height;
        _pastePlacementOx = Math.Max(0, (_layers.Width - w) / 2);
        _pastePlacementOy = Math.Max(0, (_layers.Height - h) / 2);

        ClearAreaSelection();
        _picture.Capture = false;
        UpdateUiState();
        Redraw();
    }

    private void CancelPastePlacement()
    {
        if (_pastePlacementBitmap is null) return;
        _pastePlacementBitmap.Dispose();
        _pastePlacementBitmap = null;
        UpdateUiState();
        Redraw();
    }

    private void CommitPastePlacement()
    {
        if (_pastePlacementBitmap is null || _layers is null) return;
        var layer = _layers.ActiveLayer;
        if (layer?.Bitmap is null)
        {
            CancelPastePlacement();
            return;
        }

        var floatBmp = _pastePlacementBitmap;
        _pastePlacementBitmap = null;

        PushUndoSnapshot();
        using (var g = Graphics.FromImage(layer.Bitmap))
        {
            g.CompositingMode = CompositingMode.SourceOver;
            g.DrawImage(floatBmp, _pastePlacementOx - layer.OffsetX, _pastePlacementOy - layer.OffsetY);
        }

        floatBmp.Dispose();
        RenderFromLayers();
    }

    private void UpdatePastePlacementFromCursor(Point imgPoint)
    {
        if (_pastePlacementBitmap is null || _layers is null) return;

        int w = _pastePlacementBitmap.Width;
        int h = _pastePlacementBitmap.Height;
        int ox = imgPoint.X - w / 2;
        int oy = imgPoint.Y - h / 2;
        ox = Math.Clamp(ox, -w + 1, _layers.Width - 1);
        oy = Math.Clamp(oy, -h + 1, _layers.Height - 1);
        if (ox == _pastePlacementOx && oy == _pastePlacementOy)
            return;

        _pastePlacementOx = ox;
        _pastePlacementOy = oy;
        _statusRight.Text = $"Вставка: x={ox}, y={oy} — ЛКМ в слой, Esc — отмена";
        Redraw();
    }

    private void DrawPastePlacementOverlay(Graphics g)
    {
        if (_pastePlacementBitmap is null || !_doc.HasImage || _doc.Bitmap is null) return;

        var full = new Rectangle(_pastePlacementOx, _pastePlacementOy, _pastePlacementBitmap.Width, _pastePlacementBitmap.Height);
        var vis = Rectangle.Intersect(new Rectangle(0, 0, _doc.Bitmap.Width, _doc.Bitmap.Height), full);
        if (vis.Width <= 0 || vis.Height <= 0) return;

        var src = new Rectangle(vis.X - _pastePlacementOx, vis.Y - _pastePlacementOy, vis.Width, vis.Height);
        var dst = ImageToDisplay(vis);
        g.DrawImage(_pastePlacementBitmap, dst, src, GraphicsUnit.Pixel);
    }

    private GraphicsPath? CreateSelectionOverlayInDisplaySpace()
    {
        if (_zoom <= 0) return null;
        float z = (float)_zoom;
        GraphicsPath? imgPath = null;

        if (_areaSelecting && _areaSelectionShape == AreaSelectionShape.Freehand && _lassoPoints.Count >= 2)
        {
            imgPath = new GraphicsPath();
            imgPath.AddLines(_lassoPoints.Select(p => (PointF)p).ToArray());
        }
        else if (_areaSelecting && _areaSelectionShape is AreaSelectionShape.Rectangle or AreaSelectionShape.Ellipse && _selectionImg.Width > 0 && _selectionImg.Height > 0)
        {
            imgPath = new GraphicsPath();
            if (_areaSelectionShape == AreaSelectionShape.Rectangle)
                imgPath.AddRectangle(_selectionImg);
            else
                imgPath.AddEllipse(_selectionImg);
        }
        else if (_areaSelectionPath is not null && _areaSelectionPath.PointCount > 0)
        {
            imgPath = (GraphicsPath)_areaSelectionPath.Clone();
        }

        if (imgPath is null)
            return null;

        using var m = new Matrix();
        m.Scale(z, z);
        imgPath.Transform(m);
        return imgPath;
    }

    private void LoadPalettes()
    {
        _palettes.Clear();
        _palettes.AddRange(ColorPalette.LoadPalettes());
        _paletteCombo.Items.Clear();
        foreach (var palette in _palettes)
            _paletteCombo.Items.Add(palette.Name);

        if (_palettes.Count == 0)
        {
            _currentPalette = null;
            BuildPaletteButtons();
            return;
        }

        _paletteCombo.SelectedIndex = 0;
        SelectPaletteByIndex(0);
    }

    private void ShowPaletteManager()
    {
        using var dlg = new PaletteManagerForm();
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        LoadPalettes();
    }

    private void SelectPaletteByIndex(int index)
    {
        if (index < 0 || index >= _palettes.Count) return;
        _currentPalette = _palettes[index];
        BuildPaletteButtons();
    }

    private void AddCurrentColorToPalette()
    {
        if (_currentPalette is null) return;
        if (_currentPalette.Colors.Any(c => c.ToArgb() == _selectedColor.ToArgb())) return;
        _currentPalette.Colors.Add(_selectedColor);
        _currentPalette.Save();
        BuildPaletteButtons();
    }

    private void RemoveCurrentColorFromPalette()
    {
        if (_currentPalette is null) return;
        int idx = _currentPalette.Colors.FindIndex(c => c.ToArgb() == _selectedColor.ToArgb());
        if (idx < 0) return;
        _currentPalette.Colors.RemoveAt(idx);
        _currentPalette.Save();
        BuildPaletteButtons();
    }

    private void BuildPaletteButtons()
    {
        _palettePanel.Controls.Clear();

        if (_currentPalette != null)
        {
            foreach (var color in _currentPalette.Colors)
            {
                var swatch = new Panel
                {
                    BackColor = color,
                    Width = 18,
                    Height = 18,
                    Margin = new Padding(1),
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };
                swatch.Click += (_, _) =>
                {
                    _selectedColor = color;
                    _paintColorSwatch.BackColor = _selectedColor;
                    SyncPickerFromSelectedColor();
                };
                _palettePanel.Controls.Add(swatch);
            }
        }
    }

    private void HueStripPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        int w = Math.Max(1, _hueStripPanel.ClientSize.Width);
        int h = Math.Max(2, _hueStripPanel.ClientSize.Height);
        for (int y = 0; y < h; y++)
        {
            float hue = h <= 1 ? 0f : (float)(y / (double)(h - 1) * 360.0);
            using var brush = new SolidBrush(ColorFromHsv(hue, 1.0, 1.0));
            g.FillRectangle(brush, 0, y, w, 1);
        }

        float hy = (float)(_pickerHue / 360.0 * (h - 1));
        hy = Math.Clamp(hy, 0f, h - 1f);
        using var linePen = new Pen(Color.Black, 2);
        linePen.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;
        g.DrawLine(linePen, 0, hy, w, hy);
        using var glow = new Pen(Color.White, 1);
        if (hy > 0.5f)
            g.DrawLine(glow, 0, hy - 1, w, hy - 1);
        if (hy < h - 1.5f)
            g.DrawLine(glow, 0, hy + 1, w, hy + 1);
    }

    private void HueStripPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            UpdateHueFromClientY(e.Y);
    }

    private void HueStripPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            UpdateHueFromClientY(e.Y);
    }

    private void UpdateHueFromClientY(int y)
    {
        int h = Math.Max(2, _hueStripPanel.ClientSize.Height);
        _pickerHue = (float)Math.Clamp(y / (double)(h - 1) * 360.0, 0.0, 360.0);
        RebuildSvGradientBitmap();
        ApplyPickerToSelectedColor();
        _hueStripPanel.Invalidate();
        _svSquarePanel.Invalidate();
    }

    private void SvSquarePanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        if (_svGradientBitmap is not null)
            g.DrawImage(_svGradientBitmap, new Rectangle(0, 0, _svSquarePanel.ClientSize.Width, _svSquarePanel.ClientSize.Height));

        int w = Math.Max(2, _svSquarePanel.ClientSize.Width);
        int h = Math.Max(2, _svSquarePanel.ClientSize.Height);
        float px = _pickerSat * (w - 1);
        float py = (1f - _pickerVal) * (h - 1);
        const float r = 4f;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var outer = new Pen(Color.White, 2);
        g.DrawEllipse(outer, px - r, py - r, r * 2, r * 2);
        using var inner = new Pen(Color.Black, 1);
        g.DrawEllipse(inner, px - r + 0.5f, py - r + 0.5f, r * 2 - 1, r * 2 - 1);
    }

    private void SvSquarePanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            UpdateSvFromClient(e.Location);
    }

    private void SvSquarePanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            UpdateSvFromClient(e.Location);
    }

    private void UpdateSvFromClient(Point p)
    {
        int w = Math.Max(2, _svSquarePanel.ClientSize.Width);
        int h = Math.Max(2, _svSquarePanel.ClientSize.Height);
        _pickerSat = (float)Math.Clamp(p.X / (double)(w - 1), 0, 1);
        _pickerVal = (float)Math.Clamp(1.0 - p.Y / (double)(h - 1), 0, 1);
        ApplyPickerToSelectedColor();
        _svSquarePanel.Invalidate();
    }

    private void ApplyPickerToSelectedColor()
    {
        _selectedColor = ColorFromHsv(_pickerHue, _pickerSat, _pickerVal);
        _paintColorSwatch.BackColor = _selectedColor;
    }

    private void SyncPickerFromSelectedColor()
    {
        RgbToHsv(_selectedColor, out var h, out var s, out var v);
        _pickerHue = h;
        _pickerSat = s;
        _pickerVal = v;
        RebuildSvGradientBitmap();
        _hueStripPanel.Invalidate();
        _svSquarePanel.Invalidate();
    }

    private void RebuildSvGradientBitmap()
    {
        int w = Math.Max(2, _svSquarePanel.ClientSize.Width);
        int h = Math.Max(2, _svSquarePanel.ClientSize.Height);
        _svGradientBitmap?.Dispose();
        var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        double hue = _pickerHue;
        for (int y = 0; y < h; y++)
        {
            double vv = h <= 1 ? 1.0 : 1.0 - (double)y / (h - 1);
            for (int x = 0; x < w; x++)
            {
                double ss = w <= 1 ? 0.0 : (double)x / (w - 1);
                bmp.SetPixel(x, y, ColorFromHsv(hue, ss, vv));
            }
        }

        _svGradientBitmap = bmp;
    }

    private static void RgbToHsv(Color c, out float h, out float s, out float v)
    {
        float r = c.R / 255f;
        float g = c.G / 255f;
        float b = c.B / 255f;
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        v = max;
        if (max < 1e-6f)
        {
            s = 0f;
            h = 0f;
            return;
        }

        s = delta / max;
        if (delta < 1e-6f)
        {
            h = 0f;
            return;
        }

        float hue;
        if (Math.Abs(max - r) < 1e-5f)
            hue = (((g - b) / delta % 6f) + 6f) % 6f;
        else if (Math.Abs(max - g) < 1e-5f)
            hue = (b - r) / delta + 2f;
        else
            hue = (r - g) / delta + 4f;

        h = 60f * hue;
        if (h < 0f) h += 360f;
        if (h >= 360f) h -= 360f;
    }

    private static Color ColorFromHsv(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        double c = value * saturation;
        double x = c * (1 - Math.Abs((hue / 60.0) % 2 - 1));
        double m = value - c;

        (double r, double g, double b) = hue switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x)
        };

        return Color.FromArgb(
            (int)Math.Round((r + m) * 255),
            (int)Math.Round((g + m) * 255),
            (int)Math.Round((b + m) * 255));
    }

    private Layer? GetActiveEditableLayer() => _layers?.ActiveLayer;

    // Курсор в координатах холста → координаты в bitmap слоя (учёт OffsetX/Y).
    private static PointF DocumentToLayerLocal(Layer layer, PointF documentImg) =>
        new(documentImg.X - layer.OffsetX, documentImg.Y - layer.OffsetY);

    private static Point DocumentToLayerLocal(Layer layer, Point documentImg) =>
        new(documentImg.X - layer.OffsetX, documentImg.Y - layer.OffsetY);

    private void DrawBrushStroke(PointF fromImg, PointF toImg, bool erase)
    {
        var layer = GetActiveEditableLayer();
        if (layer?.Bitmap is null) return;
        var drawColor = GetStrokeColor(erase);
        var fromLocal = DocumentToLayerLocal(layer, fromImg);
        var toLocal = DocumentToLayerLocal(layer, toImg);
        PaintingEngine.DrawStrokeSegment(layer.Bitmap, fromLocal, toLocal, drawColor, _brushSize, eraseToTransparency: erase && drawColor.A == 0);
    }

    private void DrawBrushStrokePreview(PointF fromImg, PointF toImg, bool erase)
    {
        if (!_doc.HasImage || _doc.Bitmap is null) return;
        var drawColor = GetStrokeColor(erase);
        PaintingEngine.DrawStrokeSegment(_doc.Bitmap, fromImg, toImg, drawColor, _brushSize, eraseToTransparency: erase && drawColor.A == 0);
    }

    private Color GetStrokeColor(bool erase)
    {
        if (!erase)
            return Color.FromArgb((int)Math.Round(_brushOpacity * 255f), _selectedColor);

        // For the bottom layer erase to canvas background color; for higher layers erase to transparency.
        if (_layers is not null && _layers.ActiveLayerIndex <= 0)
        {
            int alpha = (int)Math.Round(_brushOpacity * 255f);
            return Color.FromArgb(alpha, _canvasBackgroundColor);
        }

        return Color.Transparent;
    }

    private void RenderStrokePreview()
    {
        _picture.Invalidate();
    }

    private void FillAt(Point imgPoint)
    {
        var layer = GetActiveEditableLayer();
        if (layer?.Bitmap is null) return;
        PaintingEngine.FloodFill(layer.Bitmap, DocumentToLayerLocal(layer, imgPoint), _selectedColor);
    }

    private void RebuildLayerMenus()
    {
        if (_miLayerList is null || _miLayerBlendMode is null || _miLayerOpacity is null || _miLayerVisible is null)
            return;

        _miLayerList.DropDownItems.Clear();
        _miLayerBlendMode.DropDownItems.Clear();
        _miLayerOpacity.DropDownItems.Clear();

        var hasLayers = _layers is not null && _layers.Count > 0;
        if (!hasLayers)
        {
            _miLayerList.DropDownItems.Add(new ToolStripMenuItem("(нет слоев)") { Enabled = false });
            _miLayerBlendMode.DropDownItems.Add(new ToolStripMenuItem("(недоступно)") { Enabled = false });
            _miLayerOpacity.DropDownItems.Add(new ToolStripMenuItem("(недоступно)") { Enabled = false });
            _miLayerVisible.Checked = false;
            RefreshLayersSidebar();
            return;
        }

        var active = _layers!.ActiveLayer;
        _miLayerVisible.Checked = active?.IsVisible ?? false;

        for (int i = _layers.Count - 1; i >= 0; i--)
        {
            int idx = i;
            var layer = _layers.Layers[i];
            var item = new ToolStripMenuItem(layer.Name, null, (_, _) => _layers.SetActiveLayer(idx))
            {
                Checked = idx == _layers.ActiveLayerIndex
            };
            _miLayerList.DropDownItems.Add(item);
        }

        foreach (BlendMode mode in Enum.GetValues<BlendMode>())
        {
            var modeLocal = mode;
            var item = new ToolStripMenuItem(RussianUi.BlendModeName(modeLocal), null, (_, _) => SetActiveBlendMode(modeLocal))
            {
                Checked = active is not null && active.BlendMode == modeLocal
            };
            _miLayerBlendMode.DropDownItems.Add(item);
        }

        AddOpacityItem(25);
        AddOpacityItem(50);
        AddOpacityItem(75);
        AddOpacityItem(100);

        void AddOpacityItem(int percent)
        {
            byte opacity = (byte)Math.Clamp((int)Math.Round(percent * 255f / 100f), 0, 255);
            var item = new ToolStripMenuItem($"{percent}%", null, (_, _) => SetActiveOpacity(opacity))
            {
                Checked = active is not null && Math.Abs(active.Opacity - opacity) <= 1
            };
            _miLayerOpacity.DropDownItems.Add(item);
        }

        RefreshLayersSidebar();
    }

    private void RefreshLayersSidebar()
    {
        _updatingLayerUi = true;
        try
        {
            _layersList.Items.Clear();
            if (_layers is null || _layers.Count == 0)
            {
                _layerModeCombo.Enabled = false;
                _layerOpacityTrack.Enabled = false;
                _layerVisibleCheck.Enabled = false;
                _layerOpacityLabel.Text = "0%";
                return;
            }

            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                var layer = _layers.Layers[i];
                string text = $"{layer.Name} [{RussianUi.BlendModeName(layer.BlendMode)}] {(int)Math.Round(layer.Opacity * 100f / 255f)}%";
                if (!layer.IsVisible) text += " (скрыт)";
                _layersList.Items.Add(text);
            }

            int visualIndex = (_layers.Count - 1) - _layers.ActiveLayerIndex;
            if (visualIndex >= 0 && visualIndex < _layersList.Items.Count)
                _layersList.SelectedIndex = visualIndex;

            var active = _layers.ActiveLayer;
            bool hasActive = active is not null;
            _layerModeCombo.Enabled = hasActive;
            _layerOpacityTrack.Enabled = hasActive;
            _layerVisibleCheck.Enabled = hasActive;
            if (hasActive)
            {
                _layerModeCombo.SelectedItem = active!.BlendMode;
                int opacityPercent = (int)Math.Round(active.Opacity * 100f / 255f);
                _layerOpacityTrack.Value = Math.Clamp(opacityPercent, _layerOpacityTrack.Minimum, _layerOpacityTrack.Maximum);
                _layerOpacityLabel.Text = $"{opacityPercent}%";
                _layerVisibleCheck.Checked = active.IsVisible;
            }
        }
        finally
        {
            _updatingLayerUi = false;
        }
    }

    private void SyncPictureToDocument()
    {
        if (!_doc.HasImage)
        {
            _picture.Image = null;
            _picture.Size = new Size(1, 1);
            return;
        }

        // Draw image manually in Paint so we can control interpolation quality for zoom.
        _picture.Image = null;
        _picture.Size = new Size(
            Math.Max(1, (int)Math.Round(_doc.Bitmap!.Width * _zoom)),
            Math.Max(1, (int)Math.Round(_doc.Bitmap!.Height * _zoom))
        );
        UpdateCanvasPlacement();
        _picture.Invalidate();
    }

    private void ToggleCropMode(bool? force = null)
    {
        var newCrop = force ?? !_cropMode;
        if (newCrop)
            CommitPastePlacement();

        _cropMode = newCrop;
        if (_cropMode)
        {
            ClearAreaSelection();
            if (_currentTool == EditorTool.Selection)
                SetTool(EditorTool.Brush);
        }

        if (_miCropMode is not null) _miCropMode.Checked = _cropMode;
        if (_tbCrop is not null && _tbCrop.Checked != _cropMode) _tbCrop.Checked = _cropMode;
        _selectionImg = Rectangle.Empty;
        _isSelecting = false;
        UpdateUiState();
        Redraw();
    }

    private void ApplyCrop()
    {
        if (!_doc.HasImage) return;
        if (_isBusy) return;
        if (_selectionImg.Width <= 0 || _selectionImg.Height <= 0) return;

        try
        {
            var rect = Rectangle.Intersect(new Rectangle(0, 0, _doc.Bitmap!.Width, _doc.Bitmap!.Height), _selectionImg);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            // Crop the flat composite (what the user sees), then rebuild the layer stack to the new size.
            // Cropping only the active layer left the canvas size wrong and produced mismatched dimensions.
            PushUndoSnapshot();
            var cropped = ImageOperations.Crop(_doc.Bitmap!, rect);
            _doc.SetImage(cropped, _doc.FilePath);
            ResetLayersFromDocument();
            _selectionImg = Rectangle.Empty;
            ToggleCropMode(force: false);
            ZoomToFit();
            UpdateUiState();
            Redraw();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка обрезки", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowBrightnessContrastDialog()
    {
        if (!_doc.HasImage) return;

        using var dlg = new BrightnessContrastForm();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var brightness = dlg.Brightness;
        var contrast = dlg.Contrast;
        ApplyOperation(src => ImageOperations.AdjustBrightnessContrast(src, brightness, contrast));
    }

    private void ShowGammaDialog()
    {
        if (!_doc.HasImage) return;
        using var dlg = new GammaForm();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _ = ApplyOperationAsync(src => ImageOperations.AdjustGamma(src, dlg.Gamma));
    }

    private void ShowThresholdDialog()
    {
        if (!_doc.HasImage) return;
        using var dlg = new ThresholdForm();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _ = ApplyOperationAsync(src => ImageOperations.Threshold(src, dlg.Threshold));
    }

    private void ShowHslDialog()
    {
        if (!_doc.HasImage) return;
        using var dlg = new HslForm();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _ = ApplyOperationAsync(src => ImageOperations.AdjustHsl(src, dlg.HueShiftDegrees, dlg.SaturationDelta, dlg.LightnessDelta));
    }

    private void ShowResizeDialog()
    {
        if (!_doc.HasImage) return;
        using var dlg = new ResizeForm(_doc.Bitmap!.Width, _doc.Bitmap.Height);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _ = ApplyOperationAsync(src => ImageOperations.Resize(src, dlg.TargetWidth, dlg.TargetHeight, dlg.Mode))
            .ContinueWith(_ => BeginInvoke(() => ZoomToFit()));
    }

    private void ShowRotateDialog()
    {
        if (!_doc.HasImage) return;
        using var dlg = new RotateForm();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _ = ApplyOperationAsync(src => ImageOperations.Rotate(src, dlg.AngleDegrees, dlg.Mode, Color.Transparent))
            .ContinueWith(_ => BeginInvoke(() => ZoomToFit()));
    }

    private void ShowGaussianDialog()
    {
        if (!_doc.HasImage) return;
        using var dlg = new GaussianBlurForm();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _ = ApplyOperationAsync(src => Filters.GaussianBlur(src, dlg.Radius, dlg.Sigma));
    }

    private void ShowMedianDialog()
    {
        if (!_doc.HasImage) return;
        using var dlg = new MedianForm();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _ = ApplyOperationAsync(src => Filters.Median(src, dlg.Radius));
    }

    private void ShowUnsharpDialog()
    {
        if (!_doc.HasImage) return;
        using var dlg = new UnsharpForm();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _ = ApplyOperationAsync(src => Filters.UnsharpMask(src, dlg.Radius, dlg.Amount));
    }

    private void SetZoom(float newZoom)
    {
        if (!_doc.HasImage)
        {
            _zoom = 1f;
            _picture.Image = null;
            _picture.Size = new Size(1, 1);
            UpdateUiState();
            return;
        }

        _zoom = Math.Clamp(newZoom, 0.05f, 16f);

        _picture.Size = new Size(
            Math.Max(1, (int)Math.Round(_doc.Bitmap!.Width * _zoom)),
            Math.Max(1, (int)Math.Round(_doc.Bitmap!.Height * _zoom))
        );
        UpdateCanvasPlacement();

        UpdateUiState();
        Redraw();
    }

    private void UpdateCanvasPlacement()
    {
        if (!_doc.HasImage) return;

        var viewport = _scrollPanel.ClientSize;
        int pad = CanvasViewportPadding;
        int x = Math.Max(pad, (viewport.Width - _picture.Width) / 2);
        int y = Math.Max(pad, (viewport.Height - _picture.Height) / 2);
        _picture.Location = new Point(x, y);
    }

    private void ZoomToFit()
    {
        if (!_doc.HasImage) return;

        var client = _scrollPanel.ClientSize;
        if (client.Width <= 0 || client.Height <= 0) return;

        // Leave small padding so scrollbars aren't forced.
        var pad = 24;
        var zx = (client.Width - pad) / (float)_doc.Bitmap!.Width;
        var zy = (client.Height - pad) / (float)_doc.Bitmap!.Height;
        var z = Math.Min(zx, zy);
        SetZoom(z);
    }

    private void Picture_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!_doc.HasImage) return;

        if (e.Button == MouseButtons.Middle || _currentTool == EditorTool.Hand)
        {
            _isPanning = true;
            _panStartMouse = e.Location;
            _panStartScroll = new Point(-_scrollPanel.AutoScrollPosition.X, -_scrollPanel.AutoScrollPosition.Y);
            return;
        }

        if (e.Button != MouseButtons.Left) return;

        if (_pastePlacementBitmap is not null)
        {
            CommitPastePlacement();
            return;
        }

        if (_currentTool == EditorTool.Selection && !_cropMode)
        {
            _areaSelecting = true;
            _selStartImg = DisplayToImage(e.Location);
            if (_areaSelectionShape != AreaSelectionShape.Freehand)
                _selectionImg = Rectangle.Empty;
            _lassoPoints.Clear();
            if (_areaSelectionShape == AreaSelectionShape.Freehand)
                _lassoPoints.Add(ClampPointToImage(_selStartImg));
            _picture.Capture = true;
            Redraw();
            return;
        }

        if (_cropMode)
        {
            _isSelecting = true;
            _selStartImg = DisplayToImage(e.Location);
            _selectionImg = Rectangle.Empty;
            Redraw();
            return;
        }

        if (_currentTool == EditorTool.Eyedropper)
        {
            var img = _doc.Bitmap;
            if (img is null) return;
            var sample = DisplayToImage(e.Location);
            if (sample.X < 0 || sample.Y < 0 || sample.X >= img.Width || sample.Y >= img.Height) return;
            _selectedColor = img.GetPixel(sample.X, sample.Y);
            _paintColorSwatch.BackColor = _selectedColor;
            SyncPickerFromSelectedColor();
            SetTool(EditorTool.Brush);
            UpdateUiState();
            return;
        }

        if (_currentTool == EditorTool.Brush || _currentTool == EditorTool.Eraser)
        {
            PushUndoSnapshot();
            _isDrawingBrush = true;
            _picture.Capture = true;
            _lastBrushRawImg = ClampPointFToImage(DisplayToImageF(e.Location));
            _painting.BeginStroke(_lastBrushRawImg);
            _lastSmoothedBrushPoint = _lastBrushRawImg;
            DrawBrushStroke(_lastSmoothedBrushPoint, _lastSmoothedBrushPoint, erase: _currentTool == EditorTool.Eraser);
            DrawBrushStrokePreview(_lastSmoothedBrushPoint, _lastSmoothedBrushPoint, erase: _currentTool == EditorTool.Eraser);
            RenderStrokePreview();
            return;
        }

        if (_currentTool == EditorTool.Bucket)
        {
            PushUndoSnapshot();
            FillAt(DisplayToImage(e.Location));
            RenderFromLayers();
            UpdateUiState();
        }
    }

    private void Picture_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_doc.HasImage) return;

        var imgPt = DisplayToImage(e.Location);
        UpdateStatusCursor(imgPt);

        if (_isPanning)
        {
            int dx = e.Location.X - _panStartMouse.X;
            int dy = e.Location.Y - _panStartMouse.Y;
            _scrollPanel.AutoScrollPosition = new Point(
                Math.Max(0, _panStartScroll.X - dx),
                Math.Max(0, _panStartScroll.Y - dy));
            return;
        }

        if (_pastePlacementBitmap is not null && _layers is not null)
        {
            UpdatePastePlacementFromCursor(imgPt);
            return;
        }

        if ((_currentTool == EditorTool.Brush || _currentTool == EditorTool.Eraser) && _isDrawingBrush)
        {
            var rawPoint = ClampPointFToImage(DisplayToImageF(e.Location));
            var smoothedPoint = _painting.GetSmoothedPoint(rawPoint, _brushStabilization);
            DrawBrushStroke(_lastSmoothedBrushPoint, smoothedPoint, erase: _currentTool == EditorTool.Eraser);
            DrawBrushStrokePreview(_lastSmoothedBrushPoint, smoothedPoint, erase: _currentTool == EditorTool.Eraser);
            _lastBrushRawImg = rawPoint;
            _lastSmoothedBrushPoint = smoothedPoint;
            RenderStrokePreview();
            return;
        }

        if (_currentTool == EditorTool.Selection && _areaSelecting)
        {
            var imgCur = DisplayToImage(e.Location);
            if (_areaSelectionShape == AreaSelectionShape.Freehand)
            {
                var c = ClampPointToImage(imgCur);
                if (_lassoPoints.Count == 0 || SquaredDistance(_lassoPoints[^1], c) > 4)
                    _lassoPoints.Add(c);
            }
            else
            {
                _selectionImg = ClampToImage(NormalizeRect(_selStartImg, imgCur));
            }

            Redraw();
            return;
        }

        if (!_cropMode || !_isSelecting) return;

        var cur = DisplayToImage(e.Location);
        _selectionImg = NormalizeRect(_selStartImg, cur);
        _selectionImg = ClampToImage(_selectionImg);
        Redraw();
    }

    private void Picture_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_doc.HasImage) return;

        if (_isPanning && (e.Button == MouseButtons.Middle || _currentTool == EditorTool.Hand))
        {
            _isPanning = false;
            return;
        }

        if (e.Button != MouseButtons.Left) return;

        if (_isDrawingBrush)
        {
            _isDrawingBrush = false;
            _picture.Capture = false;
            _painting.EndStroke();
            RenderFromLayers();
            UpdateUiState();
            return;
        }

        if (_currentTool == EditorTool.Selection && _areaSelecting && e.Button == MouseButtons.Left)
        {
            _picture.Capture = false;
            _areaSelecting = false;
            FinalizeAreaSelection();
            UpdateUiState();
            Redraw();
            return;
        }

        if (!_cropMode || !_isSelecting) return;

        _isSelecting = false;
        var cur = DisplayToImage(e.Location);
        _selectionImg = NormalizeRect(_selStartImg, cur);
        _selectionImg = ClampToImage(_selectionImg);
        UpdateUiState();
        Redraw();
    }

    private void Picture_Paint(object? sender, PaintEventArgs e)
    {
        if (!_doc.HasImage) return;

        e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
        e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
        e.Graphics.PixelOffsetMode = _zoom >= 1f
            ? System.Drawing.Drawing2D.PixelOffsetMode.Half
            : System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        e.Graphics.InterpolationMode = _zoom >= 1f
            ? System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor
            : System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.DrawImage(_doc.Bitmap!, new Rectangle(0, 0, _picture.Width, _picture.Height));

        DrawPastePlacementOverlay(e.Graphics);

        using (var dispSel = CreateSelectionOverlayInDisplaySpace())
        {
            if (dispSel is not null)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool openLassoPreview = _areaSelecting && _areaSelectionShape == AreaSelectionShape.Freehand;
                if (!openLassoPreview)
                {
                    using var fill = new SolidBrush(Color.FromArgb(55, Color.MediumOrchid));
                    e.Graphics.FillPath(fill, dispSel);
                }

                using var pen = new Pen(Color.DarkOrchid, openLassoPreview ? 2.5f : 2f) { DashStyle = DashStyle.Dash };
                e.Graphics.DrawPath(pen, dispSel);
            }
        }

        e.Graphics.SmoothingMode = SmoothingMode.None;

        if (!_cropMode) return;
        if (_selectionImg.Width <= 0 || _selectionImg.Height <= 0) return;

        var disp = ImageToDisplay(_selectionImg);
        using var penCrop = new Pen(Color.DeepSkyBlue, 2);
        penCrop.DashStyle = DashStyle.Dash;
        e.Graphics.DrawRectangle(penCrop, disp);

        using var fillCrop = new SolidBrush(Color.FromArgb(50, Color.DeepSkyBlue));
        e.Graphics.FillRectangle(fillCrop, disp);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_isBusy && _doc.HasImage)
        {
            if (keyData == (Keys.Control | Keys.Z))
            {
                if (_history.CanUndo)
                {
                    Undo();
                    return true;
                }
            }

            if (keyData == (Keys.Control | Keys.Y) || keyData == (Keys.Control | Keys.Shift | Keys.Z))
            {
                if (_history.CanRedo)
                {
                    Redo();
                    return true;
                }
            }

            if (keyData == (Keys.Control | Keys.C) && HasAreaSelection)
            {
                CopySelectionToClipboard();
                return true;
            }

            if (keyData == (Keys.Control | Keys.V) && _layers is not null)
            {
                try
                {
                    if (Clipboard.ContainsImage())
                    {
                        PasteFromClipboard();
                        return true;
                    }
                }
                catch (ExternalException)
                {
                    // ignore
                }
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.C && e.Modifiers == Keys.None)
        {
            if (_doc.HasImage)
            {
                ToggleCropMode();
                e.Handled = true;
            }
            return;
        }

        if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None)
        {
            if (_cropMode && _selectionImg.Width > 0 && _selectionImg.Height > 0)
            {
                ApplyCrop();
                e.Handled = true;
            }
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            if (_pastePlacementBitmap is not null)
            {
                CancelPastePlacement();
                e.Handled = true;
            }
            else if (_cropMode)
            {
                _selectionImg = Rectangle.Empty;
                _isSelecting = false;
                UpdateUiState();
                Redraw();
                e.Handled = true;
            }
            else if (_areaSelectionPath is not null || _areaSelecting)
            {
                ClearAreaSelection();
                UpdateUiState();
                Redraw();
                e.Handled = true;
            }
        }
    }

    private Point DisplayToImage(Point displayPoint)
    {
        if (!_doc.HasImage || _zoom <= 0) return Point.Empty;
        var w = _doc.Bitmap!.Width;
        var h = _doc.Bitmap.Height;
        if (w <= 0 || h <= 0) return Point.Empty;

        var x = (int)Math.Floor(displayPoint.X / _zoom);
        var y = (int)Math.Floor(displayPoint.Y / _zoom);
        return new Point(Math.Clamp(x, 0, w - 1), Math.Clamp(y, 0, h - 1));
    }

    /// <summary>Image-space coordinates with sub-pixel precision (needed for stabilizer).</summary>
    private PointF DisplayToImageF(Point displayPoint)
    {
        if (!_doc.HasImage || _zoom <= 0) return PointF.Empty;
        var w = _doc.Bitmap!.Width;
        var h = _doc.Bitmap.Height;
        if (w <= 0 || h <= 0) return PointF.Empty;

        float x = displayPoint.X / _zoom;
        float y = displayPoint.Y / _zoom;
        return ClampPointFToImage(new PointF(x, y));
    }

    private PointF ClampPointFToImage(PointF point)
    {
        if (!_doc.HasImage) return point;
        float w = _doc.Bitmap!.Width;
        float h = _doc.Bitmap.Height;
        if (w <= 1f || h <= 1f) return point;
        const float eps = 1e-4f;
        return new PointF(
            Math.Clamp(point.X, 0f, w - eps),
            Math.Clamp(point.Y, 0f, h - eps));
    }

    private Rectangle ImageToDisplay(Rectangle imgRect)
    {
        if (!_doc.HasImage || _zoom <= 0) return Rectangle.Empty;
        double z = _zoom;
        int x1 = (int)Math.Floor(imgRect.X * z);
        int y1 = (int)Math.Floor(imgRect.Y * z);
        int x2 = (int)Math.Ceiling((imgRect.X + imgRect.Width) * z);
        int y2 = (int)Math.Ceiling((imgRect.Y + imgRect.Height) * z);
        return new Rectangle(x1, y1, Math.Max(0, x2 - x1), Math.Max(0, y2 - y1));
    }

    private Rectangle NormalizeRect(Point a, Point b)
    {
        var x1 = Math.Min(a.X, b.X);
        var y1 = Math.Min(a.Y, b.Y);
        var x2 = Math.Max(a.X, b.X);
        var y2 = Math.Max(a.Y, b.Y);
        return new Rectangle(x1, y1, x2 - x1, y2 - y1);
    }

    private Rectangle ClampToImage(Rectangle r)
    {
        if (!_doc.HasImage) return Rectangle.Empty;
        return Rectangle.Intersect(new Rectangle(0, 0, _doc.Bitmap!.Width, _doc.Bitmap.Height), r);
    }

    private Point ClampPointToImage(Point point)
    {
        if (!_doc.HasImage) return point;
        return new Point(
            Math.Clamp(point.X, 0, _doc.Bitmap!.Width - 1),
            Math.Clamp(point.Y, 0, _doc.Bitmap.Height - 1));
    }

    private static int SquaredDistance(Point a, Point b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private void UpdateUiState()
    {
        var has = _doc.HasImage;
        _miSave!.Enabled = has;
        _miSaveAs!.Enabled = has;
        _miUndo!.Enabled = has && _history.CanUndo;
        _miRedo!.Enabled = has && _history.CanRedo;
        if (_miCopy is not null) _miCopy.Enabled = has && HasAreaSelection;
        if (_miPaste is not null)
        {
            bool clipImg = false;
            try { clipImg = Clipboard.ContainsImage(); } catch { /* clipboard busy */ }
            _miPaste.Enabled = has && _layers is not null && clipImg;
        }
        _miCropMode!.Enabled = has;
        _miApplyCrop!.Enabled = has && _cropMode && _selectionImg.Width > 0 && _selectionImg.Height > 0;
        _miRotate90!.Enabled = has;
        _miRotateAny!.Enabled = has;
        _miFlipH!.Enabled = has;
        _miFlipV!.Enabled = has;
        _miBrightnessContrast!.Enabled = has;
        _miGamma!.Enabled = has;
        _miThreshold!.Enabled = has;
        _miHsl!.Enabled = has;
        _miResize!.Enabled = has;
        _miZoomIn!.Enabled = has;
        _miZoomOut!.Enabled = has;
        _miZoom100!.Enabled = has;
        _miZoomFit!.Enabled = has;
        _miLayersMenu!.Enabled = has;
        _miLayerAdd!.Enabled = has;

        var hasLayers = _layers is not null && _layers.Count > 0;
        var canDeleteLayer = hasLayers && _layers!.Count > 1;
        var activeIndex = hasLayers ? _layers!.ActiveLayerIndex : -1;
        _miLayerDuplicate!.Enabled = hasLayers && activeIndex >= 0;
        _miLayerDelete!.Enabled = canDeleteLayer && activeIndex >= 0;
        _miLayerMoveUp!.Enabled = hasLayers && activeIndex >= 0 && activeIndex < _layers!.Count - 1;
        _miLayerMoveDown!.Enabled = hasLayers && activeIndex > 0;
        _miLayerVisible!.Enabled = hasLayers && activeIndex >= 0;
        _miLayerList!.Enabled = hasLayers;
        _miLayerBlendMode!.Enabled = hasLayers && activeIndex >= 0;
        _miLayerOpacity!.Enabled = hasLayers && activeIndex >= 0;
        if (_tbSave is not null) _tbSave.Enabled = has;
        if (_tbUndo is not null) _tbUndo.Enabled = has && _history.CanUndo;
        if (_tbRedo is not null) _tbRedo.Enabled = has && _history.CanRedo;
        if (_tbCrop is not null) _tbCrop.Enabled = has;
        if (_tbBrush is not null) _tbBrush.Enabled = has;
        if (_tbEraser is not null) _tbEraser.Enabled = has;
        if (_tbEyedropper is not null) _tbEyedropper.Enabled = has;
        if (_tbBucket is not null) _tbBucket.Enabled = has;
        if (_tbHand is not null) _tbHand.Enabled = has;
        if (_tbSelect is not null) _tbSelect.Enabled = has;
        if (_tbAreaShape is not null) _tbAreaShape.Enabled = has && _currentTool == EditorTool.Selection;
        if (_tbBrushSize is not null) _tbBrushSize.Enabled = has;

        if (_miFiltersMenu is not null) _miFiltersMenu.Enabled = has;
        if (_miGaussian is not null) _miGaussian.Enabled = has;
        if (_miMedian is not null) _miMedian.Enabled = has;
        if (_miUnsharp is not null) _miUnsharp.Enabled = has;
        if (_miLaplace is not null) _miLaplace.Enabled = has;

        var sizeText = has ? $"{_doc.Bitmap!.Width}×{_doc.Bitmap.Height}px" : "Нет изображения";
        var modeText = _pastePlacementBitmap is not null
            ? "Режим: позиция вставки (ЛКМ — в слой, Esc — отмена)"
            : _cropMode
                ? "Режим: обрезка (C)"
                : _currentTool == EditorTool.Selection
                    ? "Режим: выделение"
                    : "Режим: просмотр";
        _statusLeft.Text = $"{sizeText}   |   {modeText}";
        _statusRight.Text = has ? $"Масштаб: {(int)Math.Round(_zoom * 100)}%" : "";
    }

    private void UpdateStatusCursor(Point imgPoint)
    {
        if (!_doc.HasImage) return;
        if (_pastePlacementBitmap is not null) return;
        var inside = imgPoint.X >= 0 && imgPoint.Y >= 0 && imgPoint.X < _doc.Bitmap!.Width && imgPoint.Y < _doc.Bitmap.Height;
        if (!inside) return;

        _statusRight.Text = $"Масштаб: {(int)Math.Round(_zoom * 100)}%   |   x={imgPoint.X}, y={imgPoint.Y}";
    }

    private void InitializeComponent()
    {

    }

    private void Redraw()
    {
        _picture.Invalidate();
        _scrollPanel.Invalidate();
        _status.Invalidate();
    }

}

