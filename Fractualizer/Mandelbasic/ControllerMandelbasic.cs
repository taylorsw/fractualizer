﻿using System.Windows.Forms;
using EVTC;

namespace Mandelbasic
{
    class ControllerMandelbasic : Controller
    {
        protected override Stage StageCreate(Form form)
        {
            return new StageMandelboxExplorer(form, this, width, height);
        }
    }
}
