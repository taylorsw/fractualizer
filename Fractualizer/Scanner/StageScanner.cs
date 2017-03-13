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
            raytracer = new RaytracerFractal(new Scene(new Mandelbulb()), width, height);
            evtc = new EvtcScanner(form, controller);
        }

        private class EvtcScanner : EvtcExplorer
        {
            public EvtcScanner(Form form, Controller controller) : base(form, controller) { }

            private Vector2f vkViewPlaneAdjust;
            public override void Setup()
            {
                base.Setup();
                //camera.MoveTo(new Vector3(0.5f, 0, 0));
                //camera.LookAt(camera.ptCamera + new Vector3(-0.5f, 0, 0));
                camera.MoveTo(new Vector3(0, 0, -1.5f));
                camera.LookAt(Vector3.Zero);
                vkViewPlaneAdjust = raytracer._raytracerfractal.rsViewPlane*0.01;
            }

            protected override void OnKeyUp(KeyEventArgs keyEventArgs)
            {
                switch (keyEventArgs.KeyCode)
                {
                    case Keys.R:
                        double duDepthSlice = raytracer._raytracerfractal.rsViewPlane.x/dxImgWidth;
                        int cslice = (int)(3.0 / duDepthSlice);
                        Scan(cslice, duDepthSlice);
                        break;
                    default:
                        base.OnKeyUp(keyEventArgs);
                        break;
                }
            }

            public override void DoEvents(float dtms)
            {
                if (IsKeyDown(Keys.NumPad8))
                    raytracer._raytracerfractal.rsViewPlane += vkViewPlaneAdjust;
                else if (IsKeyDown(Keys.NumPad5))
                    raytracer._raytracerfractal.rsViewPlane -= vkViewPlaneAdjust;
                base.DoEvents(dtms);
            }

            private const int dxImgWidth = 700;
            private int cscan = 0;
            private void Scan(int cslice, double duDepthPerSlice)
            {
                double sfRatio = (double)camera.rsScreen.Y / camera.rsScreen.X;
                int dyImgHeight = (int)(dxImgWidth * sfRatio);
                bool[][] rgrgVertices = new bool[dxImgWidth][];
                for (int x = 0; x < dxImgWidth; x++)
                    rgrgVertices[x] = new bool[dyImgHeight];

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;
                double duEpsilon = raytracer._raytracerfractal.rsViewPlane.x / dxImgWidth;

                Directory.CreateDirectory("Slices");
                
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int islice = 0; islice < cslice; islice++)
                {
                    foreach (bool[] rgrgVertex in rgrgVertices)
                        Array.Clear(rgrgVertex, 0, rgrgVertex.Length);

                    double duDepthSlice = duDepthPerSlice * islice;
                    Bitmap bitmap = new Bitmap(dxImgWidth, dyImgHeight);
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
                                    Vector2d ptPixelTl = new Vector2d(x, y);
                                    Vector3d ptViewPlane = raytracer.PtPlane(ptPixelTl * (raytracer._raytracerfractal.rsScreen.x / (double)dxImgWidth)) + (Vector3d)camera.vkCamera * duDepthSlice;
                                    double duDe = fractal.DuDeFractal(ptViewPlane);
                                    if (duDe < duEpsilon)
                                        rgrgVertices[x][y] = true;
                                    //Debug.WriteLine("{0},{1} => {2},{3}", ptPixelTl.x, ptPixelTl.y, ptViewPlane.x, ptViewPlane.y);
                                });
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

                    bitmap.Save("Slices/slice " + cscan++ + ".bmp");
                    Debug.WriteLine(islice+1 + " / " + cslice + " -- estimated time remaining: " + TimeSpan.FromMilliseconds((stopwatch.ElapsedMilliseconds / (islice+1)) * (cslice - islice)));
                }
                Debug.WriteLine("Total time elapsed: " + TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds));
            }
        }
    }
}
