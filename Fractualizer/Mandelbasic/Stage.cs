using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Audio;
using Evtc;
using Fractals;
using SharpDX;
using Util;

namespace Mandelbasic
{
    public abstract class Stage
    {
        public abstract RaytracerFractal raytracer { get; }
        public abstract Evtc evtc { get; }

        public virtual void Setup()
        {
            evtc.Setup();
        }
    }

    public class StageMandelbulbExplorer : Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelbulbExplorer(Form form, Controller controller, int width, int height)
        {
            raytracer = new RaytracerFractal(new Scene(new Mandelbulb()), width, height);
            evtc = new EvtcExplorer(form, controller);
        }
    }

    public class StageMandelboxExplorer : Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelboxExplorer(Form form, Controller controller, int width, int height)
        {
            raytracer = new RaytracerFractal(new Scene(new Mandelbox()), width, height);
            evtc = new EvtcExplorer(form, controller);
        }
    }

    public class StageMandelbulbAudioFlyover : Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelbulbAudioFlyover(Form form, Controller controller, int width, int height)
        {
            raytracer = new RaytracerFractal(new Scene(new Mandelbulb(), seed: 102), width, height);
            evtc = new EvtcMandelbulbAnim(form, controller);
        }

        public override void Setup()
        {
            base.Setup();
            raytracer._raytracerfractal.colorB = new Vector3(235, 227, 172).Normalized();
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

            protected override string StSong() => "callonme.mp3";
            public override void Setup()
            {
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
                    BallLight ballLight = new BallLight(rand.VkUnitRand()*2.0f,
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

    public class StageMandelbulbAudioCloseup : Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelbulbAudioCloseup(Form form, Controller controller, int width, int height)
        {
            raytracer = new RaytracerFractal(new Scene(new Mandelbulb(), seed: 3002), width, height);
            evtc = new EvtcMandelbulbAnim(form, controller);
        }

        public override void Setup()
        {
            base.Setup();
            raytracer._raytracerfractal.colorB = new Vector3(235, 227, 172).Normalized();
        }

        public class Sptl
        {
            public readonly SpotLight spotlight;
            public readonly RailSpotlight rail;
            public readonly float agdRadiusMin, agdRadiusMax, dagd_dtmsRadius, agdRotationMin, agdRotationMax, dagd_dtmsRotation;

            public Sptl(SpotLight spotlight, RailSpotlight rail, float agdRadiusMin, float agdRadiusMax, float dagd_dtmsRadius, float agdRotationMin, float agdRotationMax, float dagd_dtmsRotation)
            {
                this.spotlight = spotlight;
                this.rail = rail;
                this.agdRadiusMin = agdRadiusMin;
                this.agdRadiusMax = agdRadiusMax;
                this.dagd_dtmsRadius = dagd_dtmsRadius;
                this.agdRotationMin = agdRotationMin;
                this.agdRotationMax = agdRotationMax;
                this.dagd_dtmsRotation = dagd_dtmsRotation;
            }

            private int signRadius = 1;
            private int signRotation = 1;
            public void Update(float dtms, Vector3 ptCamera)
            {
                spotlight.ptLight = ptCamera;
                rail.UpdateVkSpotlight(dtms);

                if (spotlight.agdRadius > agdRadiusMax)
                    signRadius = -1;
                else if (spotlight.agdRadius < agdRadiusMin)
                    signRadius = 1;

                if (rail.agdRadius > agdRotationMax)
                    signRotation = -1;
                else if (rail.agdRadius < agdRotationMin)
                    signRotation = 1;

                spotlight.agdRadius += signRadius * dagd_dtmsRadius;
                rail.agdRadius += signRotation * dagd_dtmsRotation;
            }
        }

        private class EvtcMandelbulbAnim : EvtcAudio
        {
            private RailHover railCam;
            const int cspotlight = 30;
            private Sptl[] rgsptl;

            public EvtcMandelbulbAnim(Form form, Controller controller) : base(form, controller)
            {
            }

            protected override string StSong() => "moby.mp3";
            public override void Setup()
            {
                base.Setup();
                camera.MoveTo(new Vector3(0, 0, -1.5f));
                camera.LookAt(Vector3.Zero);

                lightManager.AddLight(new PointLight(new Vector3f(2, 0, -1), Vector3.One, brightness: 0.05f, fVisualize: false));

                const float duHover = 0.05f;
                railCam = new RailHover(
                    pt => camera.MoveTo(pt),
                    scene.fractal,
                    ptCenter: Vector3.Zero,
                    ptInitial: camera.ptCamera,
                    vkNormal: scene.rand.VkUnitRand(),
                    dtmsRevolution: 50000,
                    duHover: duHover,
                    duduAdjustMax: duHover / 5,
                    dududuAdjustMax: duHover / 10);

                rgsptl = new Sptl[cspotlight];
                for (int ispotlight = 0; ispotlight < cspotlight; ispotlight++)
                {
                    SpotLight spotlight = new SpotLight(
                        ptLight: camera.ptCamera,
                        rgbLight: Vector3.One,
                        vkLight: rand.VkUnitRand(),
                        agdRadius: rand.NextFloat(5, 30),
                        brightness: rand.NextFloat(0.1f, 0.8f));
                    lightManager.AddLight(spotlight);

                    RailSpotlight railSpotlight = new RailSpotlight(
                        dgUpdateVkSpotlight: vk => spotlight.vkLight = vk,
                        agdRadius: rand.NextFloat(3, 20),
                        vkNormal: rand.VkUnitRand(),
                        dtmsRevolution: rand.NextFloat(1, 4) * 1000);
                    float agdRadiusMin = rand.NextFloat(5, 10);
                    float agdRadiusMax = rand.NextFloat(11, 20);
                    float agdRotationMin = rand.NextFloat(3, 10);
                    float agdRotationMax = rand.NextFloat(11, 20);
                    rgsptl[ispotlight] = new Sptl(
                        spotlight,
                        railSpotlight,
                        agdRadiusMin: agdRadiusMin,
                        agdRadiusMax: agdRadiusMax,
                        dagd_dtmsRadius: rand.NextFloat(1, 5) / 1000,
                        agdRotationMin: agdRotationMin,
                        agdRotationMax: agdRotationMax,
                        dagd_dtmsRotation: rand.NextFloat(1, 5) / 1000);
                }
            }

            private float du = -1;
            private float du2 = -1;
            public override void DoEvents(float dtms)
            {
                lightManager[0].ptLight = camera.ptCamera;

                Mandelbulb mandelbulb = scene.fractal as Mandelbulb;
                if (mandelbulb != null && false)
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

                foreach (Sptl sptl in rgsptl)
                {
                    sptl.Update(dtms, camera.ptCamera);
                }

                railCam.UpdatePt(dtms);
                camera.LookAt(Vector3.Zero);

                const float dagdRoll = 0.03f;
                camera.RollBy(dagdRoll);
                
                base.DoEvents(dtms);
            }

            protected override void OnBeat()
            {
                base.OnBeat();
                foreach (Sptl sptl in rgsptl)
                {
                    sptl.rail.SetVkNormal(rand.VkUnitRand());
                }
            }
        }
    }

    public class StageMandelboxFlythroughAudio: Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelboxFlythroughAudio(Form form, Controller controller, int width, int height)
        {
            raytracer = new RaytracerFractal(new Scene(new Mandelbox()), width, height);
            evtc = new EvtcAcidHighway(form, controller);
        }

        private class EvtcAcidHighway : EvtcAudio
        {
            private PointLight pointLightCamera;
            private Vector3 ptOrbitTrap;
            private RailPt railOrbitTrap;
            private RailBounceBetween railSf;
            private Mandelbox mandelbox => (Mandelbox) scene.fractal;

            private StageMandelbulbAudioCloseup.Sptl[] rgsptl;

            private const float duOrbitTrap = 2f;
            private int iptOrbitTrap;
            private static readonly Vector3[] rgptOrbitTrap =
            {
                duOrbitTrap * new Vector3(1, 1, 1),
                duOrbitTrap * new Vector3(1, 1, -1),
                duOrbitTrap * new Vector3(1, -1, -1),
                duOrbitTrap * new Vector3(-1, -1, -1),
                duOrbitTrap * new Vector3(-1, 1, -1),
                duOrbitTrap * new Vector3(-1, 1, 1),
                duOrbitTrap * new Vector3(-1, -1, 1),
                duOrbitTrap * new Vector3(1, -1, 1),
            };


            public EvtcAcidHighway(Form form, Controller controller) : base(form, controller) { }

            protected override string StSong() => "faded.mp3";

            private Vector3 VkSpotlight() => new Vector3(-1, rand.NextFloat(-1, 1), rand.NextFloat(-1, 1)).Normalized();
            public override void Setup()
            {
                base.Setup();
                pointLightCamera = new PointLight(camera.ptCamera, Vector3.One, brightness: 0.3f, fVisualize: false);
                lightManager.AddLight(pointLightCamera);

                const int cspotlight = 10;
                rgsptl = new StageMandelbulbAudioCloseup.Sptl[cspotlight];
                for (int ispotlight = 0; ispotlight < cspotlight; ispotlight++)
                {
                    SpotLight spotlight = new SpotLight(
                        ptLight: camera.ptCamera,
                        rgbLight: Vector3.One,
                        vkLight: VkSpotlight(),
                        agdRadius: rand.NextFloat(1, 2),
                        brightness: rand.NextFloat(0.1f, 0.8f));
                    lightManager.AddLight(spotlight);

                    RailSpotlight railSpotlight = new RailSpotlight(
                        dgUpdateVkSpotlight: vk => spotlight.vkLight = vk,
                        agdRadius: rand.NextFloat(3, 20),
                        vkNormal: VkSpotlight(),
                        dtmsRevolution: rand.NextFloat(1, 4) * 300);
                    float agdRadiusMin = rand.NextFloat(5, 10);
                    float agdRadiusMax = rand.NextFloat(11, 20);
                    float agdRotationMin = rand.NextFloat(3, 10);
                    float agdRotationMax = rand.NextFloat(11, 20);
                    rgsptl[ispotlight] = new StageMandelbulbAudioCloseup.Sptl(
                        spotlight,
                        railSpotlight,
                        agdRadiusMin: agdRadiusMin,
                        agdRadiusMax: agdRadiusMax,
                        dagd_dtmsRadius: rand.NextFloat(1, 5) / 1000,
                        agdRotationMin: agdRotationMin,
                        agdRotationMax: agdRotationMax,
                        dagd_dtmsRotation: rand.NextFloat(1, 5) / 1000);
                }

                camera.MoveTo(new Vector3(0.5f, -0.479544f, -0.5555527f));

                railSf = new RailBounceBetween(rand.NextFloat(1, 2) * 7000, 2, 1.8f, 2.33f);
            }

            public override void DoEvents(float dtms)
            {
                const float du_dtms = 1.0f/10000;
                camera.MoveBy(new Vector3(-du_dtms * dtms, 0, 0));
                camera.LookAt(camera.ptCamera - new Vector3(1, 0, 0));

                const float dagd_dtms = 360/20000f;
                camera.RollBy(dagd_dtms * dtms);

                pointLightCamera.ptLight = camera.ptCamera;
                railOrbitTrap?.UpdatePt(dtms);
                mandelbox._mandelbox.ptTrap = ptOrbitTrap;

                foreach (StageMandelbulbAudioCloseup.Sptl sptl in rgsptl)
                {
                    sptl.Update(du_dtms, camera.ptCamera);
                }

                if (fDrop)
                {
                    railSf.UpdateValue(dtms);
                    mandelbox._mandelbox.sf = railSf.val;
                }

                base.DoEvents(dtms);
            }

            protected override void OnBeat()
            {
                if (fDrop)
                {
                    float dtmsBeatInterval = DtmsBeatInterval();
                    if (dtmsBeatInterval <= 0)
                        return;

                    railOrbitTrap = new RailLinear(
                        ptOrbitTrap,
                        rgptOrbitTrap[iptOrbitTrap],
                        dtmsBeatInterval / 8,
                        pt => ptOrbitTrap = pt);
                    iptOrbitTrap = (iptOrbitTrap + 1) % rgptOrbitTrap.Length;
                }
                else
                {
                    foreach (var sptl in rgsptl)
                        sptl.rail.SetVkNormal(VkSpotlight());
                }
            }
        }
    }
}
