using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;

namespace Util
{
    public static class U
    {
        public static double Lerp(double x, double y, double s) => x*(1 - s) + y*s;

        public static Vector3d Lerp(Vector3d x, Vector3d y, double s)
            => new Vector3d(Lerp(x.x, y.x, s), Lerp(x.y, y.y, s), Lerp(x.z, y.z, s));

        public static double Frac(double d) => Math.Abs(d - Math.Truncate(d));

        public static Vector3d Frac(Vector3d v) => new Vector3d(Frac(v.x), Frac(v.y), Frac(v.z));

        public static double Floor(double d) => Math.Floor(d);

        public static Vector3d Floor(Vector3d val) => new Vector3d(Math.Floor(val.x), Math.Floor(val.y), Math.Floor(val.z));

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable
        {
            if (val.CompareTo(min) < 0)
                return min;
            if (val.CompareTo(max) > 0)
                return max;
            return val;
        }

        public static Vector3d Clamp(Vector3d val, Vector3d min, Vector3d max)
        {
            return new Vector3d(Clamp(val.x, min.x, max.x), Clamp(val.y, min.y, max.y), Clamp(val.z, min.z, max.z));
        }

        public static Vector3d Clamp(Vector3d val, double min, double max)
        {
            return new Vector3d(Clamp(val.x, min, max), Clamp(val.y, min, max), Clamp(val.z, min, max));
        }
        
        public static Vector3d Abs(Vector3d v) => new Vector3d(Math.Abs(v.x), Math.Abs(v.y), Math.Abs(v.z));

        public static double Abs(double val) => Math.Abs(val);

        public static double Atan(double x)
        {
            const double halfPi = Math.PI / 2;

            if (double.IsNaN(x))
                return 0;

            if (double.IsPositiveInfinity(x))
                return halfPi;

            if (double.IsNegativeInfinity(x))
                return -halfPi;

            return Math.Atan(x);
        }

        public static SharpDX.Direct3D11.Buffer BufferCreate<T>(Device device, DeviceContext deviceContext, int ibuffer, ref T t) where T : struct
        {
            var buffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref t, usage: ResourceUsage.Dynamic, accessFlags: CpuAccessFlags.Write);
            deviceContext.PixelShader.SetConstantBuffer(ibuffer, buffer);
            return buffer;
        }

        public static SharpDX.Direct3D11.Buffer BufferCreate(Device device, DeviceContext deviceContext, int ibuffer, byte[] rgbyte)
        {
            BufferDescription bufferDesc = new BufferDescription(
                sizeInBytes: rgbyte.Length,
                bindFlags: BindFlags.ConstantBuffer, 
                usage: ResourceUsage.Dynamic, 
                cpuAccessFlags: CpuAccessFlags.Write,
                optionFlags: ResourceOptionFlags.None, 
                structureByteStride: 0);
            var buffer = SharpDX.Direct3D11.Buffer.Create(device, rgbyte, bufferDesc);
            deviceContext.PixelShader.SetConstantBuffer(ibuffer, buffer);
            return buffer;
        }

