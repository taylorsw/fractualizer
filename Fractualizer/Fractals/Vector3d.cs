using System;
using SharpDX;

namespace Fractals
{
    public static class VectorUtil
    {
        public static Vector2d Normalize(Vector2d v) => v / Vector2d.Length(v);
        public static Vector3d Normalize(Vector3d v) => v / Vector3d.Length(v);
    }

    public struct Vector2f
    {
        private Vector2 v;
        public float x => v.X;
        public float y => v.Y;

        public Vector2f(Vector2 v)
        {
            this.v = v;
        }

        public Vector2f(float x, float y)
        {
            this.v = new Vector2(x, y);
        }

        public static implicit operator Vector2f(Vector2 v) => new Vector2f(v);
        public static implicit operator float(Vector2f v) => v.x;
        public static implicit operator Vector2f(Vector3f v) => new Vector2f(v.x, v.y);

        public static Vector2f operator +(Vector2f v1, Vector2f v2) => new Vector2f(v1.v + v2.v);
        public static Vector2f operator -(Vector2f v1, Vector2f v2) => v1 + (-v2);
        public static Vector2f operator -(Vector2f v1) => new Vector2f(-v1.v);
        public static Vector2f operator *(float sf, Vector2f v) => new Vector2f(sf * v.v);
        public static Vector2f operator *(Vector2f v, float sf) => new Vector2f(sf * v.v);
        public static Vector2f operator *(Vector2f v1, Vector2f v2) => new Vector2f(v1.v * v2.v);
        public static Vector2f operator /(Vector2f v, float sf) => new Vector2f(v.v / sf);

        public static float Dot(Vector2f v1, Vector2f v2) => Vector2.Dot(v1.v, v2.v);
        public static float Length(Vector2f v) => v.v.Length();
        public static float Length2(Vector2f v) => Dot(v, v);
    }

    public struct Vector3f
    {
        private Vector3 v;
        public float x => v.X;
        public float y => v.Y;
        public float z => v.Z;

        public Vector3f(Vector3 v)
        {
            this.v = v;
        }

        public Vector3f(float x, float y, float z)
        {
            this.v = new Vector3(x, y, z);
        }

        public static implicit operator Vector3f(Vector3 v) => new Vector3f(v);
        public static implicit operator float(Vector3f v) => v.x;
        public static implicit operator Vector3f(Vector4f v) => new Vector3f(v.x, v.y, v.z);

        public static Vector3f operator +(Vector3f v1, Vector3f v2) => new Vector3f(v1.v + v2.v);
        public static Vector3f operator -(Vector3f v1, Vector3f v2) => v1 + (-v2);
        public static Vector3f operator -(Vector3f v1) => new Vector3f(-v1.v);
        public static Vector3f operator *(float sf, Vector3f v) => new Vector3f(sf * v.v);
        public static Vector3f operator *(Vector3f v, float sf) => new Vector3f(sf * v.v);
        public static Vector3f operator *(Vector3f v1, Vector3f v2) => new Vector3f(v1.v * v2.v);
        public static Vector3f operator /(Vector3f v, float sf) => new Vector3f(v.v / sf);

        public static float Dot(Vector3f v1, Vector3f v2) => Vector3f.Dot(v1.v, v2.v);
        public static float Length(Vector3f v) => v.v.Length();
        public static float Length2(Vector3f v) => Dot(v, v);
    }

    public struct Vector4f
    {
        private Vector4 v;
        public float x => v.X;
        public float y => v.Y;
        public float z => v.Z;
        public float w => v.W;

        public Vector4f(Vector4 v)
        {
            this.v = v;
        }

        public Vector4f(float x, float y, float z, float w)
        {
            this.v = new Vector4(x, y, z, w);
        }

        public static implicit operator Vector4f(Vector4 v) => new Vector4f(v);
        public static implicit operator float(Vector4f v) => v.x;

        public static Vector4f operator +(Vector4f v1, Vector4f v2) => new Vector4f(v1.v + v2.v);
        public static Vector4f operator -(Vector4f v1, Vector4f v2) => v1 + (-v2);
        public static Vector4f operator -(Vector4f v1) => new Vector4f(-v1.v);
        public static Vector4f operator *(float sf, Vector4f v) => new Vector4f(sf * v.v);
        public static Vector4f operator *(Vector4f v, float sf) => new Vector4f(sf * v.v);
        public static Vector4f operator *(Vector4f v1, Vector4f v2) => new Vector4f(v1.v * v2.v);
        public static Vector4f operator /(Vector4f v, float sf) => new Vector4f(v.v / sf);

