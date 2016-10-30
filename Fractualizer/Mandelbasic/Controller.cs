using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Fractals;
using Render;
using SharpDX.Windows;

namespace Mandelbasic
{
    class Controller : IDisposable
    {
        private readonly RenderForm renderForm;
        private readonly Renderer renderer;
        private readonly Stopwatch stopwatch;
        private readonly Evtc evtc;

        public readonly Raytracer raytracer;
        
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

            raytracer = new RaytracerDummy(new Scene(width, height, new Mandelbulb()));
            renderForm.Show();

            renderer = new Renderer(raytracer, renderForm);

            renderForm.Focus();

            stopwatch = new Stopwatch();

            evtc = new EvtcExplorer(renderForm, raytracer.scene);
        }

        public void Run()
        {
            stopwatch.Start();
            RenderLoop.Run(renderForm, RunI);
        }

        private void RunI()
        {
            stopwatch.Stop(); // probably should remove
            evtc.DoEvents((float)stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond);
            stopwatch.Restart();
            renderer.Render();
        }

        public void Dispose()
        {
            renderer.Dispose();
            renderForm.Dispose();
        }
    }
}
