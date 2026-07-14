using System.Drawing.Imaging;

namespace RasterEditor.WinFormsDemo.Operations;

public static class ImageOperations
{
    public static Bitmap RotateFlip(Bitmap src, RotateFlipType type)
    {
        if (src is null) throw new ArgumentNullException(nameof(src));
        var dst = Clone32(src);
        dst.RotateFlip(type);
        return dst;
    }

    public static Bitmap Crop(Bitmap src, Rectangle rect)
    {
        if (src is null) throw new ArgumentNullException(nameof(src));

        rect = Rectangle.Intersect(new Rectangle(0, 0, src.Width, src.Height), rect);
        if (rect.Width <= 0 || rect.Height <= 0)
            throw new ArgumentException("Crop rectangle is empty.", nameof(rect));

        var dst = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(dst);
        g.DrawImage(src, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
        return dst;
    }

    public static Bitmap AdjustBrightnessContrast(Bitmap src, float brightness, float contrast)
    {
        // brightness: [-1..1] where 0=no change
        // contrast:   [-1..1] where 0=no change
        if (src is null) throw new ArgumentNullException(nameof(src));

        brightness = Math.Clamp(brightness, -1f, 1f);
        contrast = Math.Clamp(contrast, -1f, 1f);

        var c = 1f + contrast; // 0..2
        var t = 0.5f * (1f - c) + brightness; // shift around midpoint + brightness

        var matrix = new ColorMatrix(new[]
        {
            new[] { c, 0f, 0f, 0f, 0f },
            new[] { 0f, c, 0f, 0f, 0f },
            new[] { 0f, 0f, c, 0f, 0f },
            new[] { 0f, 0f, 0f, 1f, 0f },
            new[] { t, t, t, 0f, 1f },
        });

        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(dst);
        using var attrs = new ImageAttributes();
        attrs.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attrs);
        return dst;
    }

    public static Bitmap AdjustGamma(Bitmap src, float gamma)
    {
        if (src is null) throw new ArgumentNullException(nameof(src));
        gamma = Math.Clamp(gamma, 0.1f, 10f);

        var lut = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            var n = i / 255f;
            var v = (int)Math.Round(Math.Pow(n, 1f / gamma) * 255f);
            lut[i] = (byte)Math.Clamp(v, 0, 255);
        }

        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using var a = new BitmapAccess(src, ImageLockMode.ReadOnly);
        using var b = new BitmapAccess(dst, ImageLockMode.WriteOnly);

        for (var y = 0; y < a.Height; y++)
        for (var x = 0; x < a.Width; x++)
        {
            BitmapAccess.UnpackArgb(a.GetPixelArgb(x, y), out var aa, out var rr, out var gg, out var bb);
            var nr = lut[rr];
            var ng = lut[gg];
            var nb = lut[bb];
            b.SetPixelArgb(x, y, BitmapAccess.PackArgb(aa, nr, ng, nb));
        }

