using System.Windows.Forms;
using Evtc;
using Fractals;

namespace Mandelbasic
{
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

//            double sfTwistMin = -3;
//            double sfTwistMax = 3;
//            double dtwist_dtms = (sfTwistMax - sfTwistMin) / 7000;
//            var mandelbox = (Mandelbox) raytracer.scene.fractal;
//            var camera = raytracer.camera;
//            //mandelbox._mandelbox.sfTwist = 3;
//            var avarTwist = AvarLinearDiscrete<TavarNone>.BounceBetween(
//                _ => mandelbox._mandelbox.sfTwist,
//                (_, sf) =>
//                {
//                    var xFixed = -10;
//                    float xStart = (float)(xFixed - mandelbox._mandelbox.sfTwist * (xFixed - mandelbox._mandelbox.xTwistStart) / sf);
//                    mandelbox._mandelbox.sfTwist = (float)sf;
//                    mandelbox._mandelbox.xTwistStart = xStart;
//                },
//                valMin: sfTwistMin,
//                valMax: sfTwistMax,
//                dval_dtms: dtwist_dtms);
//
//            evtc.amgr.Tween(avarTwist);
            base.Setup();
        }
    }
}
