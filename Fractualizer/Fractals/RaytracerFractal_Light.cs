using System;
using System.Collections.Generic;
using SharpDX;

namespace Fractals
{
    public enum Klight
    {
        Point,
        Ball
    }

    public abstract class Light
    {
        public Klight klight;
        public Vector3 ptLight;

        protected Light(Klight klight, Vector3 ptLight)
        {
            this.klight = klight;
            this.ptLight = ptLight;
        }
        
        internal int LidGet()
        {
            return (int) klight;
        }

        internal virtual void SyncWithBuffer(RaytracerFractal._RaytracerFractal _raytracerfractal, int ilight)
        {
            _raytracerfractal.rglidLight[ilight] = LidGet();
            _raytracerfractal.rgptLight[ilight] = ptLight;
        }
    }

    public class PointLight : Light
    {
        public PointLight(Vector3 ptLight) : base(Klight.Point, ptLight) { }
    }

    public class BallLight : Light
    {
        public float duCutoff;

        public BallLight(Vector3 ptLight, float duCutoff) : base(Klight.Ball, ptLight)
        {
            this.duCutoff = duCutoff;
        }

        internal override void SyncWithBuffer(RaytracerFractal._RaytracerFractal _raytracerfractal, int ilight)
        {
            _raytracerfractal.rgduCutoffLight[ilight] = duCutoff;
            base.SyncWithBuffer(_raytracerfractal, ilight);
        }
    }

    partial class RaytracerFractal
    {
        public class LightManager
        {
            private readonly RaytracerFractal raytracer;
            private readonly List<Light> rglight;
            public int clight => rglight.Count;

            public int clightMax => raytracer._raytracerfractal.rgptLight.cvalArray;

            public LightManager(RaytracerFractal raytracer)
            {
                this.raytracer = raytracer;
                rglight = new List<Light>(clightMax);
            }

            private int IlightEnsure(int ilight)
            {
                if (ilight < 0 || ilight > clight - 1)
                    throw new IndexOutOfRangeException();
                return ilight;
            }

            public Light this[int ilight]
            {
                get { return rglight[IlightEnsure(ilight)]; }
                set { rglight[IlightEnsure(ilight)] = value; }
            }

            public void AddLight(Light light)
            {
                if (clight == clightMax)
                    throw new ArgumentException();
                rglight.Add(light);
            }

            public void RemoveLight(int ilight)
            {
                rglight.RemoveAt(IlightEnsure(ilight));
            }

            public void RemoveAllLights()
            {
                rglight.Clear();
            }

            public IEnumerable<Light> En_light()
            {
                foreach (Light light in rglight)
                    yield return light;
            }

            internal void SyncWithBuffer()
            {
                for (int ilight = 0; ilight < clight; ilight++)
                {
                    Light light = rglight[ilight];
                    light.SyncWithBuffer(raytracer._raytracerfractal, ilight);
                }
                raytracer._raytracerfractal.cLight = clight;
            }
        }

        public readonly LightManager lightManager;
    }
}
