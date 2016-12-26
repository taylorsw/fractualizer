using System;
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
            raytracer = new RaytracerFractal(new Scene(new Mandelbox(), ((EvtcAudio)evtc).StSong().GetHashCode()),
                width, height);
        }

        private class EvtcWave : EvtcAudio
        {
            private Mandelbox mandelbox => scene.fractal as Mandelbox;

            private PointLight pointLightCamera;
            private Vector3 ptRailLight;
            private RailOrbit railOrbit;

            // Non-drop
            private AvarIndefinite<TavarNone> avarRollNonDrop;
            private AvarLinearDiscrete<TavarNone> avarBounceSfNonDrop;

            // Drop
            private AvarIndefinite<TavarNone> avarRollDrop;
            private AvarLinearDiscrete<TavarNone> avarBounceSfDrop;

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
                camera.MoveTo(new Vector3(0, mandelbox._mandelbox.duMirrorPlane, 0));
                camera.LookAt(camera.ptCamera - new Vector3(1, 0, 0));
                pointLightCamera = new PointLight(camera.ptCamera, Vector3.One, 1.2f, false);
                railOrbit = new RailOrbit(pt => ptRailLight = pt, Vector3.Zero, new Vector3(0, 0.2f, 0), new Vector3(1, 0, 0), 14.77f * 1000);
                lightManager.AddLight(pointLightCamera);

                // Avars
                const float dagdRoll_dtms = 36f / 2000;
                avarRollNonDrop = new AvarIndefinite<TavarNone>(
                    (avar, dtms) => camera.RollBy(dagdRoll_dtms * (float)dtms));
                avarRollDrop = new AvarIndefinite<TavarNone>(
                    (avar, dtms) => camera.RollBy(2*dagdRoll_dtms*(float) dtms));
                amgr.Tween(avarRollNonDrop);

                double sfMin = 1.9;
                double sfMax = 4.5;
                double sf_dtms = (sfMax - sfMin)/17000;
                avarBounceSfDrop = AvarLinearDiscrete<TavarNone>.BounceBetween(
                    avar => mandelbox._mandelbox.sf,
                    (avar, sf) => mandelbox._mandelbox.sf = (float)sf,
                    1.9,
                    4.5,
                    3 * sf_dtms);
                avarBounceSfNonDrop = AvarLinearDiscrete<TavarNone>.BounceBetween(
                    avar => mandelbox._mandelbox.sf,
                    (avar, sf) => mandelbox._mandelbox.sf = (float)sf,
                    1.9,
                    4.5,
                    sf_dtms);
                amgr.Tween(avarBounceSfNonDrop);

                const float dxCamera_dtms = 2 / 3000f;
                amgr.Tween(new AvarIndefinite<TavarNone>(
                    (avar, dtms) => camera.MoveBy(new Vector3(-dxCamera_dtms * (float)dtms, 0, 0))));
            }

            protected override void OnKeyUp(KeyEventArgs keyEventArgs)
            {
                if (keyEventArgs.KeyCode == Keys.N)
                {
                    amgr.Tween(
                        AvarLinearDiscrete<TavarNone>.LerpPt(
                            mandelbox._mandelbox.ptTrap, 
                            rand.VkUnitRand(-2, 2), 
                            500, 
                            pt => mandelbox._mandelbox.ptTrap = pt));
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

                const float duXroll_dtms = -1 / 3000f;
                if (fDrop)
                {

                    if (mandelbox._mandelbox.sfSin < sfSinMax)
                        mandelbox._mandelbox.sfSin += sfSin_dtms * dtms;
                    mandelbox._mandelbox.sfRollx -= 2* sfRollx_dtms * dtms;
                }
                else
                {
                    mandelbox._mandelbox.sfRollx -= duXroll_dtms * dtms;

                    if (mandelbox._mandelbox.sfSin > sfSinMin)
                        mandelbox._mandelbox.sfSin -= sfSin_dtms * dtms;
                }
            }

            protected override void OnDropBegin()
            {
                amgr.Cancel(avarBounceSfNonDrop);
                amgr.Tween(avarBounceSfDrop);

                amgr.Cancel(avarRollNonDrop);
                amgr.Tween(avarRollDrop);

                amgr.Tween(new AvarLinearDiscrete<TavarNone>(mandelbox._mandelbox.sfSin, sfSinMax, sfSin_dtms,
                    (avar, sfSin) => mandelbox._mandelbox.sfSin = (float) sfSin));
                base.OnDropBegin();
            }

            protected override void OnDropEnd()
            {
                amgr.Cancel(avarBounceSfDrop);
                amgr.Tween(avarBounceSfNonDrop);

                amgr.Cancel(avarRollDrop);
                amgr.Tween(avarRollNonDrop);

                amgr.Tween(new AvarLinearDiscrete<TavarNone>(mandelbox._mandelbox.sfSin, sfSinMin, sfSin_dtms,
                    (avar, sfSin) => mandelbox._mandelbox.sfSin = (float)sfSin));
                base.OnDropEnd();
            }
        }
    }
}
