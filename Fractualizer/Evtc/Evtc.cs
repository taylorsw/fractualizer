using System.Windows.Forms;
using Evtc;
using Fractals;

namespace Mandelbasic
{
    public abstract class Evtc
    {
        protected readonly Form form;
        protected Controller controller;
        protected RaytracerFractal raytracer => controller.raytracer;
        protected Scene scene => raytracer.scene;
        protected Camera camera => raytracer.camera;

        protected Evtc(Form form, Controller controller)
        {
            this.form = form;
            this.controller = controller;
        }

        public abstract void DoEvents(float dtms);
    }
}
