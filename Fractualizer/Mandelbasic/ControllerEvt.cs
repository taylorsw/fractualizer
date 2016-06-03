using System.Collections.Generic;
using System.Windows.Forms;
using Render;
using SharpDX;
using Point = System.Drawing.Point;
using System;

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
            float ddxScene = scene.camera.rsViewPlane.X * frScreenX;
            float ddyScene = scene.camera.rsViewPlane.Y * frScreenY;
            float dagrX = (float)(Math.Atan(ddxScene / scene.camera.duNear));
            float dagrY = (float)(Math.Atan(ddyScene / scene.camera.duNear));
            scene.camera.RotateCamera(-dagrY, dagrX);

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
//        private float du = -1;
//        private float du2 = -1;
        public void DecodeKeyState()
        {
            float frMove = frMoveBase;
            if (IsKeyDown(Keys.ShiftKey))
                frMove = frMove*2;

            double duFromFractal = scene.fractal.DuEstimate(scene.camera.ptCamera);
            float duMove = (float)(frMove * duFromFractal);

            if (IsKeyDown(Keys.W))
                scene.camera.ptCamera += scene.camera.vkCamera * duMove;

            if (IsKeyDown(Keys.S))
                scene.camera.ptCamera -= scene.camera.vkCamera * duMove;

            if (IsKeyDown(Keys.A))
                scene.camera.ptCamera += Vector3.Cross(scene.camera.vkCamera, scene.camera.vkCameraDown) * duMove;

            if (IsKeyDown(Keys.D))
                scene.camera.ptCamera += Vector3.Cross(scene.camera.vkCameraDown, scene.camera.vkCamera) * duMove;

            if (IsKeyDown(Keys.P))
                renderForm.Close();

            //            scene.camera.param += du*0.008f;
            //
            //            if (scene.camera.param < 0.0)
            //                du = 1;
            //            else if (scene.camera.param > 10.0)
            //                du = -1;
            //
            //            scene.camera.param2 += du2 * 0.0003f;
            //
            //            if (scene.camera.param2 < 1.0)
            //                du2 = 1;
            //            else if (scene.camera.param2 > 3.0)
            //                du2 = -1;
            //
            //            const float dagRotate = 0.08f;
            //            scene.camera.RotateAbout(new Vector3(1, 0, 0), dagRotate);
        }
    }
}
