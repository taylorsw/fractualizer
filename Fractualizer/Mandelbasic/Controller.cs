using System;
using System.Drawing;
using Render;
using SharpDX.Windows;

namespace Mandelbasic
{
    class Controller : IDisposable, IHaveScene
    {
        private const int width = 1280;
        private const int height = 720;

        private readonly RenderForm renderForm;
        private readonly Renderer renderer;

        public Scene scene { get; }
        
        public Controller()
        {
            renderForm = new RenderForm("Fractualizer")
            {
                ClientSize = new Size(width, height),
                AllowUserResizing = false
            };

            scene = new Scene(new Mandelbrot());

            renderer = new Renderer(this, renderForm);
        }

        public void Run()
        {
            RenderLoop.Run(renderForm, renderer.Render);
        }

        public void Dispose()
        {
            renderer.Dispose();
            renderForm.Dispose();
        }
    }
}
