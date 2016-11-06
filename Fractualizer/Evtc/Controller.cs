using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fractals;

namespace Evtc
{
    public abstract class Controller : IDisposable
    {
        public abstract RaytracerFractal raytracer { get; }
        public abstract void Resize(int width, int height);
        public abstract void Dispose();
    }
}
