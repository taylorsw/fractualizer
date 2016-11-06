using System;
using SharpDX;
using SharpDX.Direct3D11;

namespace Fractals
{
    public static class Util
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

        public static void UpdateBuffer<T>(Device device, DeviceContext deviceContext, SharpDX.Direct3D11.Buffer buffer, ref T t) where T : struct
        {
            DataStream dataStream;
            deviceContext.MapSubresource(buffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
            dataStream.Write(t);
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

        public static bool IsOrthogonalTo(this Vector3 v, Vector3 v2)
        {
            return Math.Abs(Vector3.Dot(v, v2)) < 0.0001f;
        }

        public static bool IsFinite(this Vector3 vk)
        {
            return !float.IsInfinity(vk.X) && !float.IsInfinity(vk.Y) && !float.IsInfinity(vk.Z)
                   && !float.IsNaN(vk.X) && !float.IsNaN(vk.Y) && !float.IsNaN(vk.Z);
        }
    }
}
