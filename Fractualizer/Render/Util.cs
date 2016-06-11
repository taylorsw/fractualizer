using System;
using SharpDX;

namespace Render
{
    public static class Util
    {
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

        public static float Saturate(float x) => Math.Max(0, Math.Min(1, x));
    }
}
