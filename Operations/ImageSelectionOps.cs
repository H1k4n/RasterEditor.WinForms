using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RasterEditor.WinFormsDemo.Operations;

/// <summary>Вырезание области по контуру и чтение изображения из буфера обмена.</summary>
public static class ImageSelectionOps
{
    /// <summary>
    /// Копирует пиксели из <paramref name="source"/> в новый bitmap размером с ограничивающий прямоугольник контура.
    /// Вне контура — прозрачный альфа-канал.
    /// </summary>
    public static Bitmap? ExtractClipped(Bitmap source, GraphicsPath pathInImagePixels)
    {
        if (source is null || pathInImagePixels is null || pathInImagePixels.PointCount == 0)
            return null;

        using var pathCopy = (GraphicsPath)pathInImagePixels.Clone();
        var boundsF = pathCopy.GetBounds();
        var bounds = Rectangle.Intersect(
            Rectangle.FromLTRB(
                (int)Math.Floor(boundsF.Left),
                (int)Math.Floor(boundsF.Top),
                (int)Math.Ceiling(boundsF.Right),
                (int)Math.Ceiling(boundsF.Bottom)),
            new Rectangle(0, 0, source.Width, source.Height));

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return null;

        var dest = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(dest))
        {
            g.Clear(Color.Transparent);
            g.TranslateTransform(-bounds.X, -bounds.Y);
            g.SetClip(pathCopy);
            g.DrawImage(source, Point.Empty);
        }

        return dest;
    }

    /// <summary>
    /// Кладёт изображение в буфер с сохранением прозрачности: формат PNG (читаем мы и часть приложений)
    /// плюс <see cref="DataFormats.Bitmap"/> как запасной вариант.
    /// </summary>
    /// <remarks><see cref="Clipboard.SetImage"/> теряет альфу (часто даёт серый фон у лассо/овала).</remarks>
    public static void SetClipboardImageWithAlpha(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var dataObject = new DataObject();

        using (var pngEncoded = new MemoryStream())
        {
            bitmap.Save(pngEncoded, ImageFormat.Png);
            dataObject.SetData("PNG", false, new MemoryStream(pngEncoded.ToArray()));
        }

        dataObject.SetData(DataFormats.Bitmap, new Bitmap(bitmap));
        Clipboard.SetDataObject(dataObject, copy: true);
    }

    public static Bitmap? TryGetBitmapFromClipboard()
    {
        try
        {
            var data = Clipboard.GetDataObject();
            if (data is not null)
            {
                foreach (var format in new[] { "PNG", "image/png" })
                {
                    try
                    {
                        var payload = data.GetData(format, autoConvert: false);
                        if (payload is byte[] bytes && bytes.Length > 0)
                        {
                            using var ms = new MemoryStream(bytes);
                            using var temp = new Bitmap(ms);
                            return new Bitmap(temp);
                        }

                        if (payload is Stream stream)
                        {
                            if (stream.CanSeek)
                                stream.Position = 0;
                            using var ms = new MemoryStream();
                            stream.CopyTo(ms);
                            if (ms.Length == 0)
                                continue;
                            ms.Position = 0;
                            using var temp = new Bitmap(ms);
                            return new Bitmap(temp);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Невалидный PNG в этом формате — пробуем следующий
                    }
                }
            }

            if (!Clipboard.ContainsImage())
                return null;
            using var img = Clipboard.GetImage();
            if (img is null)
                return null;
            if (img is Bitmap bm)
                return new Bitmap(bm);
            return new Bitmap(img);
        }
        catch (ExternalException)
        {
            return null;
        }
    }
}
