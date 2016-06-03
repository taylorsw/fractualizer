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

        public Scene scene { get; }
        
        public Controller()
        {
            int width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            int height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            renderForm = new RenderForm("Fractualizer")
            {
                ClientSize = new Size(width, height),
                AllowUserResizing = false,
                IsFullscreen = true
            };

            scene = new Scene(width, height, new Mandelbulb());
            renderForm.Show();

            renderer = new Renderer(this, renderForm);

            renderForm.Focus();
            InitializeEvents();
        }

        public void Run()
        {
            RenderLoop.Run(renderForm, RunI);
        }

        private void RunI()
        {
            DecodeKeyState();
            renderer.Render();
        }

        public void Dispose()
        {
            renderer.Dispose();
            renderForm.Dispose();
        }
    }
}
