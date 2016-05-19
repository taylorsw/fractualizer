using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Render;
using SharpDX;

namespace Mandelbasic
{
    partial class Controller
    {
        private HashSet<Keys> mpkeys;
        private void InitializeEvents()
        {
            mpkeys = new HashSet<Keys>();
            renderForm.KeyDown += OnKeyDown;
            renderForm.KeyUp += OnKeyUp;
        }

        private const float duMove = 0.1f;
        public void DoEvents()
        {
            if (IsKeyDown(Keys.W))
                scene.camera.ptCamera += scene.camera.vkCamera * duMove;

            if (IsKeyDown(Keys.S))
                scene.camera.ptCamera -= scene.camera.vkCamera * duMove;

            if (IsKeyDown(Keys.A))
                scene.camera.ptCamera += scene.camera.vkCamera.Cross3(scene.camera.vkCameraRoll) * duMove;

            if (IsKeyDown(Keys.D))
                scene.camera.ptCamera += scene.camera.vkCameraRoll.Cross3(scene.camera.vkCamera) * duMove;
        }

        private void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            mpkeys.Add(keyEventArgs.KeyCode);
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            mpkeys.Remove(keyEventArgs.KeyCode);
        }

        private bool IsKeyDown(Keys keyCode)
        {
            return mpkeys.Contains(keyCode);
        }
    }
}
