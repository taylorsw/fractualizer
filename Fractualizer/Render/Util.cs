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
        public static Vector4 Cross3(this Vector4 left, Vector4 right)
        {
            return new Vector4(Vector3.Cross(new Vector3(left.X, left.Y, left.Z), new Vector3(right.X, right.Y, right.Z)), 1);
        }
    }
}
