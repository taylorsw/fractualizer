using System.Windows.Forms;
using EVTC;

namespace Scanner
{
    class ControllerScanner : Controller
    {
        protected override Stage StageCreate(Form form)
        {
            return new StageScanner(form, this, width, height);
        }
    }
}
