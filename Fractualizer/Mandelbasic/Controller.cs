using System;
using System.Drawing;
using Render;
using SharpDX.Windows;

namespace Mandelbasic
{
    class Controller : IDisposable
    {
        private const int width = 1280;
        private const int height = 720;

        private readonly RenderForm renderForm;
        private readonly Renderer renderer;

        public Controller()
        {
            renderForm = new RenderForm("Fractualizer")
            {
                ClientSize = new Size(width, height),
                AllowUserResizing = false
            };

            renderer = new Renderer();
        }

        public void Run()
        {
            RenderLoop.Run(renderForm, renderer.Render);
        }

        public void Dispose()
        {
            renderForm.Dispose();
            renderer.Dispose();
        }
    }
}
