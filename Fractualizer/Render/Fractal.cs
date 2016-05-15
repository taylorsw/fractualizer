using System;
using SharpDX.D3DCompiler;
using D3D11 = SharpDX.Direct3D11;

namespace Render
{
    public abstract class Fractal : IDisposable
    {
        private D3D11.PixelShader pixelShader;

        internal Fractal()
        {
        }

        public virtual void InitializeFractal(D3D11.Device d3dDevice, D3D11.DeviceContext deviceContext)
        {
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Shaders/pixelShader.hlsl", "main", "ps_4_0", ShaderFlags.Debug))
            {
                pixelShader = new D3D11.PixelShader(d3dDevice, pixelShaderByteCode);
            }

            deviceContext.PixelShader.Set(pixelShader);
        }

        public abstract string Shader();

        public void Dispose()
        {
            pixelShader.Dispose();
        }
    }

    public class Mandelbrot : Fractal
    {
        public override string Shader()
        {
            return "mandelbrot.hlsl";
        }
    }
}