        public static void UpdateBuffer<T>(Device device, DeviceContext deviceContext, SharpDX.Direct3D11.Buffer buffer, ref T t) where T : struct
        {
            DataStream dataStream;
            deviceContext.MapSubresource(buffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
            dataStream.Write(t);
            dataStream.Dispose();
            deviceContext.UnmapSubresource(buffer, 0);
        }

        public static void UpdateBuffer(Device device, DeviceContext deviceContext, SharpDX.Direct3D11.Buffer buffer, byte[] rgbyte)
        {
            DataStream dataStream;
            deviceContext.MapSubresource(buffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
            dataStream.Write(rgbyte, 0, rgbyte.Length);
            dataStream.Dispose();
            deviceContext.UnmapSubresource(buffer, 0);
        }

        public static Vector4 Cross3(this Vector4 left, Vector4 right) => new Vector4(Vector3.Cross(left.Xyz(), right.Xyz()), 1);

        public static Vector3 Normalized(this Vector3 v)
        {
            v.Normalize();
            return v;
        }

        public static Vector3 PerspectiveDivide(this Vector4 v) => v.Xyz() / v.W;
        public static Vector3 Xyz(this Vector4 v) => new Vector3(v.X, v.Y, v.Z);

        public static Vector3 VkUnitRand(this Random rand, float min = -1, float max = 1) => new Vector3(rand.NextFloat(min, max), rand.NextFloat(min, max), rand.NextFloat(min, max)).Normalized();

        public static bool IsOrthogonalTo(this Vector3 v, Vector3 v2)
        {
            return Math.Abs(Vector3.Dot(v, v2)) < 0.0001f;
        }

        public static bool IsFinite(this Vector3 vk)
        {
            return !float.IsInfinity(vk.X) && !float.IsInfinity(vk.Y) && !float.IsInfinity(vk.Z)
                   && !float.IsNaN(vk.X) && !float.IsNaN(vk.Y) && !float.IsNaN(vk.Z);
        }

        public static int RoundToByteOffset(int cybte, int cbyteOffset = 16)
        {
            return cybte + (cbyteOffset - cybte % cbyteOffset) % cbyteOffset;
        }
    }

    public static class ColorU
    {
        public static readonly Vector3 rgbWhite = new Vector3(1, 1, 1);
    }

    public class PaddedArray<T> where T : struct
    {
        private Action dgDirty;
        private readonly byte[] rgbyte;
        private readonly int ibyteStart, cbyteTWithPad;
        public readonly int cvalArray;

        public PaddedArray(byte[] rgbyte, int ibyteStart, int cvalArray, Action dgDirty = null, int cbytePaddedTo = 16)
        {
            this.rgbyte = rgbyte;
            this.ibyteStart = ibyteStart;
            this.cvalArray = cvalArray;
            this.cbyteTWithPad = U.RoundToByteOffset(Marshal.SizeOf(typeof(T)), cbytePaddedTo);
        }

        public void CopyValues(T[] rgval, int ivalStart, int cval = int.MinValue)
        {
            if (cval == int.MinValue)
                cval = cvalArray;

            if (cval > cvalArray || ivalStart < 0 || ivalStart >= rgval.Length)
                throw new ArgumentException();

            for (int ival = ivalStart; ival < ivalStart + cval; ival++)
                this[ival - ivalStart] = rgval[ival];
        }

        private int IbyteIndex(int ival)
        {
            if (ival >= cvalArray || ival < 0)
                throw new IndexOutOfRangeException();
            return ibyteStart + cbyteTWithPad * ival;
        }

        public T this[int ival]
        {
            get { return ValFromRgbyte(rgbyte, IbyteIndex(ival)); }
            set { dgDirty?.Invoke(); SetVal(rgbyte, IbyteIndex(ival), value); }
        }

        public static T ValFromRgbyte(byte[] rgbyte, int ibyteOffset)
        {
            unsafe
            {
                fixed (byte* p = &rgbyte[ibyteOffset])
                {
                    return (T)Marshal.PtrToStructure(new IntPtr(p), typeof(T));
                }
            }
        }

        public static void SetVal(byte[] rgbyte, int ibyteOffset, T val)
        {
            unsafe
            {
                fixed (byte* p = &rgbyte[ibyteOffset])
                {
                    Marshal.StructureToPtr(val, new IntPtr(p), true);
                }
            }
        }
    }

    public class TextureLoader
    {
        /// <summary>
        /// Loads a bitmap using WIC.
        /// </summary>
        /// <param name="deviceManager"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static SharpDX.WIC.BitmapSource LoadBitmap(SharpDX.WIC.ImagingFactory2 factory, string filename)
        {
            var bitmapDecoder = new SharpDX.WIC.BitmapDecoder(
                factory,
                filename,
                SharpDX.WIC.DecodeOptions.CacheOnDemand
                );

            var formatConverter = new SharpDX.WIC.FormatConverter(factory);

            formatConverter.Initialize(
                bitmapDecoder.GetFrame(0),
                SharpDX.WIC.PixelFormat.Format32bppPRGBA,
                SharpDX.WIC.BitmapDitherType.None,
                null,
                0.0,
                SharpDX.WIC.BitmapPaletteType.Custom);

            return formatConverter;
        }

        /// <summary>
        /// Creates a <see cref="SharpDX.Direct3D11.Texture2D"/> from a WIC <see cref="SharpDX.WIC.BitmapSource"/>
        /// </summary>
        /// <param name="device">The Direct3D11 device</param>
        /// <param name="bitmapSource">The WIC bitmap source</param>
        /// <returns>A Texture2D</returns>
        public static SharpDX.Direct3D11.Texture2D CreateTexture2DFromBitmap(SharpDX.Direct3D11.Device device, SharpDX.WIC.BitmapSource bitmapSource)
        {
            // Allocate DataStream to receive the WIC image pixels
            int stride = bitmapSource.Size.Width * 4;
            using (var buffer = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true))
            {
                // Copy the content of the WIC to the buffer
                bitmapSource.CopyPixels(stride, buffer);
                return new SharpDX.Direct3D11.Texture2D(device, new SharpDX.Direct3D11.Texture2DDescription()
                {
                    Width = bitmapSource.Size.Width,
                    Height = bitmapSource.Size.Height,
                    ArraySize = 1,
                    BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource,
                    Usage = SharpDX.Direct3D11.ResourceUsage.Immutable,
                    CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                }, new SharpDX.DataRectangle(buffer.DataPointer, stride));
            }
        }
    }
}
