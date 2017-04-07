using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

                //raytracer._raytracerfractal.duNear = 10; // approximate orthographic
            }

            protected override void OnKeyUp(KeyEventArgs keyEventArgs)
            {
                switch (keyEventArgs.KeyCode)
                {
                    case Keys.R:
                        double duDepthSlice = raytracer._raytracerfractal.rsViewPlane.x/dxImgWidth;
                        int cslice = (int)(5.0 / duDepthSlice);
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

            private static void ClearAndGenFolder(string stFolder)
            {
                if (Directory.Exists(stFolder))
                    Directory.Delete(stFolder, recursive: true);
                Directory.CreateDirectory(stFolder);
            }

            private const int dxImgWidth = 700;
            private const string stSlicesFolder = "Slices";
            private const string stSvgFolder = "SVG";
            private const string stBmpFolder = "BMP";
            private const string stObjFolder = "OBJ";
            private static string StFileBmp(int islice) => "slice" + islice;
            private static string StFileBmpAndPath(int islice) => stSlicesFolder + "\\" + stBmpFolder + "\\" + StFileBmp(islice) + ".bmp";
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

                ClearAndGenFolder(stSlicesFolder);
                ClearAndGenFolder(stSlicesFolder + "\\" + stSvgFolder);
                ClearAndGenFolder(stSlicesFolder + "\\" + stBmpFolder);
                ClearAndGenFolder(stSlicesFolder + "\\" + stObjFolder);
                
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

                    bitmap.Save(StFileBmpAndPath(islice), ImageFormat.Bmp);
                    Debug.WriteLine(islice+1 + " / " + cslice + " -- estimated time remaining: " + TimeSpan.FromMilliseconds((stopwatch.ElapsedMilliseconds / (islice+1)) * (cslice - islice)));

                    GenSvg(StFileBmp(islice));
                }
                Debug.WriteLine("Total time elapsed: " + TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds));
                Debug.WriteLine("Generating obj file...");
                if (cslice > 0)
                    GenObj();
            }

            private static void GenSvg(string stSliceName)
            {
                string stBmpPathAndName = stSlicesFolder + "\\" + stBmpFolder + "\\" + stSliceName + ".bmp";
                string stSvgPathAndName = stSlicesFolder + "\\" + stSvgFolder + "\\" + stSliceName + ".svg";
                string stCommand = "Tools\\potrace -s " + stBmpPathAndName + " -o " + stSvgPathAndName;
                ExecuteCommand(stCommand);
            }

            private static void GenObj()
            {
                //run("Image Sequence...", "open=C:\\Users\\Taylor\\Source\\Repos\\fractualizer\\Fractualizer\\Scanner\\bin\\Debug\\Slices\\BMP\\slice0.bmp sort use");
                //run("Wavefront .OBJ ...", "stack=BMP threshold=50 resampling=2 red green blue save=C:\\Users\\Taylor\\Desktop\\batch.obj");
                // java\win64\jdk1.8.0_66\jre\bin\java -jar -Xmx1024m jars\ij-1.51j.jar -eval "print('Hello world');"
                // ..\..\..\..\Tools\fiji-win64\Fiji.app

                string stFolderScanner = Environment.CurrentDirectory;
                string stMacroPath = stFolderScanner + "\\" + "obj_macro.txt";
                string stFijiFolder = Path.GetFullPath("..\\..\\..\\..\\Tools\\fiji-win64\\Fiji.app");
                string stCommand = stFijiFolder + "\\java\\win64\\jdk1.8.0_66\\jre\\bin\\java -jar -Xmx1024m " +
                                   stFijiFolder + "\\jars\\ij-1.51j.jar -ijpath " + stFijiFolder + " -macro " + stMacroPath + " " +
                                   "\"" +
                                   Path.GetFullPath(StFileBmpAndPath(0)) + " " +
                                   Path.GetFullPath(Path.Combine(stSlicesFolder, stObjFolder, "slices.obj"))
                                   + "\"";
                Debug.Assert(Directory.Exists(stFijiFolder), "Did you screw up the folder structure and forget to tell me because I hard-coded this like a moron?");
                ExecuteCommand(stCommand);
            }

            static void ExecuteCommand(string stCommand)
            {
                var processInfo = new ProcessStartInfo("cmd.exe", "/c " + stCommand);
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardError = true;
                processInfo.RedirectStandardOutput = true;
                var process = Process.Start(processInfo);
                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Debug.WriteLine("output>>" + e.Data);
                };
                process.BeginOutputReadLine();
                process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Debug.WriteLine("error>>" + e.Data);
                };
                process.BeginErrorReadLine();
                process.WaitForExit();
                process.Close();
            }
        }
    }
}
