using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Render;
using SharpDX;
using Point = System.Drawing.Point;

namespace Mandelbasic
{
    public abstract class EvtcUserDecode : Evtc
    {
        private readonly HashSet<Keys> mpkeys;

        protected EvtcUserDecode(Form form, Scene scene) : base(form, scene)
        {
            mpkeys = new HashSet<Keys>();
            form.KeyDown += OnKeyDown;
            form.KeyUp += OnKeyUp;
            form.KeyPress += OnKeyPress;
        }

        private void OnKeyPress(object sender, KeyPressEventArgs keyPressEventArgs)
        {
            char charUpper = char.ToUpper(keyPressEventArgs.KeyChar);

            switch (charUpper)
            {
                case 'T':
                    scene.ResetScene();
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

        protected bool IsKeyDown(Keys keyCode)
        {
            return mpkeys.Contains(keyCode);
        }

        protected Point ptFormCenter => new Point(form.Width / 2, form.Height / 2);

        protected Vector3 PtViewPlaneFromPtCursor()
        {
            return PtViewPlaneFromPtCursor(Cursor.Position);
        }

        protected Vector3 PtViewPlaneFromPtCursor(Point ptCursor)
        {
            Point ptMouseClient = form.PointToClient(ptCursor);
            return PtViewPlaneFromPtClient(ptMouseClient);
        }

        protected Vector3 PtViewPlaneFromPtClient(Point ptClient)
        {
            Vector2 vkMouseDelta = new Vector2(ptClient.X - ptFormCenter.X, ptClient.Y - ptFormCenter.Y);

            float frScreenX = vkMouseDelta.X / form.Width;
            float frScreenY = vkMouseDelta.Y / form.Height;
            float ddxScene = scene.camera.rsViewPlane.X * frScreenX;
            float ddyScene = scene.camera.rsViewPlane.Y * frScreenY;

            Vector3 ptPlane = scene.camera.ptPlaneCenter
                   + scene.camera.vkCameraRight * ddxScene
                   + scene.camera.vkCameraDown * ddyScene;

            return ptPlane;
        }
    }
}
