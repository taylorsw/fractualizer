using System.Diagnostics;
using System.Windows.Forms;
using Evtc;
using Fractals;
using SharpDX;
using Util;

namespace Mandelbasic
{
    public class StageMandelboxFlythroughAudio : Stage
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

        public StageMandelboxFlythroughAudio(Form form, Controller controller, int width, int height)
        {
            evtc = new EvtcAcidHighway(form, controller);
            raytracer = new RaytracerFractal(new Scene(new Mandelbox(), ((EvtcAudio)evtc).StSong().GetHashCode()), width, height);
        }

        private class EvtcAcidHighway : EvtcAudio
        {
            private Mandelbox mandelbox => (Mandelbox)scene.fractal;

            private float sfBrightnessMin = 0.8f;
            private float sfBrightnessMax = 1.2f;
            private PointLight pointLightCamera;
            private AvarLinearDiscrete<TavarNone> avarSf;
            private AvarLinearDiscrete<TavarNone> avarTwist;
            private AvarIndefinite<TavarNone> avarRollDrop;
            private AvarIndefinite<TavarNone> avarRollNonDrop;

            private Sptl[] rgsptl;

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
                raytracer._raytracerfractal.fSkysphere = false;
                raytracer._raytracerfractal.cmarch = 140;
                mandelbox._mandelbox.fGradientColor = 0;
                mandelbox._mandelbox.fAdjustAdditional = false;


                mandelbox._mandelbox.sfTwist = 0;
                mandelbox._mandelbox.xTwistStart = 0.17f;
                double sfTwistMin = -50;
                double sfTwistMax = 50;
                double dtwist_dtms = (sfTwistMax - sfTwistMin) / 3000;
                avarTwist = AvarLinearDiscrete<TavarNone>.BounceBetween(
                    _ => mandelbox._mandelbox.sfTwist,
                    (_, sf) =>
                    {
                        var xFixed = camera.ptCamera.X;
                        float xStart = (float) (xFixed - mandelbox._mandelbox.sfTwist * (xFixed - mandelbox._mandelbox.xTwistStart) / sf);
                        mandelbox._mandelbox.sfTwist = (float)sf;
                        mandelbox._mandelbox.xTwistStart = xStart;
                    },
                    valMin: sfTwistMin,
                    valMax: sfTwistMax,
                    dval_dtms: dtwist_dtms);

                amgr.Tween(avarTwist);

                mandelbox._mandelbox.ptTrap = rgptOrbitTrap[0];

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

                const float dagd_dtms = 360 / 20000f;
                avarRollDrop = new AvarIndefinite<TavarNone>((_, dtms) => camera.RollBy(-dagd_dtms * (float)dtms));
                avarRollNonDrop = new AvarIndefinite<TavarNone>((_, dtms) => camera.RollBy(dagd_dtms / 3 * (float)dtms));
                amgr.Tween(avarRollNonDrop);

                amgr.Tween(
                    new AvarIndefinite<TavarNone>(
                        (_, dtms) => camera.MoveBy(new Vector3(dx_dtmsCamera * (float)dtms, 0, 0))));
            }

            readonly Avark avarkLight = Avark.New();
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
                }
                base.OnKeyUp(keyEventArgs);
            }

            public override void DoEvents(float dtms)
            {
                Debug.WriteLine(camera.ptCamera);
                pointLightCamera.ptLight = camera.ptCamera;// - new Vector3(0.05f, 0f, 0);

                foreach (Sptl sptl in rgsptl)
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
