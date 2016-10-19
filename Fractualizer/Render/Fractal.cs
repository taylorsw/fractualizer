using System;
using System.IO;
using Fractals;
using SharpDX.D3DCompiler;
using D3D11 = SharpDX.Direct3D11;
using SharpDX;

namespace Render
{
    public class FractalRenderer : IDisposable
    {
        public readonly Fractal3d fractal;

        public FractalRenderer(Fractal3d fractal)
        {
            this.fractal = fractal;
        }

        public class IncludeFX : Include
        {
            static string includeDirectory = "ShadersKludge/";

            public Stream Open(IncludeType type, string fileName, Stream parentStream)
            {
                return new FileStream(includeDirectory + fileName, FileMode.Open);
            }

            public void Close(Stream stream)
            {
                stream.Close();
                stream.Dispose();
            }

            public void Dispose()
            {
            }

            public IDisposable Shadow { get; set; }
        }

        protected D3D11.PixelShader pixelShader;

        public virtual void InitializeFractal(D3D11.Device d3dDevice, D3D11.DeviceContext deviceContext)
        {
            string stShaderPath = fractal.StShaderPath();
            using (
                var pixelShaderByteCode = ShaderBytecode.CompileFromFile(stShaderPath, "main", "ps_4_0",
                    ShaderFlags.Debug, include: new IncludeFX()))
            {
                string stErr = pixelShaderByteCode.Message;
                pixelShader = new D3D11.PixelShader(d3dDevice, pixelShaderByteCode);
            }

            deviceContext.PixelShader.Set(pixelShader);
        }

        public void Dispose()
        {
            pixelShader.Dispose();
        }
    }
}
