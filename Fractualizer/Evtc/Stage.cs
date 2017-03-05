using Fractals;

namespace EVTC
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
