namespace RasterEditor.WinFormsDemo.Core;

public sealed class LayerStack : IDisposable
{
    private readonly List<Layer> _layers = new();
    private int _activeLayerIndex = -1;

    public IReadOnlyList<Layer> Layers => _layers.AsReadOnly();
    public int Count => _layers.Count;
    public int ActiveLayerIndex => _activeLayerIndex;
    public Layer? ActiveLayer => _activeLayerIndex >= 0 && _activeLayerIndex < _layers.Count ? _layers[_activeLayerIndex] : null;
    public int Width { get; }
    public int Height { get; }

    public event EventHandler? LayersChanged;
    public event EventHandler? ActiveLayerChanged;

    public LayerStack(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void AddLayer(Layer layer, int? index = null)
    {
        if (layer is null) throw new ArgumentNullException(nameof(layer));

        if (index.HasValue)
        {
            _layers.Insert(Math.Clamp(index.Value, 0, _layers.Count), layer);
        }
        else
        {
            _layers.Add(layer);
            _activeLayerIndex = _layers.Count - 1;
        }

        LayersChanged?.Invoke(this, EventArgs.Empty);
        ActiveLayerChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveLayer(int index)
    {
        if (index < 0 || index >= _layers.Count) throw new IndexOutOfRangeException();

        _layers[index].Dispose();
        _layers.RemoveAt(index);

        if (_activeLayerIndex >= _layers.Count)
            _activeLayerIndex = _layers.Count - 1;
        if (_activeLayerIndex < 0)
            _activeLayerIndex = -1;

        LayersChanged?.Invoke(this, EventArgs.Empty);
        if (_activeLayerIndex == index) ActiveLayerChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveLayer(Layer layer)
    {
        var index = _layers.IndexOf(layer);
        if (index >= 0) RemoveLayer(index);
    }

    public void SetActiveLayer(int index)
    {
        if (index < -1 || index >= _layers.Count) throw new IndexOutOfRangeException();
        if (_activeLayerIndex != index)
        {
            _activeLayerIndex = index;
            ActiveLayerChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void MoveLayer(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _layers.Count) throw new IndexOutOfRangeException(nameof(fromIndex));
        if (toIndex < 0 || toIndex >= _layers.Count) throw new IndexOutOfRangeException(nameof(toIndex));

        var layer = _layers[fromIndex];
        _layers.RemoveAt(fromIndex);
        _layers.Insert(toIndex, layer);

        if (_activeLayerIndex == fromIndex)
            _activeLayerIndex = toIndex;
        else if (fromIndex < _activeLayerIndex && toIndex >= _activeLayerIndex)
            _activeLayerIndex--;
        else if (fromIndex > _activeLayerIndex && toIndex <= _activeLayerIndex)
            _activeLayerIndex++;

        LayersChanged?.Invoke(this, EventArgs.Empty);
    }

    public Bitmap Composite()
    {
        return LayerCompositor.Compose(Width, Height, _layers);
    }

    public void Clear()
    {
        foreach (var layer in _layers)
            layer.Dispose();
        _layers.Clear();
        _activeLayerIndex = -1;
        LayersChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Clear();
    }
}
