# RasterEditor.WinForms

Учебный растровый редактор на **C# / .NET 8 / WinForms** с многослойной композицией, инструментами рисования и набором алгоритмов обработки изображений. Проект подходит как демонстрация архитектуры desktop-приложения для обработки растра и как основа для дипломной работы.

## Возможности

### Файлы и холст
- Открыть / Сохранить / Сохранить как (PNG, JPEG, BMP)
- Новый холст с выбором размера и фона
- Масштаб: 100%, «вписать», `Ctrl++` / `Ctrl+-`, `Ctrl+0`, `Ctrl+F`

### Слои
- Добавление, удаление, переименование, видимость
- Прозрачность слоя (0–100%)
- 13 режимов наложения: Normal, Multiply, Screen, Overlay, Add, Subtract, Difference, Exclusion, Copy, Lighten, Darken, Soft Light, Hard Light
- Композиция слоёв в итоговый bitmap через `LayerCompositor`

### Рисование и выделение
- Кисть, ластик, пипетка, заливка, рука (панорамирование)
- Стабилизация штриха и прозрачность кисти
- Палитры цветов (JSON, см. [PALETTES_README.md](PALETTES_README.md))
- Выделение: прямоугольник, эллипс, свободный контур
- Копирование / вставка с альфа-каналом и превью размещения
- Обрезка по выделению (режим **C**, применить **Enter**, сброс **Esc**)

### История
- Undo / Redo (до 25 снимков состояния слоёв)
- Снимок создаётся перед штрихом, заливкой, вставкой, обрезкой и тяжёлыми операциями

### Геометрия
- Поворот 90°, отражения по горизонтали и вертикали
- Поворот на произвольный угол (nearest / bilinear / bicubic)
- Изменение размера (nearest / bilinear / bicubic)

### Цвет и точечные операции
- Яркость / контраст
- Гамма-коррекция
- Порог (ч/б)
- HSL (сдвиг оттенка, насыщенность, светлота)

### Фильтры
- Размытие по Гауссу
- Медианный фильтр
- Резкость (Unsharp mask)
- Лаплас (контуры)

Тяжёлые операции выполняются асинхронно, чтобы интерфейс не блокировался.

### CLI

```bash
git clone https://github.com/H1k4n/RasterEditor.WinFormsDemo.git
cd RasterEditor.WinFormsDemo
dotnet run --project RasterEditor.WinFormsDemo.csproj
```

## Структура проекта

```
RasterEditor.WinFormsDemo/
├── Core/                 # Документ, слои, композиция, I/O
│   ├── ImageDocument.cs
│   ├── LayerStack.cs, Layer.cs
│   ├── LayerCompositor.cs
│   ├── BlendMode.cs
│   └── ImageIO.cs
├── Services/
│   └── PaintingEngine.cs # Кисть, ластик, заливка
├── Operations/           # Алгоритмы обработки и выделения
│   ├── ImageOperations.cs
│   ├── ImageSelectionOps.cs
│   └── Filters.cs
├── History/
│   └── UndoRedoStack.cs
├── Forms/                # Главное окно и диалоги параметров
├── Controls/             # BufferedPanel и прочие контролы
├── Localization/
│   └── RussianUi.cs
├── Palettes/             # Стартовые палитры (копируются в output)
└── Program.cs
```

| Модуль | Назначение |
|--------|------------|
| `MainForm` | UI, ввод, координация всех подсистем |
| `ImageDocument` | Итоговый bitmap на холсте |
| `LayerStack` / `Layer` | Модель слоёв |
| `LayerCompositor` | Сведение слоёв с blend modes |
| `PaintingEngine` | Рисование на активном слое |
| `ImageOperations` / `Filters` | Геометрия, цвет, фильтры |
| `ImageSelectionOps` | Выделение, копирование, вставка |
| `UndoRedoStack` | Отмена и повтор |


## Лицензия

[MIT](LICENSE)

## См. также

- [PALETTES_README.md](PALETTES_README.md) — формат пользовательских палитр
