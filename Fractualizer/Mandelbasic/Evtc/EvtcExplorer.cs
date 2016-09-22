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
        public EvtcExplorer(Form form, Scene scene) : base(form, scene)
        {
            Cursor.Hide();
            CenterCursor();
            form.MouseMove += OnMouseMove;
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
        public override void DoEvents()
        {
            float frMove = frMoveBase;
            if (IsKeyDown(Keys.ShiftKey))
                frMove = frMove * 2;

            double duFromFractal = 1.0; //scene.fractal.DuEstimate(scene.camera.ptCamera);
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

            float dParam = 0.01f;
            if (IsKeyDown(Keys.Q))
                scene.camera.param -= dParam;

            if (IsKeyDown(Keys.E))
                scene.camera.param += dParam;
        }
    }

    public class EvtcLookAt : EvtcUserDecode
    {
        public EvtcLookAt(Form form, Scene scene) : base(form, scene) { }

        private DateTime dtLastB = DateTime.MinValue;
        public override void DoEvents()
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
