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
            const float sfSin_dtms = sfSinMax / 2000;
            const float sfRollx_dtms = -(float)Math.PI / 2000;
            private bool fSwitch = false;
            public override void DoEvents(float dtms)
            {
                base.DoEvents(dtms);

                const float dxCamera_dtms = 2 / 3000f;
                camera.MoveTo(camera.ptCamera - new Vector3(dxCamera_dtms * dtms, 0, 0));

                railOrbit.UpdatePt(dtms);
                pointLightCamera.ptLight = camera.ptCamera + ptRailLight;
                //pointLightCamera.ptLight = camera.ptCamera + new Vector3(-0.02f, 0, 0);

                railOrbitTrap?.UpdatePt(dtms);
                mandelbox._mandelbox.ptTrap = ptOrbitTrap;

                const float duXroll_dtms = -1 / 3000f;
                const float dagdRoll_dtms = 36f / 2000;
                if (fDrop)
                {
                    railSf.UpdateValue(dtms);
                    mandelbox._mandelbox.sf = railSf.val;
                    camera.RollBy(2 * dagdRoll_dtms * dtms);
                    mandelbox._mandelbox.sfRollx -= 2 * duXroll_dtms * dtms;

                    if (mandelbox._mandelbox.sfSin < sfSinMax)
                        mandelbox._mandelbox.sfSin += sfSin_dtms * dtms;
                    mandelbox._mandelbox.sfRollx += sfRollx_dtms * dtms;
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
}
