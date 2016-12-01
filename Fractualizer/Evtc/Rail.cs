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

        public void UpdatePt(float dtms)
        {
            Vector3 ptUpdated = PtUpdated(dtms);
            Debug.Assert(ptUpdated.IsFinite());
            dgUpdatePt(ptUpdated);
        }

        protected abstract Vector3 PtUpdated(float dtms);
    }

    public class RailOrbit : Rail
    {
        protected readonly Vector3 ptCenter, ptInitial;
        protected readonly Vector3 vkNormal;
        protected readonly float dtmsRevolution;
        protected float dtmsCur { get; private set; }

        public RailOrbit(DgUpdatePt dgUpdatePt, Vector3 ptCenter, Vector3 ptInitial, Vector3 vkNormal, float dtmsRevolution) : base(dgUpdatePt)
        {
            this.ptCenter = ptCenter;
            this.ptInitial = ptInitial;
            this.vkNormal = vkNormal.Normalized();
            this.dtmsRevolution = dtmsRevolution;
            this.dtmsCur = 0;
        }

        protected override Vector3 PtUpdated(float dtms)
        {
            dtmsCur = (dtmsCur + dtms)%dtmsRevolution;
            return PtFromDtms(dtmsCur);
        }

        protected Vector3 PtFromDtms(float dtms)
        {
            Debug.Assert(dtms >= 0 && dtms < dtmsRevolution);
            Vector3 vkFromCenter = ptInitial - ptCenter;
            Matrix matRotate = Matrix.RotationAxis(vkNormal, MathUtil.DegreesToRadians(360 * dtms / dtmsRevolution));
            Vector3 vkRotated = Vector3.Transform(vkFromCenter, matRotate).PerspectiveDivide();
            return ptCenter + vkRotated;
        }
    }

    public class RailHover : RailOrbit
    {
        private readonly Fractal3d fractal;
        public float duHoverMin;
        public float duHoverMax;
        private readonly float sfTravelMax;

        public RailHover(
            DgUpdatePt dgUpdatePt, 
            Fractal3d fractal, 
            Vector3 ptCenter,
            Vector3 ptInitial,
            Vector3 vkNormal,
            float dtmsRevolution, // angular speed at which the 
            float duHoverMin, // the minimum distance the point will hover above the fractal - guaranteed
            float duHoverMax, // the maximum distance the point will hover above the fractal - best attempt
            float sfTravelMax // determines the maximum speed the hover will climb/drop relative to its orbiting speed
            ) : base(dgUpdatePt, ptCenter, ptInitial, vkNormal, dtmsRevolution)
        {
            this.fractal = fractal;
            this.duHoverMin = duHoverMin;
            this.duHoverMax = duHoverMax;
            Debug.Assert(sfTravelMax >= 1.0);
            this.sfTravelMax = sfTravelMax;
        }

        protected override Vector3 PtUpdated(float dtms)
        {
            // Get orbit value and DE
            Vector3 ptCur = PtFromDtms(dtmsCur);
            Vector3 ptRotated = base.PtUpdated(dtms);
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