using System;
using System.Drawing;
using SharpDX.Windows;

namespace Render
{
    public class Renderer : IDisposable
    {
        private const int width = 1280;
        private const int height = 720;

        private readonly RenderForm renderForm;

        public Renderer()
        {
            renderForm = new RenderForm("My first SharpDX game");
            renderForm.ClientSize = new Size(width, height);
            renderForm.AllowUserResizing = false;
        }

        public void Run()
        {
            RenderLoop.Run(renderForm, Render);
        }

        public void Render()
        {
            
        }

        public void Dispose()
        {
            renderForm.Dispose();
        }
    }
}
