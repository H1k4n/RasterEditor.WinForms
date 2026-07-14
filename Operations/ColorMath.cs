namespace RasterEditor.WinFormsDemo.Operations;

internal static class ColorMath
{
    public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    public static void RgbToHsl(byte r8, byte g8, byte b8, out float h, out float s, out float l)
    {
        var r = r8 / 255f;
        var g = g8 / 255f;
        var b = b8 / 255f;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        l = (max + min) * 0.5f;

        if (delta <= 1e-6f)
        {
            h = 0f;
            s = 0f;
            return;
        }

        s = delta / (1f - Math.Abs(2f * l - 1f));

        if (Math.Abs(max - r) <= 1e-6f)
            h = ((g - b) / delta) % 6f;
        else if (Math.Abs(max - g) <= 1e-6f)
            h = ((b - r) / delta) + 2f;
        else
            h = ((r - g) / delta) + 4f;

        h *= 60f;
        if (h < 0f) h += 360f;
    }

    public static void HslToRgb(float h, float s, float l, out byte r8, out byte g8, out byte b8)
    {
        h = ((h % 360f) + 360f) % 360f;
        s = Clamp01(s);
        l = Clamp01(l);

        var c = (1f - Math.Abs(2f * l - 1f)) * s;
        var x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
        var m = l - c * 0.5f;

        float r1, g1, b1;
        if (h < 60f) { r1 = c; g1 = x; b1 = 0f; }
        else if (h < 120f) { r1 = x; g1 = c; b1 = 0f; }
        else if (h < 180f) { r1 = 0f; g1 = c; b1 = x; }
        else if (h < 240f) { r1 = 0f; g1 = x; b1 = c; }
        else if (h < 300f) { r1 = x; g1 = 0f; b1 = c; }
        else { r1 = c; g1 = 0f; b1 = x; }

        var r = (r1 + m) * 255f;
        var g = (g1 + m) * 255f;
        var b = (b1 + m) * 255f;

        r8 = (byte)Math.Clamp((int)Math.Round(r), 0, 255);
        g8 = (byte)Math.Clamp((int)Math.Round(g), 0, 255);
        b8 = (byte)Math.Clamp((int)Math.Round(b), 0, 255);
    }
}

