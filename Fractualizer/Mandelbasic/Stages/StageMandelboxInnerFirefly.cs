using System.Diagnostics;
using System.Windows.Forms;
using EVTC;
using Fractals;
using SharpDX;
using Util;

namespace Mandelbasic
{
    public class StageMandelboxInnerFirefly : Stage
    {
        public override RaytracerFractal raytracer { get; }
        public override Evtc evtc { get; }

        public StageMandelboxInnerFirefly(Form form, Controller controller, int width, int height)
        {
            evtc = new EvtcInnerFirefly(form, controller);
            raytracer = new RaytracerFractal(new Scene(new Mandelbox(), seed: 1992), width, height);
        }

        private class EvtcInnerFirefly : EvtcAudio
        {
            private Mandelbox mandelbox => (Mandelbox)scene.fractal;

            public EvtcInnerFirefly(Form form, Controller controller) : base(form, controller) { }

            public override string StSong() => "clocks.mp3";

            public override void Setup()
            {
                base.Setup();

                raytracer._raytracerfractal.sfEpsilonShadow = 4.0f;
                raytracer._raytracerfractal.sfAmbient = 0.1f;
                raytracer._raytracerfractal.fSkysphere = false;
                raytracer._raytracerfractal.cmarch = 140;
                mandelbox._mandelbox.fGradientColor = false;
                mandelbox._mandelbox.fAdjustAdditional = false;

                camera.MoveTo(new Vector3(-0.5513734f, 0.1202023f, -0.4507594f));
                Vector3 ptTrackOtherSide = new Vector3(-0.5513734f, 0.1328315f, -0.4507594f);
                camera.LookAt(camera.ptCamera - new Vector3(0, -1, 0));
                camera.RollBy(270);

                float dx_dtmsCamera = Vector3.Distance(ptTrackOtherSide, camera.ptCamera);
                amgr.Tween(
                    new AvarIndefinite<TavarNone>(
                        (_, dtms) => camera.MoveBy(new Vector3(0, dx_dtmsCamera * (float)dtms / 20000, 0))));
            }

            private Vector3 VkRandWithinView()
            {
                Vector3d vkRand =
                    (camera.PtViewPlaneFromPixel(
                        new Point(
                            rand.Next(0, (int) raytracer._raytracerfractal.rsScreen.x),
                            rand.Next(0, (int) raytracer._raytracerfractal.rsScreen.y)))
                    - camera.ptCamera).Normalized();
                return new Vector3((float)vkRand.x, (float)vkRand.y, (float)vkRand.z);
            }

            protected override void OnKeyUp(KeyEventArgs keyEventArgs)
            {
                switch (keyEventArgs.KeyCode)
                {
                    case Keys.Space:
                        float du_dtmsLight = 0.01f/2000f;
                        float duCutoffBallVisual = 0.0001f;
                        BallLight ballLight = new BallLight(camera.ptCamera, rand.VkUnitRand(min: 0.0f), duCutoff: duCutoffBallVisual, duCutoffVisual: duCutoffBallVisual * 10);
                        amgr.Tween(
                            new AvarIndefinite<Vector3>(
                                tval: VkRandWithinView(), 
                                dgWriteVal: (vkTravel, dtms) => ballLight.ptLight += vkTravel.tval * (float)dtms * du_dtmsLight));
                        lightManager.AddLight(ballLight);
                        break;
                }
                base.OnKeyUp(keyEventArgs);
            }

            public override void DoEvents(float dtms)
            {
                base.DoEvents(dtms);
            }

            protected override void OnDropBegin()
            {
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
