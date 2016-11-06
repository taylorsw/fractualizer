using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Fractals;
using SharpDX;
using Point = System.Drawing.Point;

namespace Mandelbasic
{
    public abstract class EvtcUserDecode : Evtc
    {
        private readonly HashSet<Keys> mpkeys;

        protected EvtcUserDecode(Form form, RaytracerFractal raytracer) : base(form, raytracer)
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
                    raytracer.ResetSceneAndCamera();
                    break;
                case 'G':
                    Debug.WriteLine(camera);
                    break;
                case 'Y':
                    //var color = raytracer.RgbaTrace(new Vector2d(ptFormCenter.X, ptFormCenter.Y));
                    //Debug.WriteLine(color.x + ", " + color.y + ", " + color.z);
                    raytracer.CPUScreenshot();
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
            float ddxScene = camera.rsViewPlane.X * frScreenX;
            float ddyScene = camera.rsViewPlane.Y * frScreenY;

            Vector3 ptPlane = camera.ptPlaneCenter
                   + camera.vkCameraRight * ddxScene
                   + camera.vkCameraOrtho * ddyScene;

            return ptPlane;
        }
    }
}
