using System;
using System.Diagnostics;
using System.Windows.Forms;
using Evtc;
using Fractals;
using SharpDX;
using Util;
using Point = System.Drawing.Point;

namespace Mandelbasic
{
    public class EvtcExplorer : EvtcUserDecode
    {
        public EvtcExplorer(Form form, Controller controller) : base(form, controller)
        {
            Cursor.Hide();
            CenterCursor();
            form.MouseMove += OnMouseMove;
        }

        public override void Setup()
        {
            base.Setup();

            Mandelbulb mandelbulb = scene.fractal as Mandelbulb;
            if (mandelbulb != null)
            {
                mandelbulb._mandelbulb.param = 8.0f;
                mandelbulb._mandelbulb.param2 = 1.0f;
            }

            camera.MoveTo(new Vector3(0, 0, -1.5f));
            camera.LookAt(Vector3.Zero);

            lightManager.RemoveAllLights();
            lightManager.AddLight(new PointLight(new Vector3f(2, 0, -1), ColorU.rgbWhite, fVisualize: false));
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

        private bool fLightFollows = false;
        protected override void OnKeyUp(KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.KeyCode)
            {
                case Keys.P:
                    controller.Exit();
                    break;
                case Keys.T:
                    Setup();
                    break;
                case Keys.Y:
                    raytracer.CPUScreenshot();
                    break;
                case Keys.L:
                    fLightFollows = !fLightFollows;
                    break;
                case Keys.Space:
                    if (IsKeyDown(Keys.ControlKey))
                        lightManager.RemoveLight(lightManager.clight - 1);
                    else
                        lightManager.AddLight(new PointLight(camera.ptCamera, Vector3.One, fVisualize: false));
                    break;
                case Keys.G:
                    Debug.WriteLine(camera.ptCamera);
                    break;
            }
        }

        private const float frMoveBase = 0.1f;
        private const float dagdRoll = (float)360/(60*4);
        public override void DoEvents(float dtms)
        {
            if (fLightFollows)
                lightManager[0].ptLight = camera.ptCamera;

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
                camera.MoveBy(Vector3.Cross(camera.vkCamera, camera.vkCameraOrtho) * duMove);

            if (IsKeyDown(Keys.D))
                camera.MoveBy(Vector3.Cross(camera.vkCameraOrtho, camera.vkCamera) * duMove);

            if (IsKeyDown(Keys.Q))
                camera.RollBy(-dagdRoll);

            if (IsKeyDown(Keys.E))
                camera.RollBy(dagdRoll);

            if (IsKeyDown(Keys.D))
                camera.MoveBy(Vector3.Cross(camera.vkCameraOrtho, camera.vkCamera) * duMove);

            const float dbrightnessDim = 0.01f;

            if (IsKeyDown(Keys.NumPad7))
                lightManager[0].brightness -= dbrightnessDim;
            if (IsKeyDown(Keys.NumPad8))
                lightManager[0].brightness += dbrightnessDim;

            float dParam1 = 0.01f;
            float dParam2 = 0.005f;
            if (IsKeyDown(Keys.Z) && scene.fractal.cinputFloat >= 1)
                scene.fractal.SetInputFloat(0, scene.fractal.GetInputFloat(0) - dParam1);
            if (IsKeyDown(Keys.X) && scene.fractal.cinputFloat >= 1)
                scene.fractal.SetInputFloat(0, scene.fractal.GetInputFloat(0) + dParam1);
            if (IsKeyDown(Keys.C) && scene.fractal.cinputFloat >= 2)
                scene.fractal.SetInputFloat(1, scene.fractal.GetInputFloat(1) - dParam2);
            if (IsKeyDown(Keys.V) && scene.fractal.cinputFloat >= 2)
                scene.fractal.SetInputFloat(1, scene.fractal.GetInputFloat(1) + dParam2);
        }
    }
}
