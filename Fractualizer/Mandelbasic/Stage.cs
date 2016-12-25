using System;
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

        public override void Setup()
        {
            raytracer._raytracerfractal.cmarch = 200;
            base.Setup();
        }
    }

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
                raytracer._raytracerfractal.fSkysphere = 1;
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

            public static void UpdateSpotlight(SpotLight spotlight, Vector3 ptCamera)
            {
                spotlight.ptLight = ptCamera - new Vector3(0.1f, 0, 0);
            }

            private int signRadius = 1;
            private int signRotation = 1;
            public void Update(float dtms, Vector3 ptCamera)
            {
                UpdateSpotlight(spotlight, ptCamera);
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

            public override string StSong() => "moby.mp3";
            public override void Setup()
            {
                base.Setup();
                camera.MoveTo(new Vector3(0, 0, -1.5f));
                camera.LookAt(Vector3.Zero);

                lightManager.AddLight(new PointLight(new Vector3f(2, 0, -1), Vector3.One, brightness: 0.25f, fVisualize: false));

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
                if (mandelbulb != null)
                {
                    mandelbulb._mandelbulb.param += du * 0.002f;

                    if (mandelbulb._mandelbulb.param < 2.5)
                        du = 1;
                    else if (mandelbulb._mandelbulb.param > 8)
                        du = -1;

                    mandelbulb._mandelbulb.param2 += du2 * 0.00008f;

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
//                foreach (Sptl sptl in rgsptl)
//                {
//                    sptl.rail.SetVkNormal(rand.VkUnitRand());
//                }
            }
        }
    }

    public class StageMandelboxWaveFlythrough : Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelboxWaveFlythrough(Form form, Controller controller, int width, int height)
        {
            evtc = new EvtcWave(form, controller);
            raytracer = new RaytracerFractal(new Scene(new Mandelbox(), ((EvtcAudio) evtc).StSong().GetHashCode()),
                width, height);
        }

        private class EvtcWave : EvtcAudio
        {
            private PointLight pointLightCamera;
            private Vector3 ptRailLight;
            private RailOrbit railOrbit;
            private RailPt railOrbitTrap;
            private Vector3 ptOrbitTrap;
            private RailBounceBetween railSf;
            private Mandelbox mandelbox => scene.fractal as Mandelbox;
            public EvtcWave(Form form, Controller controller) : base(form, controller)
            {
            }

            public override string StSong() => "moby.mp3";

            public override void Setup()
            {
                base.Setup();
                raytracer._raytracerfractal.fSkysphere = 1;
                raytracer._raytracerfractal.cmarch = 140;
                mandelbox._mandelbox.fGradientColor = 1;
                mandelbox._mandelbox.fAdjustAdditional = true;
                mandelbox._mandelbox.sfRollx = 0;
                mandelbox._mandelbox.duMirrorPlane = 1.0f;
                mandelbox._mandelbox.sfSin = 0.0f;
                ptOrbitTrap = new Vector3(0, 0, 0);
                camera.MoveTo(new Vector3(0, mandelbox._mandelbox.duMirrorPlane, 0));
                camera.LookAt(camera.ptCamera - new Vector3(1, 0, 0));
                pointLightCamera = new PointLight(camera.ptCamera, Vector3.One, 1.2f, false);
                railOrbit = new RailOrbit(pt => ptRailLight = pt, Vector3.Zero, new Vector3(0, 0.2f, 0), new Vector3(1, 0, 0), 14.77f * 1000);
                lightManager.AddLight(pointLightCamera);
                railSf = new RailBounceBetween(rand.NextFloat(1, 2) * 7000, 2, 1.9f, 4.5f);
            }

            protected override void OnKeyUp(KeyEventArgs keyEventArgs)
            {
                if (keyEventArgs.KeyCode == Keys.N)
                {
                    railOrbitTrap = new RailLinear(
                        ptOrbitTrap,
                        rand.VkUnitRand(),
                        500,
                        pt => ptOrbitTrap = pt);
                }
                base.OnKeyUp(keyEventArgs);
            }

            const float sfSinMin = 0;
            const float sfSinMax = 1.0f;
            const float sfSin_dtms = sfSinMax/2000;
            const float sfRollx_dtms = -(float)Math.PI/2000;
            private bool fSwitch = false;
            public override void DoEvents(float dtms)
            {
                base.DoEvents(dtms);

                const float dxCamera_dtms = 2/3000f;
                camera.MoveTo(camera.ptCamera - new Vector3(dxCamera_dtms * dtms, 0, 0));

                railOrbit.UpdatePt(dtms);
                pointLightCamera.ptLight = camera.ptCamera + ptRailLight;
                //pointLightCamera.ptLight = camera.ptCamera + new Vector3(-0.02f, 0, 0);

                railOrbitTrap?.UpdatePt(dtms);
                mandelbox._mandelbox.ptTrap = ptOrbitTrap;

                const float duXroll_dtms = -1/3000f;
                const float dagdRoll_dtms = 36f/2000;
                if (fDrop)
                {
                    railSf.UpdateValue(dtms);
                    mandelbox._mandelbox.sf = railSf.val;
                    camera.RollBy(2 * dagdRoll_dtms * dtms);
                    mandelbox._mandelbox.sfRollx -= 2 * duXroll_dtms * dtms;

                    if (mandelbox._mandelbox.sfSin < sfSinMax)
                        mandelbox._mandelbox.sfSin += sfSin_dtms*dtms;
                    mandelbox._mandelbox.sfRollx += sfRollx_dtms*dtms;
                }
                else
                {
                    railSf.UpdateValue(dtms / 8);
                    camera.RollBy(dagdRoll_dtms * dtms);
                    mandelbox._mandelbox.sfRollx -= duXroll_dtms * dtms;

                    if (mandelbox._mandelbox.sfSin > sfSinMin)
                        mandelbox._mandelbox.sfSin -= sfSin_dtms * dtms;
                }
            }

            protected override void OnDropBegin()
            {
                fSwitch = true;
                base.OnDropBegin();
            }

            protected override void OnDropEnd()
            {
                base.OnDropEnd();
            }

            protected override void OnBeat()
            {
                base.OnBeat();
            }
        }
    }

    public class StageMandelboxFlythroughAudio: Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelboxFlythroughAudio(Form form, Controller controller, int width, int height)
        {
            evtc = new EvtcAcidHighway(form, controller);
            raytracer = new RaytracerFractal(new Scene(new Mandelbox(), ((EvtcAudio)evtc).StSong().GetHashCode()), width, height);
        }

        private class EvtcAcidHighway : EvtcAudio
        {
            private Mandelbox mandelbox => (Mandelbox) scene.fractal;

            private float sfBrightnessMin = 0.8f;
            private float sfBrightnessMax = 1.2f;
            private PointLight pointLightCamera;
            private AvarLinearDiscrete<TavarNone> avarSf;
            private AvarIndefinite<TavarNone> avarRollDrop;
            private AvarIndefinite<TavarNone> avarRollNonDrop;

            private StageMandelbulbAudioCloseup.Sptl[] rgsptl;

            private const float duOrbitTrap = 2f;
            private int iptOrbitTrap;
            private Vector3[] rgptOrbitTrap;
            
            public EvtcAcidHighway(Form form, Controller controller) : base(form, controller) { }

            public override string StSong() => "faded.mp3";

            const float dx_dtmsCamera = -1.0f / 10000;
            const float duVkSpotlightRange = 1f;
            private Vector3 VkSpotlight() => new Vector3(-0.5f, rand.NextFloat(-duVkSpotlightRange, duVkSpotlightRange), rand.NextFloat(-duVkSpotlightRange, duVkSpotlightRange)).Normalized();
            public override void Setup()
            {
                base.Setup();

                //                int ctrap = rand.Next(20, 30);
                //                rgptOrbitTrap = new Vector3[ctrap];
                //                for (int itrap = 0; itrap < ctrap; itrap++)
                //                    rgptOrbitTrap[itrap] = new Vector3((rand.Next(0, 2) == 1 ? 1 : -1) + rand.NextFloat(-0.1f, 0.1f),
                //                        (rand.Next(0, 2) == 1 ? 1 : -1) + rand.NextFloat(-0.1f, 0.1f),
                //                        (rand.Next(0, 2) == 1 ? 1 : -1) + rand.NextFloat(-0.1f, 0.1f));
                rgptOrbitTrap = new[]
                {
                    duOrbitTrap*new Vector3(1, 1, 1),
                    duOrbitTrap*new Vector3(1, 1, -1),
                    duOrbitTrap*new Vector3(1, -1, -1),
                    duOrbitTrap*new Vector3(-1, -1, -1),
                    duOrbitTrap*new Vector3(-1, 1, -1),
                    duOrbitTrap*new Vector3(-1, 1, 1),
                    duOrbitTrap*new Vector3(-1, -1, 1),
                    duOrbitTrap*new Vector3(1, -1, 1),
                };

                raytracer._raytracerfractal.sfEpsilonShadow = 4.0f;
                raytracer._raytracerfractal.fSkysphere = 0;
                raytracer._raytracerfractal.cmarch = 140;
                mandelbox._mandelbox.fGradientColor = 0;
                mandelbox._mandelbox.fAdjustAdditional = false;

                mandelbox._mandelbox.ptTrap = rgptOrbitTrap[0];

                pointLightCamera = new PointLight(camera.ptCamera, Vector3.One, brightness: sfBrightnessMin, fVisualize: false);
                lightManager.AddLight(pointLightCamera);

                const int cspotlight = 15;
                rgsptl = new StageMandelbulbAudioCloseup.Sptl[cspotlight];
                for (int ispotlight = 0; ispotlight < cspotlight; ispotlight++)
                {
                    SpotLight spotlight = new SpotLight(
                        ptLight: camera.ptCamera,
                        rgbLight: Vector3.One,
                        vkLight: VkSpotlight(),
                        agdRadius: rand.NextFloat(0.7f, 1.5f),
                        brightness: rand.NextFloat(0.1f, 0.8f));
                    StageMandelbulbAudioCloseup.Sptl.UpdateSpotlight(spotlight, camera.ptCamera);
                    lightManager.AddLight(spotlight);

                    RailSpotlight railSpotlight = new RailSpotlight(
                        dgUpdateVkSpotlight: vk => spotlight.vkLight = vk,
                        agdRadius: rand.NextFloat(20, 70),
                        vkNormal: VkSpotlight(),
                        dtmsRevolution: rand.NextFloat(1, 4) * 10000);
                    float agdRadiusMin = rand.NextFloat(5, 10);
                    float agdRadiusMax = rand.NextFloat(11, 20);
                    float agdRotationMin = rand.NextFloat(45, 60);
                    float agdRotationMax = rand.NextFloat(70, 90);
                    rgsptl[ispotlight] = new StageMandelbulbAudioCloseup.Sptl(
                        spotlight,
                        railSpotlight,
                        agdRadiusMin: agdRadiusMin,
                        agdRadiusMax: agdRadiusMax,
                        dagd_dtmsRadius: rand.NextFloat(0.7f, 1.5f) / 100,
                        agdRotationMin: agdRotationMin,
                        agdRotationMax: agdRotationMax,
                        dagd_dtmsRotation: rand.NextFloat(1, 5) / 10);
                }

                camera.MoveTo(new Vector3(0.5f, -0.479544f, -0.5555527f));
                camera.LookAt(camera.ptCamera - new Vector3(1, 0, 0));

                double sfMin = 1.8;
                double sfMax = 2.33;
                double dval_dtms = (sfMax - sfMin) / (rand.NextFloat(1, 2)*7000);
                avarSf = AvarLinearDiscrete<TavarNone>.BounceBetween(
                    _ => mandelbox._mandelbox.sf,
                    (_, sf) => mandelbox._mandelbox.sf = (float)sf,
                    valMin: sfMin,
                    valMax: sfMax,
                    dval_dtms: dval_dtms);

                const float dagd_dtms = 360 / 20000f;
                avarRollDrop = new AvarIndefinite<TavarNone>((_, dtms) => camera.RollBy(-dagd_dtms * (float)dtms));
                avarRollNonDrop = new AvarIndefinite<TavarNone>((_, dtms) => camera.RollBy(dagd_dtms / 3 * (float)dtms));
                amgr.Tween(avarRollNonDrop);

                amgr.Tween(
                    new AvarIndefinite<TavarNone>(
                        (_, dtms) => camera.MoveBy(new Vector3(dx_dtmsCamera * (float)dtms, 0, 0))));
            }

            public override void DoEvents(float dtms)
            {
                Debug.WriteLine(camera.ptCamera);
                pointLightCamera.ptLight = camera.ptCamera;// - new Vector3(0.05f, 0f, 0);

                foreach (StageMandelbulbAudioCloseup.Sptl sptl in rgsptl)
                {
                    sptl.Update(dtms, camera.ptCamera);
                }

                base.DoEvents(dtms);
            }

            protected override void OnDropBegin()
            {
                amgr.Tween(avarSf);
                amgr.Cancel(avarRollNonDrop);
                amgr.Tween(avarRollDrop);
                pointLightCamera.brightness = sfBrightnessMax;
                base.OnDropBegin();
            }

            protected override void OnDropEnd()
            {
                amgr.Cancel(avarSf);
                amgr.Cancel(avarRollDrop);
                amgr.Tween(avarRollNonDrop);
                pointLightCamera.brightness = sfBrightnessMin;
                base.OnDropEnd();
            }

            protected override void OnBeat()
            {
                if (fDrop)
                {
                    float dtmsBeatInterval = DtmsBeatInterval();
                    if (dtmsBeatInterval <= 0)
                        return;

                    float dtmsPeriod = dtmsBeatInterval / 8;
                    Vector3f ptStart = mandelbox._mandelbox.ptTrap;
                    Vector3f ptEnd = rgptOrbitTrap[iptOrbitTrap];
                    amgr.Tween(
                        new AvarLinearDiscrete<TavarNone>(
                            0,
                            1.0,
                            (avar, fr) => mandelbox._mandelbox.ptTrap = U.Lerp(ptStart, ptEnd, fr),
                            dtmsPeriod));
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
