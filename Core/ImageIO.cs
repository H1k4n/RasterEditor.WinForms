using System.Drawing.Imaging;

namespace RasterEditor.WinFormsDemo.Core;

public static class ImageIO
{
    public static Bitmap Load(string path)
    {
        // Image.FromFile keeps the file locked; clone it to release the lock.
        using var img = Image.FromFile(path);
        var bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.DrawImage(img, new Rectangle(0, 0, bmp.Width, bmp.Height));
        return bmp;
    }

    public static void Save(Bitmap bitmap, string path)
    {
        if (bitmap is null) throw new ArgumentNullException(nameof(bitmap));
        if (path is null) throw new ArgumentNullException(nameof(path));

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var format = ext switch
        {
            ".png" => ImageFormat.Png,
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            _ => ImageFormat.Png
        };

        bitmap.Save(path, format);
    }
}

