using System.Drawing.Imaging;

namespace RasterEditor.WinFormsDemo.Core;

public sealed class ImageDocument : IDisposable
{
    private Bitmap? _bitmap;

    public Bitmap? Bitmap => _bitmap;

    public string? FilePath { get; private set; }

    public bool HasImage => _bitmap is not null;

    public Size? Size => _bitmap is null ? null : _bitmap.Size;

    public void SetImage(Bitmap bitmap, string? filePath = null)
    {
        if (bitmap is null) throw new ArgumentNullException(nameof(bitmap));

        var old = _bitmap;
        _bitmap = bitmap;
        FilePath = filePath;
        old?.Dispose();
    }

    public Bitmap CloneBitmap()
    {
        if (_bitmap is null) throw new InvalidOperationException("No image loaded.");
        return _bitmap.Clone(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), PixelFormat.Format32bppArgb);
    }

    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}

