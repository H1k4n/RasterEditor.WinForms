using RasterEditor.WinFormsDemo.Core;
using RasterEditor.WinFormsDemo.Operations;

namespace RasterEditor.WinFormsDemo.Localization;

internal static class RussianUi
{
    public static string BlendModeName(BlendMode m) => m switch
    {
        BlendMode.Normal => "Нормальный",
        BlendMode.Multiply => "Умножение",
        BlendMode.Screen => "Осветление",
        BlendMode.Overlay => "Перекрытие",
        BlendMode.Add => "Добавление",
        BlendMode.Subtract => "Вычитание",
        BlendMode.Difference => "Разница",
        BlendMode.Exclusion => "Исключение",
        BlendMode.Copy => "Копия",
        BlendMode.Lighten => "Замена светлым",
        BlendMode.Darken => "Замена тёмным",
        BlendMode.SoftLight => "Мягкий свет",
        BlendMode.HardLight => "Жёсткий свет",
        _ => m.ToString()
    };

    public static string ResampleModeName(ResampleMode m) => m switch
    {
        ResampleMode.Nearest => "Ближайший сосед",
        ResampleMode.Bilinear => "Билинейная",
        ResampleMode.Bicubic => "Бикубическая",
        _ => m.ToString()
    };
}
