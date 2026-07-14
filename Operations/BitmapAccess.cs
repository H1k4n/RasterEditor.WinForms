using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RasterEditor.WinFormsDemo.Operations
{
    internal sealed class BitmapAccess : IDisposable
    {
        private readonly Bitmap _bitmap;
        private BitmapData? _data;
        private IntPtr _scan0;

        public int Width { get; }
        public int Height { get; }
        public int Stride { get; private set; }

        public BitmapAccess(Bitmap bitmap, ImageLockMode mode)
        {
            _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
            Width = bitmap.Width;
            Height = bitmap.Height;

            _data = bitmap.LockBits(new Rectangle(0, 0, Width, Height), mode, PixelFormat.Format32bppArgb);
            int rawStride = _data.Stride;
            if (rawStride < 0)
            {
                _scan0 = IntPtr.Add(_data.Scan0, (Height - 1) * rawStride);
                Stride = -rawStride;
            }
            else
            {
                _scan0 = _data.Scan0;
                Stride = rawStride;
            }
        }

        public void Dispose()
        {
            if (_data is not null)
            {
                _bitmap.UnlockBits(_data);
                _data = null;
            }

            _scan0 = IntPtr.Zero;
        }

        public uint GetPixelArgb(int x, int y)
        {
            if (_data is null || _scan0 == IntPtr.Zero) throw new ObjectDisposedException(nameof(BitmapAccess));
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(x >= Width ? nameof(x) : nameof(y));

            int offset = y * Stride + x * 4;
            int b = Marshal.ReadByte(_scan0, offset + 0);
            int g = Marshal.ReadByte(_scan0, offset + 1);
            int r = Marshal.ReadByte(_scan0, offset + 2);
            int a = Marshal.ReadByte(_scan0, offset + 3);
            return (uint)(b | (g << 8) | (r << 16) | (a << 24)); // BGRA
        }

        public void SetPixelArgb(int x, int y, uint argb)
        {
            if (_data is null || _scan0 == IntPtr.Zero) throw new ObjectDisposedException(nameof(BitmapAccess));
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(x >= Width ? nameof(x) : nameof(y));

            int offset = y * Stride + x * 4;
            Marshal.WriteByte(_scan0, offset + 0, (byte)(argb & 0xFF));         // B
            Marshal.WriteByte(_scan0, offset + 1, (byte)((argb >> 8) & 0xFF));  // G
            Marshal.WriteByte(_scan0, offset + 2, (byte)((argb >> 16) & 0xFF)); // R
            Marshal.WriteByte(_scan0, offset + 3, (byte)((argb >> 24) & 0xFF)); // A
        }

        public static byte ClampToByte(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);

        public static uint PackArgb(byte a, byte r, byte g, byte b)
            => (uint)(b | (g << 8) | (r << 16) | (a << 24));

        public static void UnpackArgb(uint argb, out byte a, out byte r, out byte g, out byte b)
        {
            b = (byte)(argb & 0xFF);
            g = (byte)((argb >> 8) & 0xFF);
            r = (byte)((argb >> 16) & 0xFF);
            a = (byte)((argb >> 24) & 0xFF);
        }
    }
}
