using System.Windows.Forms;
using Fractals;

namespace Mandelbasic
{
    public abstract class Evtc
    {
        protected readonly Form form;
        protected readonly Raytracer raytracer;
        protected Scene scene => raytracer.scene;
        protected Camera camera => raytracer.camera;

        protected Evtc(Form form, Raytracer raytracer)
        {
            this.form = form;
            this.raytracer = raytracer;
        }

        public abstract void DoEvents(float dtms);
    }
}
