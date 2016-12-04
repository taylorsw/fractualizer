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

        const int cballlight = 20;
        const float duCutoffBallLight = 0.3f;
        protected readonly RailHover[] rgrailHoverBallLight;

        public EvtcAnim(Form form, Controller controller) : base(form, controller)
        {
            raytracer.camera.MoveTo(new Vector3(0, 0, -1.5f));
            raytracer.camera.LookAt(Vector3.Zero);

            Light pointLight = new PointLight(new Vector3f(2, 0, -1), new Vector3(0.5f, 0.5f, 0.5f), brightness: 0.3f, fVisualize: false);
            raytracer.lightManager.AddLight(pointLight);

            railCam = new RailHover(
                pt => raytracer.camera.MoveTo(pt), 
                scene.fractal,
                ptCenter: Vector3.Zero,
                ptInitial: raytracer.camera.ptCamera,
                vkNormal: new Vector3(scene.rand.NextFloat(-1.0f, 1.0f), scene.rand.NextFloat(-1.0f, 1.0f), scene.rand.NextFloat(-1.0f, 1.0f)), 
                dtmsRevolution: 20000,
                duHoverMin: 0.3f,
                duHoverMax: 0.5f,
                sfTravelMax: 3);

            rgrailHoverBallLight = new RailHover[cballlight];
            for (int iballlight = 0; iballlight < cballlight; iballlight++)
            {
                raytracer.lightManager.AddLight(new BallLight(rand.VkUnitRand() * 2.0f, new Vector3(0, rand.NextFloat(0.2f, 1.0f), rand.NextFloat(0.2f, 1.0f)), duCutoffBallLight, brightness: 1.5f));

                int ilight = iballlight;
                RailHover railHover = new RailHover(
                    dgUpdatePt: pt => raytracer.lightManager[ilight].ptLight = pt,
                    fractal: scene.fractal,
                    ptCenter: Vector3.Zero,
                    ptInitial: rand.VkUnitRand() * 2,
                    vkNormal: rand.VkUnitRand(),
                    dtmsRevolution: rand.NextFloat(5000, 10000),
                    duHoverMin: duCutoffBallLight / 5,
                    duHoverMax: duCutoffBallLight / 5,
                    sfTravelMax: 10.0f);
                rgrailHoverBallLight[iballlight] = railHover;
            }
        }

        protected float du = -1;
        protected float du2 = -1;

        public override void DoEvents(float dtms)
        {
            raytracer.lightManager[0].ptLight = raytracer.camera.ptCamera;

            Mandelbulb mandelbulb = scene.fractal as Mandelbulb;
            if (mandelbulb != null)
            {
                mandelbulb._mandelbulb.param += du * 0.014f;

                if (mandelbulb._mandelbulb.param < 2)
                    du = 1;
                else if (mandelbulb._mandelbulb.param > 8)
                    du = -1;

                mandelbulb._mandelbulb.param2 += du2 * 0.00014f;

                if (mandelbulb._mandelbulb.param2 < 1.0)
                    du2 = 1;
                else if (mandelbulb._mandelbulb.param2 > 3.0)
                    du2 = -1;
            }

            railCam.UpdatePt(dtms);
            for (int irailHover = 0; irailHover < rgrailHoverBallLight.Length; irailHover++)
            {
                int ilight = irailHover;
                rgrailHoverBallLight[irailHover].UpdatePt(dtms);
            }

            const float dagdRoll = 0.01f;
            camera.RollBy(dagdRoll);

            camera.LookAt(Vector3.Zero);
            camera.RotateCamera(camera.vkCameraRight, MathUtil.DegreesToRadians(10));
        }
    }
}
