using System;
using System.Diagnostics;
using System.Windows.Forms;
using Evtc;
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
            private Vector3 ptRailLight;
            private RailOrbit railOrbit;

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
                mandelbox._mandelbox.fGradientColor = 1;
                mandelbox._mandelbox.fAdjustAdditional = true;
                mandelbox._mandelbox.sfRollx = 0;
                mandelbox._mandelbox.duMirrorPlane = 1.0f;
                mandelbox._mandelbox.sfSin = 0.0f;
                camera.MoveTo(new Vector3(0, mandelbox._mandelbox.duMirrorPlane, 0));
                camera.LookAt(camera.ptCamera - new Vector3(1, 0, 0));
                pointLightCamera = new PointLight(camera.ptCamera, Vector3.One, brightnessCameraLight, false);
                railOrbit = new RailOrbit(pt => ptRailLight = pt, Vector3.Zero, new Vector3(0, 0.2f, 0), new Vector3(1, 0, 0), 14.77f * 1000);
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

            protected override void OnKeyUp(KeyEventArgs keyEventArgs)
            {
                switch (keyEventArgs.KeyCode)
                {
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
                        amgr.Tween(
                            new AvarLinearDiscrete<TavarNone>(
                                raytracer._raytracerfractal.sfR,
                                rand.NextDouble(),
                                (avar, sf) => raytracer._raytracerfractal.sfR = (float)sf,
                                200,
                                avark: avarkSfR));
                        amgr.Tween(
                            new AvarLinearDiscrete<TavarNone>(
                                raytracer._raytracerfractal.sfG,
                                rand.NextDouble(),
                                (avar, sf) => raytracer._raytracerfractal.sfG = (float) sf,
                                200,
                                avark: avarkSfG));
                        amgr.Tween(
                            new AvarLinearDiscrete<TavarNone>(
                                raytracer._raytracerfractal.sfB,
                                rand.NextDouble(),
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

                railOrbit.UpdatePt(dtms);
                pointLightCamera.ptLight = camera.ptCamera + ptRailLight;
                //pointLightCamera.ptLight = camera.ptCamera + new Vector3(-0.02f, 0, 0);
            }

            protected override void OnDropBegin()
            {
                amgr.Cancel(avarBounceSfNonDrop);
                amgr.Tween(avarBounceSfDrop);

                amgr.Cancel(avarRollNonDrop);
                amgr.Tween(avarRollDrop);

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

                amgr.Cancel(avarRollDrop);
                amgr.Tween(avarRollNonDrop);

                amgr.Tween(new AvarLinearDiscrete<TavarNone>(mandelbox._mandelbox.sfSin, sfSinMin, sfSin_dtms,
                    (avar, sfSin) => mandelbox._mandelbox.sfSin = (float)sfSin,
                    avark: avarkSfSin));

                amgr.Tween(new AvarIndefinite<TavarNone>((avar, dtms) => mandelbox._mandelbox.sfRollx -= duSfRollx*sfRollx_dtms * (float)dtms, avark: avarkSfRollx));
                base.OnDropEnd();
            }
        }
    }
}
