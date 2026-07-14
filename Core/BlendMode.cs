namespace RasterEditor.WinFormsDemo.Core;

/// <summary>
/// Режимы наложения слоёв согласно стандартам графических редакторов.
/// </summary>
public enum BlendMode
{
    Normal = 0,
    Multiply = 1,
    Screen = 2,
    Overlay = 3,
    Add = 4,
    Subtract = 5,
    Difference = 6,
    Exclusion = 7,
    Copy = 8,
    Lighten = 9,
    Darken = 10,
    SoftLight = 11,
    HardLight = 12
}
