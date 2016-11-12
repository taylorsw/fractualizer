using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Evtc;
using Fractals;
using Render;
using SharpDX.Windows;

namespace Mandelbasic
{
    class ControllerMandelbasic : Controller
    {
        public readonly RaytracerFractal raytracerI;
        public override RaytracerFractal raytracer => raytracerI;

        private readonly RenderForm renderForm;
        private readonly Renderer renderer;
        private readonly Stopwatch stopwatch;
        private readonly Evtc evtc;
        
        public ControllerMandelbasic()
        {
            int width = Renderer.fFullscreen ? Screen.PrimaryScreen.Bounds.Width : 1920;
            int height = Renderer.fFullscreen ? Screen.PrimaryScreen.Bounds.Height : 1080;
            renderForm = new RenderForm("Fractualizer")
            {
                ClientSize = new Size(width, height),
                AllowUserResizing = true,
                IsFullscreen = Renderer.fFullscreen
            };

            raytracerI = new RaytracerFractal(new Scene(new Mandelbulb()), width, height);
            renderForm.Show();

            renderer = new Renderer(raytracerI, renderForm);

            renderForm.Focus();

            stopwatch = new Stopwatch();

            evtc = new EvtcAudio(renderForm, this);
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

        public override void Resize(int width, int height)
        {
//            raytracer.width = width;
//            raytracer.height = height;
//            renderForm.ClientSize = new Size(width, height);
        }

        public override void Exit()
        {
            Dispose();
            renderForm.Close();
        }

        public override void Dispose()
        {
            renderer.Dispose();
            renderForm.Dispose();
        }
    }
}