        public static float Dot(Vector4f v1, Vector4f v2) => Vector4f.Dot(v1.v, v2.v);
        public static float Length(Vector4f v) => v.v.Length();
        public static float Length2(Vector4f v) => Dot(v, v);
    }

    public struct Vector2d
    {
        public double x, y;

        public Vector2d(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Vector2d(Vector2f v) => new Vector2d(v.x, v.y);
        public static implicit operator Vector2d(Vector2 v) => new Vector2d(v.X, v.Y);
        public static implicit operator double(Vector2d v) => v.x;
        public static implicit operator Vector2d(Vector3d v) => new Vector2d(v.x, v.y);

        public static Vector2d operator +(Vector2d v1, Vector2d v2) => new Vector2d(v1.x + v2.x, v1.y + v2.y);
        public static Vector2d operator -(Vector2d v1, Vector2d v2) => v1 + (-v2);
        public static Vector2d operator -(Vector2d v1) => new Vector2d(-v1.x, -v1.y);
        public static Vector2d operator *(double sf, Vector2d v) => new Vector2d(sf * v.x, sf * v.y);
        public static Vector2d operator *(Vector2d v, double sf) => sf * v;
        public static Vector2d operator *(Vector2d v1, Vector2d v2) => new Vector2d(v1.x * v2.x, v1.y * v2.y);
        public static Vector2d operator /(Vector2d v, double sf) => new Vector2d(v.x / sf, v.y / sf);

        public static double Dot(Vector2d v1, Vector2d v2) => v1.x * v2.x + v1.y * v2.y;
        public static double Length(Vector2d v) => Math.Sqrt(Length2(v));
        public static double Length2(Vector2d v) => Dot(v, v);
    }

    public struct Vector3d
    {
        public double x, y, z;

        public Vector3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3d(Vector3f v) => new Vector3d(v.x, v.y, v.z);
        public static implicit operator Vector3d(Vector3 v) => new Vector3d(v.X, v.Y, v.Z);
        public static implicit operator double(Vector3d v) => v.x;
        public static implicit operator Vector3d(Vector4d v) => new Vector3d(v.x, v.y, v.z);

        public static Vector3d operator +(Vector3d v1, Vector3d v2) => new Vector3d(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        public static Vector3d operator -(Vector3d v1, Vector3d v2) => v1 + (-v2);
        public static Vector3d operator -(Vector3d v1) => new Vector3d(-v1.x, -v1.y, -v1.z);
        public static Vector3d operator *(double sf, Vector3d v) => new Vector3d(sf * v.x, sf * v.y, sf * v.z);
        public static Vector3d operator *(Vector3d v, double sf) => sf * v;
        public static Vector3d operator *(Vector3d v1, Vector3d v2) => new Vector3d(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        public static Vector3d operator /(Vector3d v, double sf) => new Vector3d(v.x / sf, v.y / sf, v.z / sf);

        public static double Dot(Vector3d v1, Vector3d v2) => v1.x*v2.x + v1.y*v2.y + v1.z*v2.z;
        public static double Length(Vector3d v) => Math.Sqrt(Length2(v));
        public static double Length2(Vector3d v) => Dot(v, v);
        public static Vector3d Cross(Vector3d v1, Vector3d v2) => new Vector3d(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
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

        public static implicit operator Vector4d(Vector4f v) => new Vector4d(v.x, v.y, v.z, v.w);
        public static implicit operator Vector4d(Vector4 v) => new Vector4d(v.X, v.Y, v.Z, v.W);
        public static implicit operator double(Vector4d v) => v.x;

        public static Vector4d operator +(Vector4d v1, Vector4d v2) => new Vector4d(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w);
        public static Vector4d operator -(Vector4d v1, Vector4d v2) => v1 + (-v2);
        public static Vector4d operator -(Vector4d v1) => new Vector4d(-v1.x, -v1.y, -v1.z, -v1.w);
        public static Vector4d operator *(double sf, Vector4d v) => new Vector4d(sf * v.x, sf * v.y, sf * v.z, sf * v.w);
        public static Vector4d operator *(Vector4d v, double sf) => sf * v;
        public static Vector4d operator *(Vector4d v1, Vector4d v2) => new Vector4d(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z, v1.w * v2.w);

        public static double Dot(Vector4d v1, Vector4d v2) => v1.x * v2.x + v1.y * v2.y + v1.z * v2.z + v1.w * v2.w;
        public static double Length(Vector4d v) => Math.Sqrt(Length2(v));
        public static double Length2(Vector4d v) => Dot(v, v);
    }
}
