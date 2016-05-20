using System.Collections.Generic;
using System.Windows.Forms;
using Render;
using SharpDX;
using Point = System.Drawing.Point;

namespace Mandelbasic
{
    partial class Controller
    {
        private HashSet<Keys> mpkeys;
        private void InitializeEvents()
        {
            Cursor.Hide();
            CenterCursor();
            mpkeys = new HashSet<Keys>();
            renderForm.KeyDown += OnKeyDown;
            renderForm.KeyUp += OnKeyUp;
            renderForm.MouseMove += OnMouseMove;
            renderForm.KeyPress += OnKeyPress;
        }

        private Point ptFormCenter => new Point(renderForm.Width/2, renderForm.Height/2);
        private void CenterCursor()
        {
            Cursor.Position = renderForm.PointToScreen(ptFormCenter);
        }

        private const float frDamping = 1.0f;
        private void OnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            Point ptMouseClient = renderForm.PointToClient(Cursor.Position);

            if (ptMouseClient == ptFormCenter)
                return;

            Vector2 vkMouseDelta = new Vector2(ptMouseClient.X - ptFormCenter.X, ptMouseClient.Y - ptFormCenter.Y);

            vkMouseDelta *= frDamping;

            float frScreenX = (float)vkMouseDelta.X/renderForm.Width;
            float frScreenY = (float)vkMouseDelta.Y/renderForm.Height;

            Vector4 ptPlaneCameraNew = scene.camera.ptPlaneCenter
                                       + scene.camera.vkCameraDown*scene.camera.rsViewPlane.Y*frScreenY
                                       + scene.camera.vkCameraRight*scene.camera.rsViewPlane.X*frScreenX;

            scene.camera.vkCamera = ptPlaneCameraNew - scene.camera.ptCamera;
            scene.camera.vkCamera.Normalize();

            CenterCursor();
        }

        private void OnKeyPress(object sender, KeyPressEventArgs keyPressEventArgs)
        {
            if (char.ToUpper(keyPressEventArgs.KeyChar) == 'T')
                scene.camera = Scene.Camera.Initial(renderForm.Width, renderForm.Height);
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

        private const float frMoveBase = 0.1f;
        public void DecodeKeyState()
        {
            float frMove = frMoveBase;
            if (IsKeyDown(Keys.ShiftKey))
                frMove = frMove*2;

            double duFromFractal = scene.fractal.DuEstimate(scene.camera.ptCamera.Xyz());
            float duMove = (float)(frMove * duFromFractal);

            if (IsKeyDown(Keys.W))
                scene.camera.ptCamera += scene.camera.vkCamera * duMove;

            if (IsKeyDown(Keys.S))
                scene.camera.ptCamera -= scene.camera.vkCamera * duMove;

            if (IsKeyDown(Keys.A))
                scene.camera.ptCamera += scene.camera.vkCamera.Cross3(scene.camera.vkCameraDown) * duMove;

            if (IsKeyDown(Keys.D))
                scene.camera.ptCamera += scene.camera.vkCameraDown.Cross3(scene.camera.vkCamera) * duMove;
        }
    }
}
