﻿using System;
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
