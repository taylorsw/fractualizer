using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Fractals;
using Render;
using SharpDX.Windows;

namespace EVTC
{
    public abstract class Controller : IDisposable
    {
        private readonly RenderForm renderForm;
        private Renderer renderer;
        private readonly Stopwatch stopwatch;
 
        public Stage stage { get; private set; }
        public RaytracerFractal raytracer => stage.raytracer;

        protected virtual int width => Renderer.fFullscreen ? Screen.PrimaryScreen.Bounds.Width : 1920;
        protected virtual int height => Renderer.fFullscreen ? Screen.PrimaryScreen.Bounds.Height : 1080;

        protected Controller()
        {
            renderForm = new RenderForm("Fractualizer")
            {
                ClientSize = new Size(width, height),
                AllowUserResizing = true,
                IsFullscreen = Renderer.fFullscreen
            };

            stopwatch = new Stopwatch();
        }

        protected abstract Stage StageCreate(Form form);

        public void Run()
        {
            stage = StageCreate(renderForm);
            stage.Setup();

            renderForm.Show();

            renderer = new Renderer(stage.raytracer, renderForm);

            renderForm.Focus();

            stopwatch.Start();
            RenderLoop.Run(renderForm, RunI);
        }

        private void RunI()
        {
            stopwatch.Stop(); // probably should remove
            stage.evtc.HandleTime((float)stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond);
            stopwatch.Restart();
            renderer.Render();
        }

        public virtual void Resize(int width, int height)
        {
            //            raytracer.width = width;
            //            raytracer.height = height;
            //            renderForm.ClientSize = new Size(width, height);
        }

        public virtual void Exit()
        {
            Dispose();
            renderForm.Close();
        }

        public virtual void Dispose()
        {
            renderer.Dispose();
            renderForm.Dispose();
        }
    }
}
