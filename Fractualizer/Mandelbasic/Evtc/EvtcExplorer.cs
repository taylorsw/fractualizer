using System;
using System.Diagnostics;
using System.Windows.Forms;
using Render;
using SharpDX;
using Point = System.Drawing.Point;

namespace Mandelbasic
{
    public class EvtcExplorer : EvtcUserDecode
    {
        private readonly RailOrbit railLight;
        private readonly RailOrbit railLight2;
        public EvtcExplorer(Form form, Scene scene) : base(form, scene)
        {
            Cursor.Hide();
            CenterCursor();
            form.MouseMove += OnMouseMove;
            railLight = new RailOrbit(pt => scene.camera.ptLight = pt, Vector3.Zero, new Vector3(1, 1, 1), 60f / 1000);
            railLight2 = new RailHover(pt => scene.camera.ptLight2 = pt, scene.fractalRenderer.fractal, Vector3.Zero, new Vector3(0.3f, 0.4f, 0.7f), 0.08f, 0.15f, 0.15f, 1.0f);
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
            float ddxScene = scene.camera.rsViewPlane.X * frScreenX;
            float ddyScene = scene.camera.rsViewPlane.Y * frScreenY;
            float dagrX = (float)(Math.Atan(ddxScene / scene.camera.duNear));
            float dagrY = (float)(Math.Atan(ddyScene / scene.camera.duNear));
            scene.camera.RotateCamera(-dagrY, dagrX);

            CenterCursor();
        }

        private const float frMoveBase = 0.1f;
        public override void DoEvents(float dtms)
        {
            railLight.UpdatePt(scene.camera.ptLight, dtms);
            railLight2.UpdatePt(scene.camera.ptLight2, dtms);

            float frMove = frMoveBase;
            if (IsKeyDown(Keys.ShiftKey))
                frMove = frMove * 2;

            double duFromFractal = scene.fractalRenderer.fractal.DuEstimate(scene.camera.ptCamera);
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

            if (scene.fractalRenderer.fractal.cinputFloat > 0)
            {
                float dParam = 0.01f;
                if (IsKeyDown(Keys.Q))
                    scene.fractalRenderer.fractal.SetInputFloat(0, scene.fractalRenderer.fractal.GetInputFloat(0) - dParam);

                if (IsKeyDown(Keys.E))
                    scene.fractalRenderer.fractal.SetInputFloat(0, scene.fractalRenderer.fractal.GetInputFloat(0) + dParam);
            }
        }
    }

    public class EvtcLookAt : EvtcUserDecode
    {
        public EvtcLookAt(Form form, Scene scene) : base(form, scene) { }

        private DateTime dtLastB = DateTime.MinValue;
        public override void DoEvents(float dtms)
        {
            const float dagd = 2;
            if (IsKeyDown(Keys.O))
                scene.camera.RollBy(dagd);
            if (IsKeyDown(Keys.I))
                scene.camera.RollBy(-dagd);

            if (IsKeyDown(Keys.B))
            {
                TimeSpan tsSinceLast = DateTime.Now - dtLastB;
                if (tsSinceLast > TimeSpan.FromSeconds(1))
                {
                    dtLastB = DateTime.Now;
                    Vector3 ptViewTopLeft = PtViewPlaneFromPtClient(new Point(0, 0));
                    Vector3 ptViewCursor = PtViewPlaneFromPtCursor();
                    Debug.Assert(scene.camera.vkCamera.IsOrthogonalTo(ptViewCursor - ptViewTopLeft));
                    scene.camera.LookAt(ptViewCursor);
                }
            }
        }
    }
}
