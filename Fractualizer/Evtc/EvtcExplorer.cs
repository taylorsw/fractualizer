using System;
using System.Diagnostics;
using System.Windows.Forms;
using Fractals;
using SharpDX;
using Point = System.Drawing.Point;

namespace Mandelbasic
{
    public class EvtcExplorer : EvtcUserDecode
    {
        private readonly RailOrbit railLight;
        private readonly RailOrbit railLight2;
        public EvtcExplorer(Form form, Raytracer raytracer) : base(form, raytracer)
        {
            Cursor.Hide();
            CenterCursor();
            form.MouseMove += OnMouseMove;
            railLight = new RailOrbit(pt => camera.cameraData.ptLight = pt, Vector3.Zero, new Vector3(1, 1, 1), 60f / 1000);
            railLight2 = new RailHover(pt => camera.cameraData.ptLight2 = pt, scene.fractal, Vector3.Zero, new Vector3(0.3f, 0.4f, 0.7f), 0.08f, 0.15f, 0.15f, 1.0f);
        }

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
            float ddxScene = camera.rsViewPlane.X * frScreenX;
            float ddyScene = camera.rsViewPlane.Y * frScreenY;
            float dagrX = (float)(Math.Atan(ddxScene / camera.duNear));
            float dagrY = (float)(Math.Atan(ddyScene / camera.duNear));
            camera.RotateCamera(-dagrY, dagrX);

            CenterCursor();
        }

        private const float frMoveBase = 0.1f;
        public override void DoEvents(float dtms)
        {
            railLight.UpdatePt(camera.cameraData.ptLight, dtms);
            railLight2.UpdatePt(camera.cameraData.ptLight2, dtms);

            float frMove = frMoveBase;
            if (IsKeyDown(Keys.ShiftKey))
                frMove = frMove * 2;

            double duFromFractal = scene.fractal.DuDeFractal(camera.ptCamera);
            float duMove = (float)(frMove * duFromFractal);

            if (IsKeyDown(Keys.W))
                camera.MoveBy(camera.vkCamera * duMove);

            if (IsKeyDown(Keys.S))
                camera.MoveBy(-camera.vkCamera * duMove);

            if (IsKeyDown(Keys.A))
                camera.MoveBy(Vector3.Cross(camera.vkCamera, camera.vkCameraDown) * duMove);

            if (IsKeyDown(Keys.D))
                camera.MoveBy(Vector3.Cross(camera.vkCameraDown, camera.vkCamera) * duMove);

            if (IsKeyDown(Keys.P))
                form.Close();

            if (scene.fractal.cinputFloat > 0)
            {
                float dParam = 0.01f;
                if (IsKeyDown(Keys.Q))
                    scene.fractal.SetInputFloat(0, scene.fractal.GetInputFloat(0) - dParam);

                if (IsKeyDown(Keys.E))
                    scene.fractal.SetInputFloat(0, scene.fractal.GetInputFloat(0) + dParam);
            }
        }
    }

    public class EvtcLookAt : EvtcUserDecode
    {
        public EvtcLookAt(Form form, Raytracer raytracer) : base(form, raytracer) { }

        private DateTime dtLastB = DateTime.MinValue;
        public override void DoEvents(float dtms)
        {
            const float dagd = 2;
            if (IsKeyDown(Keys.O))
                camera.RollBy(dagd);
            if (IsKeyDown(Keys.I))
                camera.RollBy(-dagd);

            if (IsKeyDown(Keys.B))
            {
                TimeSpan tsSinceLast = DateTime.Now - dtLastB;
                if (tsSinceLast > TimeSpan.FromSeconds(1))
                {
                    dtLastB = DateTime.Now;
                    Vector3 ptViewTopLeft = PtViewPlaneFromPtClient(new Point(0, 0));
                    Vector3 ptViewCursor = PtViewPlaneFromPtCursor();
                    Debug.Assert(camera.vkCamera.IsOrthogonalTo(ptViewCursor - ptViewTopLeft));
                    camera.LookAt(ptViewCursor);
                }
            }
        }
    }
}
