using System;
using System.Windows.Forms;
using Fractals;

namespace EVTC
{
    public abstract class Evtc
    {
        protected readonly Form form;
        protected Controller controller;
        protected RaytracerFractal raytracer => controller.raytracer;
        protected Scene scene => raytracer.scene;
        protected Fractal3d fractal => scene.fractal;
        protected Camera camera => raytracer.camera;
        public readonly Amgr amgr;
        protected RaytracerFractal.LightManager lightManager => raytracer.lightManager;
        protected Random rand => scene.rand;

        protected Evtc(Form form, Controller controller)
        {
            this.form = form;
            this.controller = controller;
            this.amgr = new Amgr();
        }

        public virtual void Setup() { }

        public void HandleTime(float dtms)
        {
            DoEvents(dtms);
            amgr.Update(dtms);
        }
        public abstract void DoEvents(float dtms);
    }
}
