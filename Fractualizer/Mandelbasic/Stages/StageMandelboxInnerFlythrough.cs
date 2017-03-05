using System.Diagnostics;
using System.Windows.Forms;
using EVTC;
using Fractals;
using SharpDX;
using Util;

namespace Mandelbasic
{
    public class StageMandelboxInnerFlythrough : Stage
    {
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

        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelboxInnerFlythrough(Form form, Controller controller, int width, int height)
        {
            evtc = new EvtcInnerFlythrough(form, controller);
            raytracer = new RaytracerFractal(new Scene(new Mandelbox(), ((EvtcAudio)evtc).StSong().GetHashCode()), width, height);
        }

        private class EvtcInnerFlythrough : EvtcAudio
        {
            private Mandelbox mandelbox => (Mandelbox)scene.fractal;

            private float sfBrightnessMin = 0.8f;
            private float sfBrightnessMax = 1.2f;
            private PointLight pointLightCamera;
            private Vector3 vkRailCamera;
            private RailOrbit railOrbitCamera;
            private AvarLinearDiscrete<TavarNone> avarSf;

            private Sptl[] rgsptl;

            private const float duOrbitTrap = 2f;
            private int iptOrbitTrap;
            private Vector3[] rgptOrbitTrap;

            public EvtcInnerFlythrough(Form form, Controller controller) : base(form, controller) { }

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
                raytracer._raytracerfractal.fSkysphere = false;
                raytracer._raytracerfractal.cmarch = 140;
                mandelbox._mandelbox.fGradientColor = 0;
                mandelbox._mandelbox.fAdjustAdditional = false;
                mandelbox._mandelbox.ptTrap = rgptOrbitTrap[6];

                pointLightCamera = new PointLight(camera.ptCamera, Vector3.One, brightness: sfBrightnessMin, fVisualize: false);
                lightManager.AddLight(pointLightCamera);

                const int cspotlight = 15;
                rgsptl = new Sptl[cspotlight];
                for (int ispotlight = 0; ispotlight < cspotlight; ispotlight++)
                {
                    SpotLight spotlight = new SpotLight(
                        ptLight: camera.ptCamera,
                        rgbLight: Vector3.One,
                        vkLight: VkSpotlight(),
                        agdRadius: rand.NextFloat(0.7f, 1.5f),
                        brightness: rand.NextFloat(0.1f, 0.8f));
                    Sptl.UpdateSpotlight(spotlight, camera.ptCamera);
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
                    rgsptl[ispotlight] = new Sptl(
                        spotlight,
                        railSpotlight,
                        agdRadiusMin: agdRadiusMin,
                        agdRadiusMax: agdRadiusMax,
                        dagd_dtmsRadius: rand.NextFloat(0.7f, 1.5f) / 100,
                        agdRotationMin: agdRotationMin,
                        agdRotationMax: agdRotationMax,
                        dagd_dtmsRotation: rand.NextFloat(1, 5) / 10);
                }

                mandelbox._mandelbox.vkTwistY = -0.479544f;
                mandelbox._mandelbox.vkTwistZ = -0.5555527f;

                railOrbitCamera = new RailOrbit(pt => vkRailCamera = pt, Vector3.Zero, new Vector3(0, 0.04f, 0), new Vector3(1, 0, 0), 20000);
                camera.MoveTo(new Vector3(0.5f, -0.479544f, -0.5555527f));
                camera.LookAt(camera.ptCamera - new Vector3(1, 0, 0));

                double sfMin = 1.8;
                double sfMax = 2.33;
                double dval_dtms = (sfMax - sfMin) / (rand.NextFloat(1, 2) * 7000);
                avarSf = AvarLinearDiscrete<TavarNone>.BounceBetween(
                    _ => mandelbox._mandelbox.sf,
                    (_, sf) => mandelbox._mandelbox.sf = (float)sf,
                    valMin: sfMin,
                    valMax: sfMax,
                    dval_dtms: dval_dtms);

                //const float dagd_dtms = 360 / 20000f;
                //avarRollDrop = new AvarIndefinite<TavarNone>((_, dtms) => camera.RollBy(-dagd_dtms * (float)dtms));
                //avarRollNonDrop = new AvarIndefinite<TavarNone>((_, dtms) => camera.RollBy(dagd_dtms / 3 * (float)dtms));
                //amgr.Tween(avarRollNonDrop);

                amgr.Tween(
                    new AvarIndefinite<TavarNone>(
                        (_, dtms) => camera.MoveBy(new Vector3(dx_dtmsCamera * (float)dtms, 0, 0))));
                amgr.Tween(AvarTwistGentle(1.0 / 5, 10000));
            }

