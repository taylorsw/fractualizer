using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Render;
using SharpDX;
using Point = System.Drawing.Point;

namespace Mandelbasic
{
    public class EvtcUser : Evtc
    {
        private readonly HashSet<Keys> mpkeys;

        public EvtcUser(Form form, Scene scene) : base(form, scene)
        {
            mpkeys = new HashSet<Keys>();

            Cursor.Hide();
            CenterCursor();
            form.KeyDown += OnKeyDown;
            form.KeyUp += OnKeyUp;
            form.MouseMove += OnMouseMove;
            form.KeyPress += OnKeyPress;
        }

        private Point ptFormCenter => new Point(form.Width / 2, form.Height / 2);
        private void CenterCursor()
        {
            Cursor.Position = form.PointToScreen(ptFormCenter);
        }

        private const float frDamping = 1.0f;
        private void OnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            Point ptMouseClient = form.PointToClient(Cursor.Position);

            if (ptMouseClient == ptFormCenter)
                return;

            Vector2 vkMouseDelta = new Vector2(ptMouseClient.X - ptFormCenter.X, ptMouseClient.Y - ptFormCenter.Y);

            vkMouseDelta *= frDamping;

            float frScreenX = (float)vkMouseDelta.X / form.Width;
            float frScreenY = (float)vkMouseDelta.Y / form.Height;
            float ddxScene = scene.camera.rsViewPlane.X * frScreenX;
            float ddyScene = scene.camera.rsViewPlane.Y * frScreenY;
            float dagrX = (float)(Math.Atan(ddxScene / scene.camera.duNear));
            float dagrY = (float)(Math.Atan(ddyScene / scene.camera.duNear));
            scene.camera.RotateCamera(-dagrY, dagrX);

            CenterCursor();
        }

        private void OnKeyPress(object sender, KeyPressEventArgs keyPressEventArgs)
        {
            char charUpper = char.ToUpper(keyPressEventArgs.KeyChar);

            switch (charUpper)
            {
                case 'T':
                    scene.camera = Scene.Camera.Initial(form.Width, form.Height);
                    break;
                case 'G':
                    Debug.WriteLine(scene.camera);
                    break;
            }
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
        public override void DoEvents()
        {
            float frMove = frMoveBase;
            if (IsKeyDown(Keys.ShiftKey))
                frMove = frMove * 2;

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
                form.Close();
        }
    }
}
