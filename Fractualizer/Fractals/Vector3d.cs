using System;
using SharpDX;

namespace Fractals
{
    public struct Vector3d
    {
        public double x, y, z;

        public Vector3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3d(Vector3 v) => new Vector3d(v.X, v.Y, v.Z);
        public static Vector3d operator +(Vector3d v1, Vector3d v2) => new Vector3d(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        public static Vector3d operator -(Vector3d v1, Vector3d v2) => v1 + (-v2);
        public static Vector3d operator -(Vector3d v1) => new Vector3d(-v1.x, -v1.y, -v1.z);
        public static Vector3d operator *(double sf, Vector3d v) => new Vector3d(sf * v.x, sf * v.y, sf * v.z);
        public static Vector3d operator *(Vector3d v, double sf) => new Vector3d(sf * v.x, sf * v.y, sf * v.z);
        public static Vector3d operator *(Vector3d v1, Vector3d v2) => new Vector3d(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);

        public static double Dot(Vector3d v1, Vector3d v2) => v1.x*v2.x + v1.y*v2.y + v1.z*v2.z;
        public static double Length(Vector3d v) => Math.Sqrt(Length2(v));
        public static double Length2(Vector3d v) => Dot(v, v);        
    }

    public struct Vector4d
    {
        public double x, y, z, w;

        public Vector4d(double x, double y, double z, double w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Vector4d(Vector3d v, double w)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
            this.w = w;
        }

        public static Vector4d operator +(Vector4d v1, Vector4d v2) => new Vector4d(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w);
        public static Vector4d operator *(double sf, Vector4d v) => new Vector4d(sf * v.x, sf * v.y, sf * v.z, sf * v.w);
        public static Vector4d operator *(Vector4d v, double sf) => new Vector4d(sf * v.x, sf * v.y, sf * v.z, sf * v.w);

        public static double Dot(Vector4d v1, Vector4d v2) => v1.x * v2.x + v1.y * v2.y + v1.z * v2.z + v1.w * v2.w;
        public static double Length(Vector4d v) => Math.Sqrt(Length2(v));
        public static double Length2(Vector4d v) => Dot(v, v);
    }
}
