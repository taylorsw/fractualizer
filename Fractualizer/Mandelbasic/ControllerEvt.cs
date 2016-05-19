using System.Collections.Generic;
using System.Windows.Forms;
using Render;

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

        private const double frMove = 0.1;
        public void DoEvents()
        {
            double duFromFractal = scene.fractal.DuEstimate(scene.camera.ptCamera.Xyz());
            float duMove = (float)(frMove * duFromFractal);

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
