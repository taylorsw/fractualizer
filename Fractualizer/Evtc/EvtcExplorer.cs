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
        const int cballlight = 15;
        private RailHover[] rgrailHoverBallLight;
        private RailSpotlight railSpotlight;
        private SpotLight spotlight;

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

            rgrailHoverBallLight = new RailHover[cballlight];
            for (int iballlight = 0; iballlight < cballlight; iballlight++)
            {
                BallLight ballLight = new BallLight(rand.VkUnitRand() * 2.0f, rand.VkUnitRand(), duCutoffBallLight, fVisualize: false);
                raytracer.lightManager.AddLight(ballLight);

                RailHover railHover = new RailHover(
                    dgUpdatePt: pt => ballLight.ptLight = pt,
                    fractal: scene.fractal,
                    ptCenter: Vector3.Zero,
                    ptInitial: rand.VkUnitRand() * 2,
                    vkNormal: rand.VkUnitRand(),
                    dtmsRevolution: rand.NextFloat(5000, 10000),
                    duHoverMin: duCutoffBallLight/5, 
                    duHoverMax: duCutoffBallLight/5, 
                    sfTravelMax: 10.0f);
                rgrailHoverBallLight[iballlight] = railHover;
            }

            spotlight = new SpotLight(camera.ptCamera, Vector3.One, Vector3.One, 4);
            raytracer.lightManager.AddLight(spotlight);
            railSpotlight = new RailSpotlight(
                dgUpdateVkSpotlight: vk => spotlight.vkLight = vk,
                agdRadius: 50,
                vkNormal: camera.vkCamera,
                dtmsRevolution: 1500);
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
            }
        }

        private void DimBallLights(float dbrightness)
        {
            for (int iballlight = 0; iballlight < cballlight; iballlight++)
                raytracer.lightManager[iballlight + 1].brightness += dbrightness;
        }

        private const float frMoveBase = 0.1f;
        private const float dagdRoll = (float)360/(60*4);
        public override void DoEvents(float dtms)
        {
            if (fLightFollows)
                raytracer.lightManager[0].ptLight = raytracer.camera.ptCamera;

            foreach (RailHover railHover in rgrailHoverBallLight)
                railHover.UpdatePt(dtms);

            railSpotlight.UpdateVkSpotlight(dtms);
            //spotlight.ptLight = camera.ptCamera;

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

            if (IsKeyDown(Keys.Q))
                camera.RollBy(-dagdRoll);

            if (IsKeyDown(Keys.E))
                camera.RollBy(dagdRoll);

            if (IsKeyDown(Keys.D))
                camera.MoveBy(Vector3.Cross(camera.vkCameraOrtho, camera.vkCamera) * duMove);

            const float dbrightnessDim = 0.01f;

            if (IsKeyDown(Keys.NumPad7))
                raytracer.lightManager[0].brightness -= dbrightnessDim;
            if (IsKeyDown(Keys.NumPad8))
                raytracer.lightManager[0].brightness += dbrightnessDim;
            if (IsKeyDown(Keys.NumPad4))
                DimBallLights(-0.01f);
            if (IsKeyDown(Keys.NumPad5))
                DimBallLights(0.01f);

            const float dsf = 0.01f;
            if (IsKeyDown(Keys.NumPad1))
            {
                raytracer._raytracerfractal.sfR += dsf;
                if (raytracer._raytracerfractal.sfR > 1.0)
                    raytracer._raytracerfractal.sfR = 0.0f;
            }
            if (IsKeyDown(Keys.NumPad2))
            {
                raytracer._raytracerfractal.sfG += dsf;
                if (raytracer._raytracerfractal.sfG > 1.0)
                    raytracer._raytracerfractal.sfG = 0.0f;
            }
            if (IsKeyDown(Keys.NumPad3))
            {
                raytracer._raytracerfractal.sfB += dsf;
                if (raytracer._raytracerfractal.sfB > 1.0)
                    raytracer._raytracerfractal.sfB = 0.0f;
            }

            if (scene.fractal.cinputFloat > 0)
            {
                float dParam = 0.01f;
                if (IsKeyDown(Keys.Z))
                    scene.fractal.SetInputFloat(0, scene.fractal.GetInputFloat(0) - dParam);

                if (IsKeyDown(Keys.C))
                    scene.fractal.SetInputFloat(0, scene.fractal.GetInputFloat(0) + dParam);
            }
        }
    }
}
