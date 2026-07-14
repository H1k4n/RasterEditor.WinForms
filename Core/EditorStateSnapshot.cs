namespace RasterEditor.WinFormsDemo.Core;

public sealed class EditorStateSnapshot : IDisposable
{
    private readonly List<Layer> _layers;

    public IReadOnlyList<Layer> Layers => _layers;
    public int ActiveLayerIndex { get; }
    public string? FilePath { get; }
    public Color CanvasBackgroundColor { get; }

    public EditorStateSnapshot(IEnumerable<Layer> layers, int activeLayerIndex, string? filePath, Color canvasBackgroundColor)
    {
        _layers = layers.Select(layer => layer.Clone(layer.Name)).ToList();
        ActiveLayerIndex = activeLayerIndex;
        FilePath = filePath;
        CanvasBackgroundColor = canvasBackgroundColor;
    }

    public void Dispose()
    {
        foreach (var layer in _layers)
            layer.Dispose();
        _layers.Clear();
    }
}
