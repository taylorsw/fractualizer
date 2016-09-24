using System;
using System.Drawing;
using System.Windows.Forms;
using Render;
using SharpDX.Windows;

namespace Mandelbasic
{
    partial class Controller : IDisposable, IHaveScene
    {
        private readonly RenderForm renderForm;
        private readonly Renderer renderer;
        private readonly Evtc evtc;

        public Scene scene { get; }
        
        public Controller()
        {
            int width = 1920;//Screen.PrimaryScreen.Bounds.Width;
            int height = 1080;//Screen.PrimaryScreen.Bounds.Height;
            renderForm = new RenderForm("Fractualizer")
            {
                ClientSize = new Size(width, height),
                AllowUserResizing = false,
                IsFullscreen = false //true
            };

            scene = new Scene(width, height, new Mandelbulb());
            renderForm.Show();

            renderer = new Renderer(this, renderForm);

            renderForm.Focus();

            evtc = new EvtcExplorer(renderForm, scene);
        }

        public void Run()
        {
            RenderLoop.Run(renderForm, RunI);
        }

        private void RunI()
        {
            evtc.DoEvents();
            renderer.Render();
        }

        private void CPURender()
        {
            int width = (int)scene.camera.rsScreen.X;
            int height = (int)scene.camera.rsScreen.Y;
            Bitmap bitmap = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var color = Raytracer.Raytrace(scene, new SharpDX.Vector2(x, y));
                    bitmap.SetPixel(x, y, Color.FromArgb(IntComponentFromDouble(color.X), IntComponentFromDouble(color.Y), IntComponentFromDouble(color.Z)));
                }
            }

            bitmap.Save("test.jpg");
        }

        private static int IntComponentFromDouble(double component) => (int)(255 * Util.Saturate((float)Math.Abs(component)));

        public void Dispose()
        {
            renderer.Dispose();
            renderForm.Dispose();
        }
    }
}
