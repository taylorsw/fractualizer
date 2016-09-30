using System;
using SharpDX;

namespace Fractals
{
    public abstract class Fractal3d
    {
        public abstract double DuEstimate(Vector3 pt);
    }

    public class Mandelbrot : Fractal3d
    {
        public override double DuEstimate(Vector3 pt) => 1;
    }

    public class Tetrahedron : Fractal3d
    {
        public override double DuEstimate(Vector3 pt)
        {
            return 1;
        }
    }

    public class Mandelbox : Fractal3d
    {
        public override double DuEstimate(Vector3 pt)
        {
            return 1;
        }
    }

    public class Mandelbulb : Fractal3d
    {
        public override double DuEstimate(Vector3 pt)
        {
            double power = 8;
            int iterations = 10;
            double bailout = 5;

            Vector3 z = pt;
            double dr = 1;
            double r = 0;
            for (int i = 0; i < iterations; i++)
            {
                r = z.Length();
                if (r > bailout)
                    break;

                // convert to polar coordinates
                double theta = Math.Acos(z.Z / r);
                double phi = Math.Atan2(z.Y, z.X);
                dr = Math.Pow(r, power - 1f) * power * dr + 1f;

                // scale and rotate the point
                double zr = Math.Pow(r, power);
                theta = theta * power;
                phi = phi * power;

                // convert back to cartesian coordinates
                z = (float)zr * new Vector3(
                    (float)(Math.Sin(theta) * Math.Cos(phi)),
                    (float)(Math.Sin(phi) * Math.Sin(theta)),
                    (float)(Math.Cos(theta)));

                z += pt;
            }
            return 0.5 * Math.Log(r) * r / dr;
        }
    }
}
