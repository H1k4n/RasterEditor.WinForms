# Текст для GitHub Release v1.0.0

Скопируйте поля ниже в форму **Releases → New release** на https://github.com/H1k4n/RasterEditor.WinFormsDemo/releases/new

---

## Tag

```
v1.0.0
```

## Release title

```
v1.0.0 — первый публичный релиз
```

## Release description (тело)

```markdown
Первый стабильный релиз **RasterEditor.WinFormsDemo** — учебного растрового редактора на C# / .NET 8 / WinForms с многослойной композицией и набором алгоритмов обработки изображений.

## Установка и запуск

1. Скачайте архив **RasterEditor-WinFormsDemo** из Assets этого релиза (или соберите из исходников).
2. Распакуйте в любую папку.
3. Убедитесь, что установлен [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) для Windows.
4. Запустите `RasterEditor.WinFormsDemo.exe`.

Сборка из исходников:

```bash
git clone https://github.com/H1k4n/RasterEditor.WinFormsDemo.git
cd RasterEditor.WinFormsDemo
dotnet run --project RasterEditor.WinFormsDemo.csproj
```

## Что входит в релиз

### Файлы и холст
- Открытие и сохранение PNG, JPEG, BMP
- Новый холст с настраиваемым размером и фоном
- Масштабирование: 100%, «вписать», горячие клавиши `Ctrl++` / `Ctrl+-`, `Ctrl+0`, `Ctrl+F`

### Слои
- Управление слоями: добавление, удаление, видимость, прозрачность
- **13 режимов наложения**: Normal, Multiply, Screen, Overlay, Add, Subtract, Difference, Exclusion, Copy, Lighten, Darken, Soft Light, Hard Light
- Композиция слоёв через `LayerCompositor`

### Рисование
- Кисть, ластик, пипетка, заливка, панорамирование
- Стабилизация штриха и прозрачность кисти
- Пользовательские палитры цветов (JSON в папке `Palettes`)

### Выделение и редактирование
- Прямоугольное, эллиптическое и свободное выделение
- Копирование / вставка с альфа-каналом
- Обрезка по выделению

### История изменений
- Undo / Redo (до 25 снимков состояния слоёв)

### Обработка изображения
- Геометрия: поворот, отражение, изменение размера (nearest / bilinear / bicubic)
- Цвет: яркость/контраст, гамма, порог, HSL
- Фильтры: Gaussian blur, median, unsharp mask, Laplace

Тяжёлые операции выполняются асинхронно, чтобы UI оставался отзывчивым.

## Системные требования

- Windows 10 / 11 (x64)
- .NET 8 Desktop Runtime

## Состав архива

| Файл / папка | Назначение |
|--------------|------------|
| `RasterEditor.WinFormsDemo.exe` | Запуск приложения |
| `RasterEditor.WinFormsDemo.dll` | Основная сборка |
| `Palettes/Default.json` | Стартовая палитра цветов |
| `*.deps.json`, `*.runtimeconfig.json` | Конфигурация .NET runtime |

## Известные ограничения

- Только Windows (WinForms + GDI+).
- Предпросмотр незавершённого штриха кисти рисуется на композитном bitmap документа, а не поверх live-композиции слоёв.
- История отмены ограничена 25 снимками.

## Лицензия

MIT — см. [LICENSE](https://github.com/H1k4n/RasterEditor.WinFormsDemo/blob/main/LICENSE).

---

**Full Changelog**: https://github.com/H1k4n/RasterEditor.WinFormsDemo/commits/v1.0.0
```

## Pre-release

Снять галочку **Set as a pre-release** — это стабильный первый релиз.

## Прикрепление бинарника

**Вариант A — из CI (после push в `main`):**
1. Откройте [Actions](https://github.com/H1k4n/RasterEditor.WinFormsDemo/actions) → последний успешный workflow **Build**.
2. Скачайте артефакт `RasterEditor-WinFormsDemo`.
3. Упакуйте содержимое в `RasterEditor.WinFormsDemo-v1.0.0-win-x64.zip`.
4. Прикрепите zip к релизу.

**Вариант B — локально:**

```bash
dotnet publish RasterEditor.WinFormsDemo.csproj -c Release -o publish
```

Упакуйте папку `publish/` в zip и прикрепите к релизу.

## Создание через GitHub CLI (опционально)

После подготовки zip-архива:

```bash
gh release create v1.0.0 ^
  --repo H1k4n/RasterEditor.WinFormsDemo ^
  --title "v1.0.0 — первый публичный релиз" ^
  --notes-file .github/RELEASE_v1.0.0-notes.md ^
  RasterEditor.WinFormsDemo-v1.0.0-win-x64.zip
```

Для CLI удобно сохранить только тело релиза (без обёртки markdown из этого файла) в отдельный файл `.github/RELEASE_v1.0.0-notes.md` — см. ниже.
