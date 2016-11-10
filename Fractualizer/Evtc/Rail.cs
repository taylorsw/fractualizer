using System;
using System.Diagnostics;
using Fractals;
using SharpDX;
using Util;

namespace Mandelbasic
{
    public abstract class Rail
    {
        public delegate void DgUpdatePt(Vector3 pt);

        private readonly DgUpdatePt dgUpdatePt;

        protected Rail(DgUpdatePt dgUpdatePt)
        {
            this.dgUpdatePt = dgUpdatePt;
        }

        public void UpdatePt(Vector3 ptCur, float dtms)
        {
            Vector3 ptUpdated = PtUpdated(ptCur, dtms);
            Debug.Assert(ptUpdated.IsFinite());
            dgUpdatePt(ptUpdated);
        }

        protected abstract Vector3 PtUpdated(Vector3 ptCur, float dtms);
    }

    public class RailOrbit : Rail
    {
        protected readonly Vector3 ptCenter;
        protected readonly Vector3 vkNormal;
        protected readonly float agd_dtms;

        public RailOrbit(DgUpdatePt dgUpdatePt, Vector3 ptCenter, Vector3 vkNormal, float agd_dtms) : base(dgUpdatePt)
        {
            this.ptCenter = ptCenter;
            this.vkNormal = vkNormal.Normalized();
            this.agd_dtms = agd_dtms;
        }

        protected override Vector3 PtUpdated(Vector3 ptCur, float dtms)
        {
            Vector3 vkFromCenter = ptCur - ptCenter;
            Matrix matRotate = Matrix.RotationAxis(vkNormal, MathUtil.DegreesToRadians(dtms * agd_dtms));
            Vector3 vkRotated = Vector3.Transform(vkFromCenter, matRotate).PerspectiveDivide();
            return ptCenter + vkRotated;
        }
    }

    public class RailHover : RailOrbit
    {
        private readonly Fractal3d fractal;
        private readonly float duHoverMin;
        private readonly float duHoverMax;
        private readonly float sfTravelMax;

        public RailHover(
            DgUpdatePt dgUpdatePt, 
            Fractal3d fractal, 
            Vector3 ptCenter, 
            Vector3 vkNormal, 
            float agd_dtms, // angular speed at which the 
            float duHoverMin, // the minimum distance the point will hover above the fractal - guaranteed
            float duHoverMax, // the maximum distance the point will hover above the fractal - best attempt
            float sfTravelMax // determines the maximum speed the hover will climb/drop relative to its orbiting speed
            ) : base(dgUpdatePt, ptCenter, vkNormal, agd_dtms)
        {
            this.fractal = fractal;
            this.duHoverMin = duHoverMin;
            this.duHoverMax = duHoverMax;
            Debug.Assert(sfTravelMax >= 1.0);
            this.sfTravelMax = sfTravelMax;
        }

        protected override Vector3 PtUpdated(Vector3 ptCur, float dtms)
        {
            // Get orbit value and DE
            Vector3 ptRotated = base.PtUpdated(ptCur, dtms);
            double duDE = fractal.DuDeFractalOrCache(ptRotated);

            // Calculate distance we will travel along that arc
            float duOrbitTravel = sfTravelMax * (ptCur - ptRotated).Length();

            // Calculate the maximum amount we should adjust the location towards the center of the fractal
            Vector3 vkFromCenter = ptRotated - ptCenter;
            float duAdjustMax = duHoverMax - (float) duDE;
            int sign = Math.Sign(duAdjustMax);
            float duAdjustMaxAbs = Math.Abs(duAdjustMax);

            // Computes the final hover adjustment. 
            // Can be less than the amount needed to bring the point to within duHoverMax
            // However, it will always ensure that the point is not closer than duHoverMin
            float duAdjustFinal =
                sign * 
                    (duAdjustMaxAbs > duOrbitTravel 
                        ?  sign < 0 
                            ? duOrbitTravel 
                            : duOrbitTravel < duHoverMin ? duHoverMin : duOrbitTravel
                        : duAdjustMaxAbs);

            ptRotated += vkFromCenter.Normalized()*duAdjustFinal;
            return ptRotated;
        }
    }
}