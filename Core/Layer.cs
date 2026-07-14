namespace RasterEditor.WinFormsDemo.Core;

public sealed class Layer : IDisposable
{
    private Bitmap? _bitmap;

    public Guid Id { get; }
    public string Name { get; set; }
    public byte Opacity { get; set; }
    public BlendMode BlendMode { get; set; }
    public bool IsVisible { get; set; }
    public int Width => _bitmap?.Width ?? 0;
    public int Height => _bitmap?.Height ?? 0;
    public Bitmap? Bitmap => _bitmap;
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }

    public Layer(int width, int height, string name = "Слой")
    {
        Id = Guid.NewGuid();
        Name = name;
        Opacity = 255;
        BlendMode = BlendMode.Normal;
        IsVisible = true;
        OffsetX = 0;
        OffsetY = 0;

        _bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(_bitmap);
        g.Clear(Color.Transparent);
    }

    public Layer(Bitmap bitmap, string name = "Слой") : this(bitmap.Width, bitmap.Height, name)
    {
        if (bitmap is null) throw new ArgumentNullException(nameof(bitmap));
        _bitmap?.Dispose();
        _bitmap = new Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(_bitmap);
        g.DrawImage(bitmap, 0, 0);
    }

    public void Fill(Color color)
    {
        if (_bitmap is null) throw new InvalidOperationException("Layer bitmap is null.");
        using var g = Graphics.FromImage(_bitmap);
        g.Clear(color);
    }

    public Layer Clone(string? newName = null)
    {
        if (_bitmap is null) throw new InvalidOperationException("Layer bitmap is null.");
        var clone = new Layer(_bitmap, newName ?? $"{Name} (копия)")
        {
            Opacity = Opacity,
            BlendMode = BlendMode,
            IsVisible = IsVisible,
            OffsetX = OffsetX,
            OffsetY = OffsetY
        };
        return clone;
    }

    public void SetBitmap(Bitmap bitmap)
    {
        if (bitmap is null) throw new ArgumentNullException(nameof(bitmap));

        var replacement = new Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(replacement))
        {
            g.DrawImage(bitmap, 0, 0);
        }

        _bitmap?.Dispose();
        _bitmap = replacement;
    }

    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
