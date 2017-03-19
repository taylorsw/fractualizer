using System;
using System.Collections.Generic;
using SharpDX;

namespace Fractals
{
    public enum Klight
    {
        Point,
        Ball,
        Spotlight
    }

    public abstract class Light
    {
        public Klight klight;
        public Vector3 ptLight;
        public Vector3 rgbLight;
        public float brightness;
        public bool fVisualize;

        protected Light(Klight klight, Vector3 ptLight, Vector3 rgbLight, float brightness, bool fVisualize)
        {
            this.klight = klight;
            this.ptLight = ptLight;
            this.rgbLight = rgbLight;
            this.brightness = brightness;
            this.fVisualize = fVisualize;
        }

        internal int LidGet()
        {
            return (int) klight;
        }

        internal virtual void SyncWithBuffer(RaytracerFractal._RaytracerFractal _raytracerfractal, int ilight)
        {
            _raytracerfractal.rglidLight[ilight] = LidGet();
            _raytracerfractal.rgptLight[ilight] = ptLight;
            _raytracerfractal.rgrgbLight[ilight] = rgbLight;
            _raytracerfractal.rgbrightnessLight[ilight] = brightness;
            _raytracerfractal.rgfVisualizeLight[ilight] = fVisualize;
        }
    }

    public class PointLight : Light
    {
        public PointLight(Vector3 ptLight, Vector3 rgbLight, float brightness = 1.0f, bool fVisualize = true) : base(Klight.Point, ptLight, rgbLight, brightness, fVisualize)
        {
        }
    }

    public class BallLight : Light
    {
        public float duCutoff;
        public float duCutoffVisual;

        public BallLight(Vector3 ptLight, Vector3 rgbLight, float duCutoff, float duCutoffVisual, float brightness = 1.0f, bool fVisualize = true) : base(Klight.Ball, ptLight, rgbLight, brightness, fVisualize)
        {
            this.duCutoff = duCutoff;
            this.duCutoffVisual = duCutoffVisual;
        }

        internal override void SyncWithBuffer(RaytracerFractal._RaytracerFractal _raytracerfractal, int ilight)
        {
            _raytracerfractal.rgduCutoffBallLight[ilight] = duCutoff;
            _raytracerfractal.rgduCutoffBallVisual[ilight] = duCutoffVisual;
            base.SyncWithBuffer(_raytracerfractal, ilight);
        }
    }

    public class SpotLight : Light
    {
        public Vector3 vkLight;
        public float agdRadius;

        public SpotLight(Vector3 ptLight, Vector3 rgbLight, Vector3 vkLight, float agdRadius, float brightness = 1.0f, bool fVisualize = false) : base(Klight.Spotlight, ptLight, rgbLight, brightness, fVisualize)
        {
            this.vkLight = vkLight;
            this.agdRadius = agdRadius;
        }

        internal override void SyncWithBuffer(RaytracerFractal._RaytracerFractal _raytracerfractal, int ilight)
        {
            _raytracerfractal.rgcosCutoffLight[ilight] = (float)Math.Cos(MathUtil.DegreesToRadians(agdRadius));
            _raytracerfractal.rgvkLight[ilight] = vkLight;
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
