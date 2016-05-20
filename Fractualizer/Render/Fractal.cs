using System;
using SharpDX.D3DCompiler;
using D3D11 = SharpDX.Direct3D11;
using SharpDX;

namespace Render
{
    public abstract class Fractal : IDisposable
    {
        protected D3D11.PixelShader pixelShader;

        internal Fractal() { }

        public virtual void InitializeFractal(D3D11.Device d3dDevice, D3D11.DeviceContext deviceContext)
        {
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Shaders/" + StShader(), "main", "ps_4_0", ShaderFlags.Debug))
            {
                pixelShader = new D3D11.PixelShader(d3dDevice, pixelShaderByteCode);
            }

            deviceContext.PixelShader.Set(pixelShader);
        }

        public abstract string StShader();

        public void Dispose()
        {
            pixelShader.Dispose();
        }
    }

    public abstract class Fractal3d : Fractal
    {
        public abstract double DuEstimate(Vector3 pt);
    }

    public class Mandelbrot : Fractal
    {
        public override string StShader() => "mandelbrot.hlsl";
    }

    public class Mandelbox : Fractal3d
    {
        public override string StShader() => "mandelbox.hlsl";

        public override double DuEstimate(Vector3 pt)
        {
            const float scale = 9;
            Vector3 boxfold = new Vector3(1, 1, 1);
            const float spherefold = 0.2f;

            Vector4 c0 = new Vector4(pt, 1);
            Vector3 c = c0.Xyz();
            float w = c0.W;
            for (int i = 0; i < 4; ++i)
            {
                c = Vector3.Clamp(c, -boxfold, boxfold) * 2 - c;
                float rr = Vector3.Dot(c, c);
                c *= Util.Saturate(Math.Max(spherefold / rr, spherefold));
                c = c * scale + c0.Xyz();
                w = w * scale + c0.W;
            }
            return ((c.Length() - (scale - 1)) / w - Math.Pow(scale, -3));
        }
    }

    public class Mandelbulb : Fractal3d
    {        
        public override string StShader() => "mandelbulb.hlsl";

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
                double phi = Math.Atan(z.Y / z.X);
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
