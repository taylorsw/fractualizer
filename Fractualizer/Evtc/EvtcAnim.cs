using System;
using System.Windows.Forms;
using Evtc;
using Fractals;
using SharpDX;
using Util;

namespace Mandelbasic
{
    public class EvtcAnim : Evtc
    {
        private readonly RailHover railCam;

        const int cballlight = 35;
        const float duCutoffBallLight = 0.3f;
        protected readonly RailHover[] rgrailHoverBallLight;

        public EvtcAnim(Form form, Controller controller) : base(form, controller)
        {
            raytracer.camera.MoveTo(new Vector3(0, 0, -1.5f));
            raytracer.camera.LookAt(Vector3.Zero);

            raytracer.lightManager.AddLight(new PointLight(new Vector3f(2, 0, -1), ColorU.rgbWhite));
            raytracer.lightManager.AddLight(new PointLight(new Vector3f(-2, 0, -1.5f), ColorU.rgbWhite));

            railCam = new RailHover(
                pt => raytracer.camera.MoveTo(pt), 
                scene.fractal,
                ptCenter: Vector3.Zero, 
                vkNormal: new Vector3(scene.rand.NextFloat(-1.0f, 1.0f), scene.rand.NextFloat(-1.0f, 1.0f), scene.rand.NextFloat(-1.0f, 1.0f)), 
                agd_dtms: 20f / 1000,
                duHoverMin: 0.3f,
                duHoverMax: 0.5f,
                sfTravelMax: 3);

            rgrailHoverBallLight = new RailHover[cballlight];
            for (int iballlight = 0; iballlight < cballlight; iballlight++)
            {
                raytracer.lightManager.AddLight(new BallLight(rand.VkUnitRand() * 2.0f, rand.VkUnitRand(0.2f), duCutoffBallLight));

                int ilight = iballlight;
                RailHover railHover = new RailHover(pt => raytracer.lightManager[ilight].ptLight = pt,
                    scene.fractal,
                    Vector3.Zero,
                    rand.VkUnitRand(),
                    rand.NextFloat(0.01f, 0.05f),
                    duCutoffBallLight / 5,
                    duCutoffBallLight / 5,
                    10.0f);
                rgrailHoverBallLight[iballlight] = railHover;
            }
        }

        protected float du = -1;
        protected float du2 = -1;

        public override void DoEvents(float dtms)
        {
//            scene.camera.param += du * 0.0014f;
//
//            if (scene.camera.param < 2)
//                du = 1;
//            else if (scene.camera.param > 8.5)
//                du = -1;
//
//            scene.camera.param2 += du2 * 0.000014f;
//
//            if (scene.camera.param2 < 1.0)
//                du2 = 1;
//            else if (scene.camera.param2 > 3.0)
//                du2 = -1;

            railCam.UpdatePt(camera.ptCamera, dtms);
            for (int irailHover = 0; irailHover < rgrailHoverBallLight.Length; irailHover++)
            {
                int ilight = irailHover;
                rgrailHoverBallLight[irailHover].UpdatePt(raytracer.lightManager[ilight].ptLight, dtms);
            }

            const float dagdRoll = 0.01f;
            camera.RollBy(dagdRoll);

            camera.LookAt(Vector3.Zero);
            camera.RotateCamera(camera.vkCameraRight, MathUtil.DegreesToRadians(20));
        }
    }
}