        return dst;
    }

    public static Bitmap Threshold(Bitmap src, byte threshold)
    {
        if (src is null) throw new ArgumentNullException(nameof(src));
        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);

        using var a = new BitmapAccess(src, ImageLockMode.ReadOnly);
        using var b = new BitmapAccess(dst, ImageLockMode.WriteOnly);

        for (var y = 0; y < a.Height; y++)
        for (var x = 0; x < a.Width; x++)
        {
            BitmapAccess.UnpackArgb(a.GetPixelArgb(x, y), out var aa, out var rr, out var gg, out var bb);
            var gray = (byte)Math.Clamp((int)Math.Round(0.299 * rr + 0.587 * gg + 0.114 * bb), 0, 255);
            var v = gray >= threshold ? (byte)255 : (byte)0;
            b.SetPixelArgb(x, y, BitmapAccess.PackArgb(aa, v, v, v));
        }

        return dst;
    }

    public static Bitmap AdjustHsl(Bitmap src, float hueShiftDegrees, float saturationDelta, float lightnessDelta)
    {
        // hueShiftDegrees: [-180..180]
        // saturationDelta: [-1..1] additive
        // lightnessDelta:  [-1..1] additive
        if (src is null) throw new ArgumentNullException(nameof(src));

        hueShiftDegrees = Math.Clamp(hueShiftDegrees, -180f, 180f);
        saturationDelta = Math.Clamp(saturationDelta, -1f, 1f);
        lightnessDelta = Math.Clamp(lightnessDelta, -1f, 1f);

        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using var a = new BitmapAccess(src, ImageLockMode.ReadOnly);
        using var b = new BitmapAccess(dst, ImageLockMode.WriteOnly);

        for (var y = 0; y < a.Height; y++)
        for (var x = 0; x < a.Width; x++)
        {
            BitmapAccess.UnpackArgb(a.GetPixelArgb(x, y), out var aa, out var rr, out var gg, out var bb);
            ColorMath.RgbToHsl(rr, gg, bb, out var h, out var s, out var l);

            h = h + hueShiftDegrees;
            s = ColorMath.Clamp01(s + saturationDelta);
            l = ColorMath.Clamp01(l + lightnessDelta);

            ColorMath.HslToRgb(h, s, l, out var nr, out var ng, out var nb);
            b.SetPixelArgb(x, y, BitmapAccess.PackArgb(aa, nr, ng, nb));
        }

        return dst;
    }

    public static Bitmap Resize(Bitmap src, int newWidth, int newHeight, ResampleMode mode)
    {
        if (src is null) throw new ArgumentNullException(nameof(src));
        if (newWidth <= 0) throw new ArgumentOutOfRangeException(nameof(newWidth));
        if (newHeight <= 0) throw new ArgumentOutOfRangeException(nameof(newHeight));

        if (newWidth == src.Width && newHeight == src.Height)
            return Clone32(src);

        var dst = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

        using var a = new BitmapAccess(src, ImageLockMode.ReadOnly);
        using var b = new BitmapAccess(dst, ImageLockMode.WriteOnly);

        var sx = a.Width / (float)newWidth;
        var sy = a.Height / (float)newHeight;

        for (var y = 0; y < newHeight; y++)
        {
            var fy = (y + 0.5f) * sy - 0.5f;
            for (var x = 0; x < newWidth; x++)
            {
                var fx = (x + 0.5f) * sx - 0.5f;
                var c = mode switch
                {
                    ResampleMode.Nearest => SampleNearest(a, fx, fy),
                    ResampleMode.Bilinear => SampleBilinear(a, fx, fy),
                    ResampleMode.Bicubic => SampleBicubic(a, fx, fy),
                    _ => SampleBilinear(a, fx, fy)
                };
                b.SetPixelArgb(x, y, c);
            }
        }

        return dst;
    }

    public static Bitmap Rotate(Bitmap src, float angleDegrees, ResampleMode mode, Color? background = null)
    {
        if (src is null) throw new ArgumentNullException(nameof(src));
        var bg = background ?? Color.Transparent;

        var theta = angleDegrees * (float)(Math.PI / 180.0);
        var cos = (float)Math.Cos(theta);
        var sin = (float)Math.Sin(theta);

        var w = src.Width;
        var h = src.Height;
        var cx = (w - 1) / 2f;
        var cy = (h - 1) / 2f;

        // Compute bounds of rotated image.
        var corners = new[]
        {
            RotatePoint(-cx, -cy, cos, sin),
            RotatePoint(w - 1 - cx, -cy, cos, sin),
            RotatePoint(-cx, h - 1 - cy, cos, sin),
            RotatePoint(w - 1 - cx, h - 1 - cy, cos, sin),
        };
        var minX = corners.Min(p => p.X);
        var maxX = corners.Max(p => p.X);
        var minY = corners.Min(p => p.Y);
        var maxY = corners.Max(p => p.Y);

        var newW = Math.Max(1, (int)Math.Ceiling(maxX - minX + 1));
        var newH = Math.Max(1, (int)Math.Ceiling(maxY - minY + 1));

        var dst = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
        using var a = new BitmapAccess(src, ImageLockMode.ReadOnly);
        using var b = new BitmapAccess(dst, ImageLockMode.WriteOnly);

        BitmapAccess.UnpackArgb((uint)bg.ToArgb(), out var bga, out var bgr, out var bgg, out var bgb);
        var bgPacked = BitmapAccess.PackArgb(bga, bgr, bgg, bgb);

        var ncx = (newW - 1) / 2f;
        var ncy = (newH - 1) / 2f;

        // Inverse mapping: dst -> src.
        var icos = cos;
        var isin = -sin;

        for (var y = 0; y < newH; y++)
        for (var x = 0; x < newW; x++)
        {
            var dx = x - ncx;
            var dy = y - ncy;

            var sx = dx * icos - dy * isin + cx;
            var sy = dx * isin + dy * icos + cy;

            if (sx < 0 || sy < 0 || sx > w - 1 || sy > h - 1)
            {
                b.SetPixelArgb(x, y, bgPacked);
                continue;
            }

            var c = mode switch
            {
                ResampleMode.Nearest => SampleNearest(a, sx, sy),
                ResampleMode.Bilinear => SampleBilinear(a, sx, sy),
                ResampleMode.Bicubic => SampleBicubic(a, sx, sy),
                _ => SampleBilinear(a, sx, sy)
            };
            b.SetPixelArgb(x, y, c);
        }

        return dst;
    }

    private static Bitmap Clone32(Bitmap src)
    {
        return src.Clone(new Rectangle(0, 0, src.Width, src.Height), PixelFormat.Format32bppArgb);
    }

    private static (float X, float Y) RotatePoint(float x, float y, float cos, float sin)
        => (x * cos - y * sin, x * sin + y * cos);

    private static uint SampleNearest(BitmapAccess src, float x, float y)
    {
        var ix = (int)Math.Round(x);
        var iy = (int)Math.Round(y);
        ix = Math.Clamp(ix, 0, src.Width - 1);
        iy = Math.Clamp(iy, 0, src.Height - 1);
        return src.GetPixelArgb(ix, iy);
    }

    private static uint SampleBilinear(BitmapAccess src, float x, float y)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        var tx = x - x0;
        var ty = y - y0;

        x0 = Math.Clamp(x0, 0, src.Width - 1);
        x1 = Math.Clamp(x1, 0, src.Width - 1);
        y0 = Math.Clamp(y0, 0, src.Height - 1);
        y1 = Math.Clamp(y1, 0, src.Height - 1);

        var c00 = src.GetPixelArgb(x0, y0);
        var c10 = src.GetPixelArgb(x1, y0);
        var c01 = src.GetPixelArgb(x0, y1);
        var c11 = src.GetPixelArgb(x1, y1);

        return Lerp2(c00, c10, c01, c11, tx, ty);
    }

    private static uint SampleBicubic(BitmapAccess src, float x, float y)
    {
        // Keys cubic kernel with a = -0.5 (Catmull-Rom like)
        const float a = -0.5f;

        var ix = (int)Math.Floor(x);
        var iy = (int)Math.Floor(y);
        var fx = x - ix;
        var fy = y - iy;

        float Cubic(float t)
        {
            t = Math.Abs(t);
            if (t <= 1f) return (a + 2) * t * t * t - (a + 3) * t * t + 1;
            if (t < 2f) return a * t * t * t - 5 * a * t * t + 8 * a * t - 4 * a;
            return 0f;
        }

        float wsumA = 0, wsumR = 0, wsumG = 0, wsumB = 0;
        float wsum = 0;

        for (var m = -1; m <= 2; m++)
        for (var n = -1; n <= 2; n++)
        {
            var px = Math.Clamp(ix + n, 0, src.Width - 1);
            var py = Math.Clamp(iy + m, 0, src.Height - 1);

            var wx = Cubic(n - fx);
            var wy = Cubic(m - fy);
            var w = wx * wy;

            var c = src.GetPixelArgb(px, py);
            BitmapAccess.UnpackArgb(c, out var aa, out var rr, out var gg, out var bb);

            wsumA += aa * w;
            wsumR += rr * w;
            wsumG += gg * w;
            wsumB += bb * w;
            wsum += w;
        }

        if (Math.Abs(wsum) < 1e-6f) return SampleNearest(src, x, y);

        var A = (byte)Math.Clamp((int)Math.Round(wsumA / wsum), 0, 255);
        var R = (byte)Math.Clamp((int)Math.Round(wsumR / wsum), 0, 255);
        var G = (byte)Math.Clamp((int)Math.Round(wsumG / wsum), 0, 255);
        var B = (byte)Math.Clamp((int)Math.Round(wsumB / wsum), 0, 255);
        return BitmapAccess.PackArgb(A, R, G, B);
    }

    private static uint Lerp2(uint c00, uint c10, uint c01, uint c11, float tx, float ty)
    {
        BitmapAccess.UnpackArgb(c00, out var a00, out var r00, out var g00, out var b00);
        BitmapAccess.UnpackArgb(c10, out var a10, out var r10, out var g10, out var b10);
        BitmapAccess.UnpackArgb(c01, out var a01, out var r01, out var g01, out var b01);
        BitmapAccess.UnpackArgb(c11, out var a11, out var r11, out var g11, out var b11);

        static float L(float v0, float v1, float t) => v0 + (v1 - v0) * t;

        var a0 = L(a00, a10, tx);
        var r0 = L(r00, r10, tx);
        var g0 = L(g00, g10, tx);
        var b0 = L(b00, b10, tx);

        var a1 = L(a01, a11, tx);
        var r1 = L(r01, r11, tx);
        var g1 = L(g01, g11, tx);
        var b1 = L(b01, b11, tx);

        var a = L(a0, a1, ty);
        var r = L(r0, r1, ty);
        var g = L(g0, g1, ty);
        var b = L(b0, b1, ty);

        return BitmapAccess.PackArgb(
            (byte)Math.Clamp((int)Math.Round(a), 0, 255),
            (byte)Math.Clamp((int)Math.Round(r), 0, 255),
            (byte)Math.Clamp((int)Math.Round(g), 0, 255),
            (byte)Math.Clamp((int)Math.Round(b), 0, 255)
        );
    }
}

