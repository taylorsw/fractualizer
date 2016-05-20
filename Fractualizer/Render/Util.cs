using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace Render
{
    public static class Util
    {
        public static Vector4 Cross3(this Vector4 left, Vector4 right) => new Vector4(Vector3.Cross(left.Xyz(), right.Xyz()), 1);
        public static Vector3 Xyz(this Vector4 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
