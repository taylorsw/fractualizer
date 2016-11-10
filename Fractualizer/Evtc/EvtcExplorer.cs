using System;
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
        private Random rand = new Random(1990);

        private RailHover[] rgrailHoverBallLight;

        const float duCutoffBallLight = 0.3f;
        public EvtcExplorer(Form form, Controller controller) : base(form, controller)
        {
            Cursor.Hide();
            CenterCursor();
            form.MouseMove += OnMouseMove;

            Setup();
        }

        private void Setup()
        {
            Mandelbulb mandelbulb = raytracer.scene.fractal as Mandelbulb;
            if (mandelbulb != null)
            {
                mandelbulb._mandelbulb.param = 8.0f;
                mandelbulb._mandelbulb.param2 = 1.0f;
            }

            raytracer.camera.MoveTo(new Vector3(0, 0, -1.5f));
            raytracer.camera.LookAt(Vector3.Zero);

            raytracer.lightManager.RemoveAllLights();
            raytracer.lightManager.AddLight(new PointLight(new Vector3f(2, 0, -1), ColorU.rgbWhite, fVisualize: false));

            const int cballlight = 7;
            rgrailHoverBallLight = new RailHover[cballlight];
            for (int iballlight = 0; iballlight < cballlight; iballlight++)
            {
                raytracer.lightManager.AddLight(new BallLight(rand.VkUnitRand() * 2.0f, rand.VkUnitRand(), duCutoffBallLight));

                int ilight = iballlight + 1;
                RailHover railHover = new RailHover(pt => raytracer.lightManager[ilight].ptLight = pt, 
                    scene.fractal,
                    Vector3.Zero, 
                    rand.VkUnitRand(),
                    rand.NextFloat(0, 0.1f),
                    duCutoffBallLight/5, 
                    duCutoffBallLight/5, 
                    10.0f);
                rgrailHoverBallLight[iballlight] = railHover;
            }
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
                case Keys.T:
                    Setup();
                    break;
                case Keys.Y:
                    raytracer.CPUScreenshot();
                    break;
                case Keys.L:
                    fLightFollows = !fLightFollows;
                    break;
                case Keys.NumPad8:
                    controller.Resize(raytracer.width * 2, raytracer.height * 2);
                    break;
            }
        }

        private const float frMoveBase = 0.1f;
        public override void DoEvents(float dtms)
        {
            if (fLightFollows)
                raytracer.lightManager[0].ptLight = raytracer.camera.ptCamera;

            for (int irailHover = 0; irailHover < rgrailHoverBallLight.Length; irailHover++)
            {
                int ilight = irailHover + 1;
                rgrailHoverBallLight[irailHover].UpdatePt(raytracer.lightManager[ilight].ptLight, dtms);
            }

            float frMove = frMoveBase;
            if (IsKeyDown(Keys.ShiftKey))
                frMove = frMove * 2;

            double duFromFractal = scene.fractal.DuDeFractalOrCache(camera.ptCamera);
            float duMove = (float)(frMove * duFromFractal);

            if (IsKeyDown(Keys.W))
                camera.MoveBy(camera.vkCamera * duMove);

            if (IsKeyDown(Keys.S))
                camera.MoveBy(-camera.vkCamera * duMove);

            if (IsKeyDown(Keys.A))
                camera.MoveBy(Vector3.Cross(camera.vkCamera, camera.vkCameraOrtho) * duMove);

            if (IsKeyDown(Keys.D))
                camera.MoveBy(Vector3.Cross(camera.vkCameraOrtho, camera.vkCamera) * duMove);

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
}
