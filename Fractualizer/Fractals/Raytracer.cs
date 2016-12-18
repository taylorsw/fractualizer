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
using Util;
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
        protected readonly Rgparam rgparam;

        protected Fractal3d fractal => scene.fractal;

        public class Rgparam
        {
            public static Rgparam Default = new Rgparam("Textures/skysphere.jpg");

            public readonly string stPathSkysphere;

            public Rgparam(string stPathSkysphere)
            {
                this.stPathSkysphere = stPathSkysphere;
            }
        }

        protected Raytracer(Scene scene, int width, int height, Rgparam rgparam)
        {
            this.scene = scene;
            this.width = width;
            this.height = height;
            this.rgparam = rgparam;
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
                var pixelShaderByteCode = ShaderBytecode.CompileFromFile(stShaderPath, "main", "ps_5_0",
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
            int imgWidth = width;
            int imgHeight = height;
            Bitmap bitmap = new Bitmap(imgWidth, imgHeight);
            Vector3d[][] rgrgColorScreenshot = new Vector3d[imgWidth][];
            for (int x = 0; x < imgWidth; x++)
                rgrgColorScreenshot[x] = new Vector3d[imgHeight];

            int duxProgress = 0;
            int duxPerProg = imgWidth / 100;
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;
            Parallel.For(
                0,
                imgWidth,
                parallelOptions,
                x =>
                {
                    Parallel.For(
                        0,
                        imgHeight,
                        y =>
                        {
                            Vector3d rgbaTrace = RgbaTrace(new Vector2d(x, y));
                            rgrgColorScreenshot[x][y] = rgbaTrace;
                        });
                    if (x % duxPerProg == 0)
                    {
                        Interlocked.Increment(ref duxProgress);
                        Debug.WriteLine("Progress: " + 100f * (duxPerProg * duxProgress) / (float)imgWidth + "%");
                    }
                });

            for (int x = 0; x < imgWidth; x++)
            {
                for (int y = 0; y < imgHeight; y++)
                {
                    Vector3d rgbd = rgrgColorScreenshot[x][y];
                    Color color = ProcessColor(rgbd);
                    bitmap.SetPixel(x, y, color);
                }
            }
        
            bitmap.Save("screenshot" + cscreenshot++ + ".jpg");
        }

        private Color ProcessColor(Vector3d rgb)
        {
//            double duMin = Math.Min(double.PositiveInfinity, Math.Min(rgb.x, Math.Min(rgb.y, rgb.z)));
//            if (duMin < 0)
//            {
//                double duAbsMin = Math.Abs(duMin);
//                rgb = new Vector3d(rgb.x + duAbsMin, rgb.y + duAbsMin, rgb.z + duAbsMin);
//            }
//            double duMax = Math.Max(double.NegativeInfinity, Math.Max(rgb.x, Math.Max(rgb.y, rgb.z)));
//            if (duMax > 1.0)
//            {
//                rgb = new Vector3d(rgb.x / duMax, rgb.y / duMax, rgb.z / duMax);
//            }
            Color color = Color.FromArgb(
                SharpDX.Color.ToByte(IntComponentFromDouble(rgb.x)),
                SharpDX.Color.ToByte(IntComponentFromDouble(rgb.y)),
                SharpDX.Color.ToByte(IntComponentFromDouble(rgb.z)));
            return color;
        }

        private static int IntComponentFromDouble(double component) => (int) ((double) component*(double) byte.MaxValue);

        public abstract Vector4d RgbaTrace(Vector2d pos);

        public override void Dispose()
        {
            pixelShader.Dispose();
            scene.Dispose();
        }
    }

    public class Scene : IDisposable
    {
        public readonly Random rand;
        public readonly Fractal3d fractal;

        public Scene(Fractal3d fractal, int seed = 1993)
        {
            this.rand = new Random(seed);
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

        public void Dispose()
        {
            fractal.Dispose();
        }
    }
}
