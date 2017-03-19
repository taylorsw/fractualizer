using System;
using System.Diagnostics;
using System.Windows.Forms;
using EVTC;
using Fractals;
using SharpDX;
using Util;

namespace Mandelbasic
{
    public class StageMandelboxWaveFlythrough : Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelboxWaveFlythrough(Form form, Controller controller, int width, int height)
        {
            evtc = new EvtcWave(form, controller);
            raytracer = new RaytracerFractal(new Scene(new Mandelbox(), 1932),
                width, height);
        }

        private class EvtcWave : EvtcAudio
        {
            private Mandelbox mandelbox => scene.fractal as Mandelbox;

            private float duSfRollx = 1.0f;

            private PointLight pointLightCamera;
            private Vector3 vkRailCamera;
            private RailOrbit railOrbitCamera;

            // Avarks
            private readonly Avark avarkRoll = Avark.New();
            private readonly Avark avarkSf = Avark.New();
            private readonly Avark avarkSfSin = Avark.New();
            private readonly Avark avarkSfRollx = Avark.New();
            private readonly Avark avarkSfR = Avark.New();
            private readonly Avark avarkSfG = Avark.New();
            private readonly Avark avarkSfB = Avark.New();
            private readonly Avark avarkDuSfRollx = Avark.New();

            // Non-drop
            private AvarIndefinite<TavarNone> avarRollNonDrop;
            private AvarLinearDiscrete<TavarNone> avarBounceSfNonDrop;

            // Drop
            private AvarIndefinite<TavarNone> avarRollDrop;
            private AvarLinearDiscrete<TavarNone> avarBounceSfDrop;

            public EvtcWave(Form form, Controller controller) : base(form, controller)
            {
            }

            public override string StSong() => "indiansummer.mp3";

            const float brightnessCameraLight = 1.2f;
            public override void Setup()
            {
                base.Setup();
                raytracer._raytracerfractal.fSkysphere = true;
                raytracer._raytracerfractal.cmarch = 140;
                mandelbox._mandelbox.fGradientColor = true;
                mandelbox._mandelbox.fAdjustAdditional = true;
                mandelbox._mandelbox.sfRollx = 0;
                mandelbox._mandelbox.duMirrorPlane = 1.0f;
                mandelbox._mandelbox.sfSin = 0.0f;
                camera.MoveTo(new Vector3(0, mandelbox._mandelbox.duMirrorPlane, 0));
                pointLightCamera = new PointLight(camera.ptCamera, Vector3.One, brightnessCameraLight, false);
                railOrbitCamera = new RailOrbit(pt => vkRailCamera = pt, Vector3.Zero, new Vector3(0, 0.04f, 0), new Vector3(1, 0, 0), 20000);
                lightManager.AddLight(pointLightCamera);

                // Avars
                const float dagdRoll_dtms = 36f / 2000;
                avarRollNonDrop = new AvarIndefinite<TavarNone>(
                    (avar, dtms) => camera.RollBy(dagdRoll_dtms * (float)dtms),
                    avark: avarkRoll);
                avarRollDrop = new AvarIndefinite<TavarNone>(
                    (avar, dtms) => camera.RollBy(2*dagdRoll_dtms*(float) dtms),
                    avark: avarkRoll);
                amgr.Tween(avarRollNonDrop);

                double sfMin = 1.9;
                double sfMax = 4.5 + rand.NextDouble(0, 1.0);
                double sf_dtms = (sfMax - sfMin)/17000;
                avarBounceSfDrop = AvarLinearDiscrete<TavarNone>.BounceBetween(
                    avar => mandelbox._mandelbox.sf,
                    (avar, sf) => mandelbox._mandelbox.sf = (float)sf,
                    sfMin,
                    sfMax,
                    3 * sf_dtms,
                    avark: avarkSf);
                avarBounceSfNonDrop = AvarLinearDiscrete<TavarNone>.BounceBetween(
                    avar => mandelbox._mandelbox.sf,
                    (avar, sf) => mandelbox._mandelbox.sf = (float)sf,
                    sfMin,
                    sfMax,
                    sf_dtms,
                    avark: avarkSf);
                amgr.Tween(avarBounceSfNonDrop);

                const float dxCamera_dtms = 2 / 3000f;
                amgr.Tween(new AvarIndefinite<TavarNone>(
                    (avar, dtms) => camera.MoveBy(new Vector3(-dxCamera_dtms * (float)dtms, 0, 0))));
            }

            private void SetSfTwist(double sfTwist)
            {
                var xFixed = camera.ptCamera.X;
                float xStart = (float)(xFixed - mandelbox._mandelbox.sfTwist * (xFixed - mandelbox._mandelbox.xTwistStart) / sfTwist);
                mandelbox._mandelbox.sfTwist = (float)sfTwist;
                mandelbox._mandelbox.xTwistStart = xStart; // + 0.17f;
            }

            readonly Avark avarkTwist = Avark.New();
            const float sfTwistMax = 10;
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

