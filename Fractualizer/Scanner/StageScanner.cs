using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EVTC;
using Fractals;
using SharpDX;
using Util;
using Color = System.Drawing.Color;

namespace Scanner
{
    public class StageScanner : Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageScanner(Form form, Controller controller, int width, int height)
        {
            raytracer = new RaytracerFractal(new Scene(new Mandelbox()), width, height);
            evtc = new EvtcScanner(form, controller);
        }

        private class EvtcScanner : EvtcExplorer
        {
            public EvtcScanner(Form form, Controller controller) : base(form, controller) { }

            protected override void OnKeyUp(KeyEventArgs keyEventArgs)
            {
                switch (keyEventArgs.KeyCode)
                {
                    case Keys.R:
                        double duDepthSlice = raytracer._raytracerfractal.rsViewPlane.x/dxImgWidth;
                        int cslice = 10;
                        Scan(cslice, duDepthSlice);
                        break;
                    default:
                        base.OnKeyUp(keyEventArgs);
                        break;
                }
            }

            private const int dxImgWidth = 700;
            private int cscan = 0;
            private void Scan(int cslice, double duDepthPerSlice)
            {
                for (int islice = 0; islice < cslice; islice++)
                {
                    double duDepthSlice = duDepthPerSlice * islice;

                    double sfRatio = (double)camera.rsScreen.Y / camera.rsScreen.X;
                    int dyImgHeight = (int)(dxImgWidth * sfRatio);
                    Bitmap bitmap = new Bitmap(dxImgWidth, dyImgHeight);
                    bool[][] rgrgVertices = new bool[dxImgWidth][];
                    for (int x = 0; x < dxImgWidth; x++)
                        rgrgVertices[x] = new bool[dyImgHeight];

                    int dcolProgress = 0;
                    int dcolPerProg = dxImgWidth / 100;
                    ParallelOptions parallelOptions = new ParallelOptions();
                    parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;
                    double duEpsilon = raytracer._raytracerfractal.rsViewPlane.x / dxImgWidth;
                    Parallel.For(
                        0,
                        dxImgWidth,
                        parallelOptions,
                        x =>
                        {
                            Parallel.For(
                                0,
                                dyImgHeight,
                                y =>
                                {
                                    Vector3d ptViewPlane = raytracer.PtPlane(new Vector2d(x, y)) + (Vector3d)camera.vkCamera * duDepthSlice;
                                    double duDe = fractal.DuDeFractal(ptViewPlane);
                                    if (duDe < duEpsilon)
                                        rgrgVertices[x][y] = true;
                                });
                            if (x % dcolPerProg == 0)
                            {
                                Interlocked.Increment(ref dcolProgress);
                                Debug.WriteLine("Progress: " + 100f * (dcolPerProg * dcolProgress) / (float)dxImgWidth + "%");
                            }
                        });

                    for (int x = 0; x < dxImgWidth; x++)
                    {
                        for (int y = 0; y < dyImgHeight; y++)
                        {
                            bool fVertex = rgrgVertices[x][y];
                            Color color = fVertex ? Color.Black : Color.White;
                            bitmap.SetPixel(x, y, color);
                        }
                    }

                    Directory.CreateDirectory("Slices");
                    bitmap.Save("Slices/slice " + cscan++ + ".bmp");
                }
            }
        }
    }
}
