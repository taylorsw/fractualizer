using System.Windows.Forms;
using EVTC;

namespace Mandelbasic
{
    class ControllerMandelbasic : Controller
    {
        protected override Stage StageCreate(Form form)
        {
            return new StageMandelboxInnerFirefly(form, this, width, height);
        }
    }
}
