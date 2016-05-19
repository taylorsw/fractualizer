﻿using System;
using System.Drawing;
using System.Windows.Forms;
using Render;
using SharpDX.Windows;

namespace Mandelbasic
{
    partial class Controller : IDisposable, IHaveScene
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
                AllowUserResizing = false,
                IsFullscreen = true
            };

            scene = new Scene(width, height, new Mandelbulb());
            renderForm.Show();

            renderer = new Renderer(this, renderForm);

            InitializeEvents();
        }

        public void Run()
        {
            RenderLoop.Run(renderForm, RunI);
        }

        private void RunI()
        {
            DoEvents();
            renderer.Render();
        }

        public void Dispose()
        {
            renderer.Dispose();
            renderForm.Dispose();
        }
    }
}
