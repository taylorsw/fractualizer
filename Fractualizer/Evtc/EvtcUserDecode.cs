using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using EVTC;
using Fractals;
using SharpDX;
using Point = System.Drawing.Point;

namespace EVTC
{
    public abstract class EvtcUserDecode : Evtc
    {
        private readonly HashSet<Keys> mpkeys;

        protected EvtcUserDecode(Form form, Controller controller) : base(form, controller)
        {
            mpkeys = new HashSet<Keys>();
            form.KeyDown += OnKeyDown;
            form.KeyUp += OnKeyUp;
        }

        private void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            mpkeys.Add(keyEventArgs.KeyCode);
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            OnKeyUp(keyEventArgs);
            mpkeys.Remove(keyEventArgs.KeyCode);
        }

        protected virtual void OnKeyUp(KeyEventArgs keyEventArgs) { }

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
            float ddxScene = camera.rsViewPlane.X * frScreenX;
            float ddyScene = camera.rsViewPlane.Y * frScreenY;

            Vector3 ptPlane = camera.ptPlaneCenter
                   + camera.vkCameraRight * ddxScene
                   + camera.vkCameraOrtho * ddyScene;

            return ptPlane;
        }
    }
}
