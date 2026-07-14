namespace RasterEditor.WinFormsDemo.Controls;

/// <summary>Panel with double-buffering for smooth custom painting (color pickers).</summary>
internal sealed class BufferedPanel : Panel
{
    public BufferedPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        ResizeRedraw = true;
    }
}
