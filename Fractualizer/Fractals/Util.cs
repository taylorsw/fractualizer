using System;
using SharpDX;
using SharpDX.Direct3D11;

namespace Fractals
{
    public static class Util
    {
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

        public static double Abs(double val) => Math.Abs(val);

        public static Vector3d Abs(Vector3d v) => new Vector3d(Math.Abs(v.x), Math.Abs(v.y), Math.Abs(v.z));

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

        public static void UpdateBuffer<T>(Device device, DeviceContext deviceContext, SharpDX.Direct3D11.Buffer buffer, ref T t) where T : struct
        {
            DataStream dataStream;
            deviceContext.MapSubresource(buffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
            dataStream.Write(t);
            dataStream.Dispose();
            deviceContext.UnmapSubresource(buffer, 0);
        }
    }
}