            readonly Avark avarkLight = Avark.New();
            readonly Avark avarkTwist = Avark.New();
            const float sfTwistMax = 40;
            const float sfTwistRangeStart = sfTwistMax * 0.8f;
            const float dtmsTwistDrop = 6500;
            protected override void OnKeyUp(KeyEventArgs keyEventArgs)
            {
                switch (keyEventArgs.KeyCode)
                {
                    case Keys.L:
                        pointLightCamera.brightness = 2.0f;
                        amgr.Tween(
                            new AvarLinearDiscrete<TavarNone>(
                                pointLightCamera.brightness,
                                sfBrightnessMin,
                                (avar, brightness) => pointLightCamera.brightness = (float)brightness,
                                500,
                                avark: avarkLight));
                        break;
                    case Keys.D1:
                        fCam = false;
                        AnimateTwistTo(mandelbox._mandelbox.sfTwist < 0 ? rand.NextFloat(sfTwistRangeStart, sfTwistMax) : rand.NextFloat(-sfTwistMax, -sfTwistRangeStart), dtmsTwistDrop);
                        Vector3 vkCameraStart = camera.vkCamera;
                        amgr.Tween(
                            new AvarLinearDiscreteQuadraticEaseInOut<TavarNone>(
                                _ => 0,
                                1.0,
                                (_, sf) => camera.LookAt(Vector3.Lerp(camera.ptCamera + vkCameraStart, camera.ptCamera + new Vector3(1.0f, 0, 0), (float)sf)),
                                dtmsTwistDrop / 2));
                        break;
                    case Keys.D2:
                        AnimateTwistTo(1, dtmsTwistDrop);
                        break;
                    case Keys.D3:
                        AnimateFOV(duNearDefault / 6);
                        break;
                    case Keys.D4:
                        AnimateFOV(duNearDefault);
                        break;
                }
                base.OnKeyUp(keyEventArgs);
            }

            private readonly Avark avarkFOV = Avark.New();
            private const float duNearDefault = 0.5f;
            private void AnimateFOV(float duNearDst)
            {
                float duNearStart = camera.duNear;
                Avar avarFOV = new AvarLinearDiscreteQuadraticEaseInOut<TavarNone>(
                    _ => camera.duNear,
                    duNearDst,
                    (_, duNear) => camera.SetDuNear((float) duNear),
                    1000,
                    avark: avarkFOV);
//                Avar avarFOVBounce = AvarLinearDiscreteQuadraticEaseInOut<TavarNone>.BounceBetween(
//                    _ => camera.duNear,
//                    (_, duNear) => camera.SetDuNear((float) duNear),
//                    duNearStart,
//                    duNearDst,
//                    5000,
//                    avark: avarkFOV);
//                avarFOV.SetDgNext(
//                    _ => avarFOVBounce);
                amgr.Tween(
                    avarFOV);
            }

            private void AnimateTwistTo(float sfTwist, float dtmsTwist)
            {
                var avarTwist = new AvarLinearDiscreteQuadraticEaseInOut<TavarNone>(
                    _ => mandelbox._mandelbox.sfTwist,
                    sfTwist,
                    (_, sf) => SetSfTwist(sf),
                    dtmsTwist,
                    avark: avarkTwist);
                if (sfTwist != 0)
                {
                    avarTwist.SetDgNext(
                        prev => AvarTwistGentle(1.0 / 3, 5000));
                }
                amgr.Tween(avarTwist);
            }

            private Avar AvarTwistGentle(double sfTwistOfMax, double dtmsOneWay)
            {
                double sfTwistBounceRange = sfTwistMax * sfTwistOfMax;
                return AvarLinearDiscreteQuadraticEaseInOut<TavarNone>.BounceBetween(
                    avar => mandelbox._mandelbox.sfTwist,
                    (avar, sf) => SetSfTwist(sf),
                    mandelbox._mandelbox.sfTwist - sfTwistBounceRange,
                    mandelbox._mandelbox.sfTwist + sfTwistBounceRange,
                    dtmsOneWay,
                    avark: avarkTwist);
            }

            private void SetSfTwist(double sfTwist)
            {
                var xFixed = camera.ptCamera.X;
                float xStart = (float)(xFixed - mandelbox._mandelbox.sfTwist * (xFixed - mandelbox._mandelbox.xTwistStart) / sfTwist);
                mandelbox._mandelbox.sfTwist = (float)sfTwist;
                mandelbox._mandelbox.xTwistStart = xStart; // + 0.17f;
            }

            private bool fCam = true;
            public override void DoEvents(float dtms)
            {
                pointLightCamera.ptLight = camera.ptCamera;// - new Vector3(0.05f, 0f, 0);

                foreach (Sptl sptl in rgsptl)
                {
                    sptl.Update(dtms, camera.ptCamera);
                }

                if (fCam)
                {
                    railOrbitCamera.UpdatePt(dtms);
                    camera.LookAt(camera.ptCamera + vkRailCamera + new Vector3(-0.1f, 0, 0));
                }

                base.DoEvents(dtms);
            }

            protected override void OnDropBegin()
            {
                amgr.Tween(avarSf);
                pointLightCamera.brightness = sfBrightnessMax;
                base.OnDropBegin();
            }

            protected override void OnDropEnd()
            {
                amgr.Cancel(avarSf);
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
                    amgr.Tween(AvarLinearDiscrete<TavarNone>.LerpPt(ptStart, ptEnd, dtmsPeriod, pt => mandelbox._mandelbox.ptTrap = pt));

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
