using System.Windows.Forms;
using Render;

namespace Mandelbasic
{
    public abstract class Evtc
    {
        protected readonly Form form;
        protected readonly Scene scene;

        protected Evtc(Form form, Scene scene)
        {
            this.form = form;
            this.scene = scene;
        }

        public abstract void DoEvents();
    }
}
