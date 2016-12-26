using System;
using System.Collections.Generic;
using System.Diagnostics;
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
}
