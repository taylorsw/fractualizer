using System;
using SharpDX;

namespace Fractals
{
    public abstract class Fractal3d
    {
        // todo make this not virtual
        public virtual string StShaderPath()
        {
            return "Shaders/" + GetType().Name + ".hlsl";
        }

        public abstract double DuEstimate(Vector3d pt);
    }

    public class Tetrahedron : Fractal3d
    {
        public override string StShaderPath()
        {
            return "ShadersKludge/" + GetType().Name + ".hlsl";
        }

        public override double DuEstimate(Vector3d pt)
        {
            return 1;
        }
    }
}
