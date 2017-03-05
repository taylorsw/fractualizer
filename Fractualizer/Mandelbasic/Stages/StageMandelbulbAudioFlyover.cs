using System.Windows.Forms;
using EVTC;
using Fractals;
using SharpDX;
using Util;

namespace Mandelbasic
{
    public class StageMandelbulbAudioFlyover : Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelbulbAudioFlyover(Form form, Controller controller, int width, int height)
        {
            raytracer = new RaytracerFractal(new Scene(new Mandelbulb(), seed: 6969), width, height);
            evtc = new EvtcMandelbulbAnim(form, controller);
        }

        private class EvtcMandelbulbAnim : EvtcAudio
        {
            private RailHover railCam;

            const int cballlight = 25;
            const float duCutoffBallLight = 0.3f;
            private RailHover[] rgrailHoverBallLight;

            public EvtcMandelbulbAnim(Form form, Controller controller) : base(form, controller)
            {
            }

            public override string StSong() => "callonme.mp3";
            public override void Setup()
            {
                raytracer._raytracerfractal.fSkysphere = true;
                base.Setup();
                camera.MoveTo(new Vector3(0, 0, -1.5f));
                camera.LookAt(Vector3.Zero);

                lightManager.AddLight(new PointLight(new Vector3f(2, 0, -1), Vector3.One, brightness: 0.4f, fVisualize: false));

                const float duHover = 0.6f;
                railCam = new RailHover(
                    pt => camera.MoveTo(pt),
                    scene.fractal,
                    ptCenter: Vector3.Zero,
                    ptInitial: camera.ptCamera,
                    vkNormal: scene.rand.VkUnitRand(),
                    dtmsRevolution: 20000,
                    duHover: duHover,
                    duduAdjustMax: duHover / 5,
                    dududuAdjustMax: duHover / 10);

                rgrailHoverBallLight = new RailHover[cballlight];
                for (int iballlight = 0; iballlight < cballlight; iballlight++)
                {
                    BallLight ballLight = new BallLight(rand.VkUnitRand() * 2.0f,
                        new Vector3(0, rand.NextFloat(0.2f, 1.0f), rand.NextFloat(0.2f, 1.0f)), duCutoffBallLight,
                        brightness: rand.NextFloat(1.5f, 2.5f), fVisualize: false);
                    lightManager.AddLight(ballLight);

                    RailHover railHover = new RailHover(
                        dgUpdatePt: pt => ballLight.ptLight = pt,
                        fractal: scene.fractal,
                        ptCenter: Vector3.Zero,
                        ptInitial: rand.VkUnitRand() * 2,
                        vkNormal: rand.VkUnitRand(),
                        dtmsRevolution: rand.NextFloat(5000, 10000),
                        duHover: duCutoffBallLight / 5);
                    rgrailHoverBallLight[iballlight] = railHover;
                }
            }

            private float du = -1;
            private float du2 = -1;
            public override void DoEvents(float dtms)
            {
                base.DoEvents(dtms);

                lightManager[0].ptLight = camera.ptCamera;

                Mandelbulb mandelbulb = scene.fractal as Mandelbulb;
                if (mandelbulb != null)
                {
                    mandelbulb._mandelbulb.param += du * 0.007f;

                    if (mandelbulb._mandelbulb.param < 2.5)
                        du = 1;
                    else if (mandelbulb._mandelbulb.param > 8)
                        du = -1;

                    mandelbulb._mandelbulb.param2 += du2 * 0.00014f;

                    if (mandelbulb._mandelbulb.param2 < 1.5)
                        du2 = 1;
                    else if (mandelbulb._mandelbulb.param2 > 3.5)
                        du2 = -1;
                }

                railCam.UpdatePt(dtms);
                for (int irailHover = 0; irailHover < rgrailHoverBallLight.Length; irailHover++)
                {
                    int ilight = irailHover;
                    rgrailHoverBallLight[irailHover].UpdatePt(dtms);
                }

                const float dagdRoll = 0.03f;
                camera.RollBy(dagdRoll);

                camera.LookAt(Vector3.Zero);
                camera.RotateCamera(camera.vkCameraRight, MathUtil.DegreesToRadians(10));
            }

            protected override void OnBeat()
            {
                base.OnBeat();
                for (int ilight = 2; ilight < lightManager.clight; ilight++)
                {
                    lightManager[ilight].rgbLight = new Vector3(1, 1, 1) - lightManager[ilight].rgbLight;
                }
            }
        }
    }
}