            protected override void OnKeyUp(KeyEventArgs keyEventArgs)
            {
                switch (keyEventArgs.KeyCode)
                {
                    case Keys.Q:
                        amgr.Tween(AvarTwistGentle(1.0 / 5, 20000));
                        break;
                    case Keys.N:
                        amgr.Tween(
                            AvarLinearDiscrete<TavarNone>.LerpPt(
                                mandelbox._mandelbox.ptTrap,
                                rand.VkUnitRand(-2, 2),
                                500,
                                pt => mandelbox._mandelbox.ptTrap = pt));
                        break;
                    case Keys.L:
                        pointLightCamera.brightness = 2.0f;
                        amgr.Tween(
                            new AvarLinearDiscrete<TavarNone>(
                                pointLightCamera.brightness,
                                brightnessCameraLight,
                                (avar, brightness) => pointLightCamera.brightness = (float)brightness,
                                500));
                        break;
                    case Keys.NumPad0:
                    case Keys.NumPad1:
                        amgr.Tween(
                            new AvarLinearDiscrete<TavarNone>(
                                pointLightCamera.brightness,
                                keyEventArgs.KeyCode == Keys.NumPad0 ? 0.5 : brightnessCameraLight,
                                (avar, brightness) => pointLightCamera.brightness = (float) brightness,
                                1000));
                        break;
                    case Keys.T:
                        double val = rand.NextDouble();
                        amgr.Tween(
                            new AvarLinearDiscrete<TavarNone>(
                                raytracer._raytracerfractal.sfR,
                                val < 1.0/3 ? 1.0 - raytracer._raytracerfractal.sfR : rand.NextDouble(),
                                (avar, sf) => raytracer._raytracerfractal.sfR = (float) sf,
                                200,
                                avark: avarkSfR));
                        amgr.Tween(
                            new AvarLinearDiscrete<TavarNone>(
                                raytracer._raytracerfractal.sfG,
                                val > 1.0/3 && val < 2.0/3 ? 1.0 - raytracer._raytracerfractal.sfG : rand.NextDouble(),
                                (avar, sf) => raytracer._raytracerfractal.sfG = (float) sf,
                                200,
                                avark: avarkSfG));
                        amgr.Tween(
                            new AvarLinearDiscrete<TavarNone>(
                                raytracer._raytracerfractal.sfB,
                                val > 2.0/3 ? 1.0 - raytracer._raytracerfractal.sfB : rand.NextDouble(),
                                (avar, sf) => raytracer._raytracerfractal.sfB = (float) sf,
                                200,
                                avark: avarkSfB));
                        break;
                    case Keys.R:
                        amgr.Tween(new AvarLinearDiscrete<TavarNone>(
                            duSfRollx,
                            -duSfRollx,
                            (avar, duduSfRollx) => duSfRollx = (float)duduSfRollx,
                            1500,
                            avark: avarkDuSfRollx));
                        break;
                }
                base.OnKeyUp(keyEventArgs);
            }

            const float sfSinMin = 0;
            const float sfSinMax = 1.0f;
            const float sfSin_dtms = sfSinMax / 2000;
            const float sfRollx_dtms = -(float)Math.PI / 2000;
            private bool fSwitch = false;
            public override void DoEvents(float dtms)
            {
                base.DoEvents(dtms);

                railOrbitCamera.UpdatePt(dtms);
                pointLightCamera.ptLight = camera.ptCamera + new Vector3(0.1f, 0, 0); //camera.ptCamera + ptRailLight + new Vector3(0.05f, 0, 0);
                camera.LookAt(camera.ptCamera + vkRailCamera + new Vector3(-0.1f, 0, 0));
                //pointLightCamera.ptLight = camera.ptCamera + new Vector3(-0.02f, 0, 0);
            }

            protected override void OnDropBegin()
            {
                amgr.Cancel(avarBounceSfNonDrop);
                amgr.Tween(avarBounceSfDrop);

                //amgr.Cancel(avarRollNonDrop);
                //amgr.Tween(avarRollDrop);

                amgr.Tween(new AvarLinearDiscrete<TavarNone>(mandelbox._mandelbox.sfSin, sfSinMax, sfSin_dtms,
                    (avar, sfSin) => mandelbox._mandelbox.sfSin = (float) sfSin,
                    avark: avarkSfSin));

                amgr.Tween(new AvarIndefinite<TavarNone>((avar, dtms) => mandelbox._mandelbox.sfRollx -= 2*duSfRollx*sfRollx_dtms*(float)dtms, avark: avarkSfRollx));
                base.OnDropBegin();
            }

            protected override void OnDropEnd()
            {
                amgr.Cancel(avarBounceSfDrop);
                amgr.Tween(avarBounceSfNonDrop);

                //amgr.Cancel(avarRollDrop);
                //amgr.Tween(avarRollNonDrop);

                amgr.Tween(new AvarLinearDiscrete<TavarNone>(mandelbox._mandelbox.sfSin, sfSinMin, sfSin_dtms,
                    (avar, sfSin) => mandelbox._mandelbox.sfSin = (float)sfSin,
                    avark: avarkSfSin));

                amgr.Tween(new AvarIndefinite<TavarNone>((avar, dtms) => mandelbox._mandelbox.sfRollx -= duSfRollx*sfRollx_dtms * (float)dtms, avark: avarkSfRollx));
                base.OnDropEnd();
            }
        }
    }
}
