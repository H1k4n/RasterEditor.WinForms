using System.Drawing.Imaging;

namespace RasterEditor.WinFormsDemo.Operations;

public static class Filters
{
    public static Bitmap GaussianBlur(Bitmap src, int radius, float? sigma = null)
    {
        if (src is null) throw new ArgumentNullException(nameof(src));
        radius = Math.Clamp(radius, 1, 50);
        var s = sigma ?? (radius / 3f);
        s = Math.Max(0.01f, s);

        var kernel = BuildGaussianKernel1D(radius, s);
        var tmp = Convolve1D(src, kernel, horizontal: true);
        try
        {
            var dst = Convolve1D(tmp, kernel, horizontal: false);
            return dst;
        }
        finally
        {
            tmp.Dispose();
        }
    }

    public static Bitmap Laplace(Bitmap src)
    {
        if (src is null) throw new ArgumentNullException(nameof(src));
        // 3x3 Laplacian (4-neighborhood)
        var k = new int[,]
        {
            { 0, -1,  0 },
            { -1, 4, -1 },
            { 0, -1,  0 }
        };
        return Convolve3x3(src, k, bias: 128);
    }

    public static Bitmap UnsharpMask(Bitmap src, int radius, float amount, float? sigma = null)
    {
        if (src is null) throw new ArgumentNullException(nameof(src));
        amount = Math.Clamp(amount, 0f, 5f);

        using var blurred = GaussianBlur(src, radius, sigma);
        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);

        using var a = new BitmapAccess(src, ImageLockMode.ReadOnly);
        using var b = new BitmapAccess(blurred, ImageLockMode.ReadOnly);
        using var d = new BitmapAccess(dst, ImageLockMode.WriteOnly);

        for (var y = 0; y < a.Height; y++)
        for (var x = 0; x < a.Width; x++)
        {
            BitmapAccess.UnpackArgb(a.GetPixelArgb(x, y), out var aa, out var ar, out var ag, out var ab);
            BitmapAccess.UnpackArgb(b.GetPixelArgb(x, y), out var _, out var br, out var bg, out var bb);

            var rr = (int)Math.Round(ar + amount * (ar - br));
            var gg = (int)Math.Round(ag + amount * (ag - bg));
            var bb2 = (int)Math.Round(ab + amount * (ab - bb));

            d.SetPixelArgb(x, y, BitmapAccess.PackArgb(
                aa,
                BitmapAccess.ClampToByte(rr),
                BitmapAccess.ClampToByte(gg),
                BitmapAccess.ClampToByte(bb2)
            ));
        }

        return dst;
    }

    public static Bitmap Median(Bitmap src, int radius)
    {
        if (src is null) throw new ArgumentNullException(nameof(src));
        radius = Math.Clamp(radius, 1, 10);
        var size = radius * 2 + 1;
        var window = size * size;

        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using var a = new BitmapAccess(src, ImageLockMode.ReadOnly);
        using var d = new BitmapAccess(dst, ImageLockMode.WriteOnly);

        var rs = new byte[window];
        var gs = new byte[window];
        var bs = new byte[window];
        var alphas = new byte[window];

        for (var y = 0; y < a.Height; y++)
        for (var x = 0; x < a.Width; x++)
        {
            var idx = 0;
            for (var dy = -radius; dy <= radius; dy++)
            for (var dx = -radius; dx <= radius; dx++)
            {
                var px = Math.Clamp(x + dx, 0, a.Width - 1);
                var py = Math.Clamp(y + dy, 0, a.Height - 1);
                BitmapAccess.UnpackArgb(a.GetPixelArgb(px, py), out var aa, out var rr, out var gg, out var bb);
                alphas[idx] = aa;
                rs[idx] = rr;
                gs[idx] = gg;
                bs[idx] = bb;
                idx++;
            }

            Array.Sort(alphas);
            Array.Sort(rs);
            Array.Sort(gs);
            Array.Sort(bs);

            var mid = window / 2;
            d.SetPixelArgb(x, y, BitmapAccess.PackArgb(alphas[mid], rs[mid], gs[mid], bs[mid]));
        }

        return dst;
    }

    private static float[] BuildGaussianKernel1D(int radius, float sigma)
    {
        var size = radius * 2 + 1;
        var k = new float[size];
        var s2 = 2f * sigma * sigma;
        float sum = 0f;
        for (var i = -radius; i <= radius; i++)
        {
            var v = (float)Math.Exp(-(i * i) / s2);
            k[i + radius] = v;
            sum += v;
        }
        for (var i = 0; i < size; i++) k[i] /= sum;
        return k;
    }

    private static Bitmap Convolve1D(Bitmap src, float[] kernel, bool horizontal)
    {
        var radius = kernel.Length / 2;
        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using var a = new BitmapAccess(src, ImageLockMode.ReadOnly);
        using var d = new BitmapAccess(dst, ImageLockMode.WriteOnly);

        for (var y = 0; y < a.Height; y++)
        for (var x = 0; x < a.Width; x++)
        {
            float sa = 0, sr = 0, sg = 0, sb = 0;
            for (var k = -radius; k <= radius; k++)
            {
                var px = horizontal ? Math.Clamp(x + k, 0, a.Width - 1) : x;
                var py = horizontal ? y : Math.Clamp(y + k, 0, a.Height - 1);
                var w = kernel[k + radius];
                BitmapAccess.UnpackArgb(a.GetPixelArgb(px, py), out var aa, out var rr, out var gg, out var bb);
                sa += aa * w;
                sr += rr * w;
                sg += gg * w;
                sb += bb * w;
            }

            d.SetPixelArgb(x, y, BitmapAccess.PackArgb(
                (byte)Math.Clamp((int)Math.Round(sa), 0, 255),
                (byte)Math.Clamp((int)Math.Round(sr), 0, 255),
                (byte)Math.Clamp((int)Math.Round(sg), 0, 255),
                (byte)Math.Clamp((int)Math.Round(sb), 0, 255)
            ));
        }

        return dst;
    }

    private static Bitmap Convolve3x3(Bitmap src, int[,] kernel, int bias = 0)
    {
        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using var a = new BitmapAccess(src, ImageLockMode.ReadOnly);
        using var d = new BitmapAccess(dst, ImageLockMode.WriteOnly);

        for (var y = 0; y < a.Height; y++)
        for (var x = 0; x < a.Width; x++)
        {
            int sr = 0, sg = 0, sb = 0;
            byte aa0;
            {
                BitmapAccess.UnpackArgb(a.GetPixelArgb(x, y), out var aa, out _, out _, out _);
                aa0 = aa;
            }

            for (var ky = -1; ky <= 1; ky++)
            for (var kx = -1; kx <= 1; kx++)
            {
                var px = Math.Clamp(x + kx, 0, a.Width - 1);
                var py = Math.Clamp(y + ky, 0, a.Height - 1);
                var w = kernel[ky + 1, kx + 1];
                BitmapAccess.UnpackArgb(a.GetPixelArgb(px, py), out _, out var rr, out var gg, out var bb);
                sr += w * rr;
                sg += w * gg;
                sb += w * bb;
            }

            sr += bias;
            sg += bias;
            sb += bias;

            d.SetPixelArgb(x, y, BitmapAccess.PackArgb(aa0, BitmapAccess.ClampToByte(sr), BitmapAccess.ClampToByte(sg), BitmapAccess.ClampToByte(sb)));
        }

        return dst;
    }
}

