using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RasterEditor.WinFormsDemo.Core
{
    public static class LayerCompositor
    {
        // Compose layers (bottom-to-top). Each layer has OffsetX/OffsetY in destination coords.
        public static Bitmap Compose(int width, int height, IReadOnlyList<Layer> layers)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("Invalid target size");

            var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, width, height);

            var dstData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int dstRawStride = dstData.Stride;
                int dstStride = Math.Abs(dstRawStride);
                var dstScan0 = dstRawStride < 0 ? IntPtr.Add(dstData.Scan0, (height - 1) * dstRawStride) : dstData.Scan0;

                var dstBytes = new byte[dstStride * height]; // initialized to 0 = transparent

                // iterate layers bottom -> top
                for (int li = 0; li < layers.Count; li++)
                {
                    var layer = layers[li];
                    if (layer is null) continue;
                    if (!layer.IsVisible) continue;
                    if (layer.Bitmap is null) continue;

                    int ox = layer.OffsetX;
                    int oy = layer.OffsetY;
                    int sw = layer.Width;
                    int sh = layer.Height;

                    // compute intersection in destination coords
                    int dstX0 = Math.Max(0, ox);
                    int dstY0 = Math.Max(0, oy);
                    int dstX1 = Math.Min(width, ox + sw);
                    int dstY1 = Math.Min(height, oy + sh);
                    if (dstX0 >= dstX1 || dstY0 >= dstY1) continue;

                    int w = dstX1 - dstX0;
                    int h = dstY1 - dstY0;

                    var srcRect = new Rectangle(dstX0 - ox, dstY0 - oy, w, h);

                    var srcData = layer.Bitmap.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        int srcRawStride = srcData.Stride;
                        int srcStride = Math.Abs(srcRawStride);
                        var srcBytes = new byte[srcStride * h];
                        for (int yy = 0; yy < h; yy++)
                        {
                            int srcOffset = srcRawStride >= 0 ? yy * srcRawStride : (h - 1 - yy) * srcStride;
                            Marshal.Copy(IntPtr.Add(srcData.Scan0, srcOffset), srcBytes, yy * srcStride, srcStride);
                        }

                        float layerOpacity = Math.Clamp(layer.Opacity / 255f, 0f, 1f);

                        for (int yy = 0; yy < h; yy++)
                        {
                            int dstRow = (dstY0 + yy) * dstStride;
                            int srcRow = yy * srcStride;
                            for (int xx = 0; xx < w; xx++)
                            {
                                int di = dstRow + (dstX0 + xx) * 4;
                                int si = srcRow + xx * 4;

                                byte sb = srcBytes[si + 0];
                                byte sg = srcBytes[si + 1];
                                byte sr = srcBytes[si + 2];
                                byte sa = srcBytes[si + 3];

                                float sA = (sa / 255f) * layerOpacity;
                                if (sA <= 0f) continue;

                                float dB = dstBytes[di + 0] / 255f;
                                float dG = dstBytes[di + 1] / 255f;
                                float dR = dstBytes[di + 2] / 255f;
                                float dA = dstBytes[di + 3] / 255f;

                                float sR = sr / 255f, sG = sg / 255f, sB = sb / 255f;

                                float blendR = BlendChannel(sR, dR, layer.BlendMode);
                                float blendG = BlendChannel(sG, dG, layer.BlendMode);
                                float blendB = BlendChannel(sB, dB, layer.BlendMode);

                                // W3C "simple alpha compositing" + blend mode compositing.
                                float outA = sA + dA * (1f - sA);
                                float outR = (1f - sA) * dR + sA * ((1f - dA) * sR + dA * blendR);
                                float outG = (1f - sA) * dG + sA * ((1f - dA) * sG + dA * blendG);
                                float outB = (1f - sA) * dB + sA * ((1f - dA) * sB + dA * blendB);

                                dstBytes[di + 0] = (byte)(Clamp01(outB) * 255f);
                                dstBytes[di + 1] = (byte)(Clamp01(outG) * 255f);
                                dstBytes[di + 2] = (byte)(Clamp01(outR) * 255f);
                                dstBytes[di + 3] = (byte)(Clamp01(outA) * 255f);
                            }
                        }
                    }
                    finally
                    {
                        layer.Bitmap.UnlockBits(srcData);
                    }
                }

                // copy result buffer to bitmap memory
                Marshal.Copy(dstBytes, 0, dstScan0, dstBytes.Length);
            }
            finally
            {
                result.UnlockBits(dstData);
            }

            return result;
        }

        private static float BlendChannel(float s, float d, BlendMode mode)
        {
            return mode switch
            {
                BlendMode.Copy => s,
                BlendMode.Multiply => s * d,
                BlendMode.Screen => 1f - ((1f - s) * (1f - d)),
                BlendMode.Overlay => d < 0.5f ? (2f * s * d) : (1f - 2f * (1f - s) * (1f - d)),
                BlendMode.Add => s + d,
                BlendMode.Subtract => d - s,
                BlendMode.Difference => Math.Abs(d - s),
                BlendMode.Exclusion => d + s - (2f * d * s),
                BlendMode.Lighten => Math.Max(s, d),
                BlendMode.Darken => Math.Min(s, d),
                BlendMode.HardLight => s < 0.5f ? (2f * s * d) : (1f - 2f * (1f - s) * (1f - d)),
                BlendMode.SoftLight => SoftLight(s, d),
                _ => s, // Normal
            };
        }

        private static float SoftLight(float s, float d)
        {
            if (s <= 0.5f)
            {
                return d - ((1f - 2f * s) * d * (1f - d));
            }

            float g = d <= 0.25f
                ? (((16f * d - 12f) * d + 4f) * d)
                : MathF.Sqrt(d);

            return d + (2f * s - 1f) * (g - d);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
