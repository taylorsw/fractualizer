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
            raytracer = new RaytracerFractal(new Scene(new Mandelbulb()), width, height);
            evtc = new EvtcAudio(form, controller);
        }

        public override void Setup()
        {
            base.Setup();
            raytracer._raytracerfractal.colorB = new Vector3(235, 227, 172).Normalized();
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

        private class EvtcAcidHighway : Evtc
        {
            private readonly AudioProcessor processor;
            private PointLight pointLightCamera;
            public EvtcAcidHighway(Form form, Controller controller) : base(form, controller)
            {
                processor = new AudioProcessor();
            }

            public override void Setup()
            {
                base.Setup();
                processor.StartProcessor("Resources/dontletmedown.mp3");
                pointLightCamera = new PointLight(camera.ptCamera, Vector3.One, brightness: 0.3f, fVisualize: false);
                lightManager.AddLight(pointLightCamera);
                camera.MoveTo(new Vector3(0.1548655f, -0.479544f, -0.5555527f));
            }

            public override void DoEvents(float dtms)
            {
                const float du_dtms = 1.0f/10000;
                camera.MoveBy(new Vector3(-du_dtms * dtms, 0, 0));
                camera.LookAt(camera.ptCamera - new Vector3(1, 0, 0));

                const float dagd_dtms = 360/20000f;
                camera.RollBy(dagd_dtms * dtms);

                pointLightCamera.ptLight = camera.ptCamera;
            }
        }
    }
}
