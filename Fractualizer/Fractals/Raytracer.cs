using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeGen;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Color = System.Drawing.Color;

namespace Fractals
{
    public abstract class Raytracer : FPLGenBase
    {
        public class IncludeFX : Include
        {
            private Fractal3d fractal3D;

            public IncludeFX(Fractal3d fractal3D)
            {
                this.fractal3D = fractal3D;
            }

            public Stream Open(IncludeType type, string fileName, Stream parentStream)
            {
                Debug.Assert(fileName == GenU.stFractalInclude);
                return new FileStream(fractal3D.StShaderPath(), FileMode.Open);
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

        public int width;
        public int height;
        private PixelShader pixelShader;
        public readonly Scene scene;
        public abstract Camera camera { get; }

        protected Fractal3d fractal => scene.fractal;

        protected Raytracer(Scene scene, int width, int height)
        {
            this.scene = scene;
            this.width = width;
            this.height = height;
        }

        public override void Initialize(Device device, DeviceContext deviceContext)
        {
            InitializePixelShader(device, deviceContext);
            base.Initialize(device, deviceContext);
            scene.Initialize(device, deviceContext);
        }

        public override void Update(Device device, DeviceContext deviceContext)
        {
            base.Update(device, deviceContext);
            scene.UpdateBuffers(device, deviceContext);
        }

        private void InitializePixelShader(Device device, DeviceContext deviceContext)
        {
            string stShaderPath = StShaderPath();
            using (
                var pixelShaderByteCode = ShaderBytecode.CompileFromFile(stShaderPath, "main", "ps_4_0",
                    ShaderFlags.Debug, include: new IncludeFX(fractal)))
            {
                string stErr = pixelShaderByteCode.Message;
                pixelShader = new PixelShader(device, pixelShaderByteCode);
            }

            deviceContext.PixelShader.Set(pixelShader);
        }

        private static int cscreenshot = 0;
        public void CPUScreenshot()
        {
            Bitmap bitmap = new Bitmap(width, height);
            Vector3d[][] rgrgColorScreenshot = new Vector3d[width][];
            for (int x = 0; x < width; x++)
                rgrgColorScreenshot[x] = new Vector3d[height];

            int duxProgress = 0;
            int duxPerProg = width / 10;
            Parallel.For(
                0,
                width,
                x =>
                {
                    if (x%duxPerProg == 0)
                    {
                        Interlocked.Increment(ref duxProgress);
                        Debug.WriteLine("Progress: " + 100f * (duxPerProg * duxProgress) / (float)width + "%");
                    }
                    Parallel.For(
                        0,
                        height,
                        y =>
                        {
                            rgrgColorScreenshot[x][y] = RgbaTrace(new Vector2d(x, y));
                        });
                });

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3d rgbd = rgrgColorScreenshot[x][y];
                    Color color = Color.FromArgb(
                        SharpDX.Color.ToByte(IntComponentFromDouble(rgbd.x)),
                        SharpDX.Color.ToByte(IntComponentFromDouble(rgbd.y)),
                        SharpDX.Color.ToByte(IntComponentFromDouble(rgbd.z)));
                    bitmap.SetPixel(x, y, color);
                }
            }
        
            bitmap.Save("screenshot" + cscreenshot++ + ".jpg");
        }

        private static int IntComponentFromDouble(double component) => (int) ((double) component*(double) byte.MaxValue);

        public abstract Vector4d RgbaTrace(Vector2d pos);

        public void ResetSceneAndCamera()
        {
            scene.ResetScene();
            camera.ResetCamera();
        }

        public override void Dispose()
        {
            pixelShader.Dispose();
            scene.Dispose();
        }
    }

    public class Scene : IDisposable
    {
        public readonly Random rand = new Random(1984);

        public readonly Fractal3d fractal;

        public Scene(Fractal3d fractal)
        {
            this.fractal = fractal;
        }

        internal void Initialize(Device device, DeviceContext deviceContext)
        {
            fractal.Initialize(device, deviceContext);
        }

        internal void UpdateBuffers(Device device, DeviceContext deviceContext)
        {
            fractal.Update(device, deviceContext);
        }

        public void ResetScene()
        {
            fractal.ResetInputs();
        }

        public void Dispose()
        {
            fractal.Dispose();
        }
    }
}
