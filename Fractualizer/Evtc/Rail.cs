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

        protected float dtmsCur;
        protected readonly float dtmsRevolution;

        protected Rail(float dtmsRevolution)
        {
            this.dtmsCur = 0;
            this.dtmsRevolution = dtmsRevolution;
        }

        public virtual void UpdateDtms(float dtms)
        {
            dtmsCur = (dtmsCur + dtms)%dtmsRevolution;
        }
    }

    public abstract class RailPt : Rail
    {
        private readonly DgUpdatePt dgUpdatePt;

        protected RailPt(DgUpdatePt dgUpdatePt, float dtmsRevolution) : base(dtmsRevolution)
        {
            this.dgUpdatePt = dgUpdatePt;
        }
        
        public abstract Vector3 PtCur();
        public void UpdatePt(float dtms)
        {
            UpdateDtms(dtms);
            Vector3 ptUpdated = PtCur();
            Debug.Assert(ptUpdated.IsFinite());
            dgUpdatePt(ptUpdated);
        }
    }

    public class RailOrbit : RailPt
    {
        protected readonly Vector3 ptCenter, ptInitial, vkNormal;

        public RailOrbit(DgUpdatePt dgUpdatePt, Vector3 ptCenter, Vector3 ptInitial, Vector3 vkNormal, float dtmsRevolution) : base(dgUpdatePt, dtmsRevolution)
        {
            this.ptCenter = ptCenter;
            this.ptInitial = ptInitial;
            this.vkNormal = vkNormal.Normalized();
        }

        public override Vector3 PtCur()
        {
            return PtFromDtms(dtmsCur);
        }

        protected Vector3 PtFromDtms(float dtms)
        {
            return PtGet(ptInitial, ptCenter, vkNormal, dtms, dtmsRevolution);
        }

        public static Vector3 PtGet(Vector3 ptInitial, Vector3 ptCenter, Vector3 vkNormal, float dtms, float dtmsRevolution)
        {
            Debug.Assert(dtms >= 0 && dtms < dtmsRevolution);
            Vector3 vkFromCenter = ptInitial - ptCenter;
            Matrix matRotate = Matrix.RotationAxis(vkNormal, MathUtil.DegreesToRadians(360 * dtms / dtmsRevolution));
            Vector3 vkRotated = Vector3.Transform(vkFromCenter, matRotate).PerspectiveDivide();
            return ptCenter + vkRotated;
        }
    }

    public class RailSpotlight : Rail
    {
        private readonly DgUpdatePt dgUpdateVkSpotlight;
        private float agdRadius;
        private Vector3 vkNormal;
        public RailSpotlight(DgUpdatePt dgUpdateVkSpotlight, float agdRadius, Vector3 vkNormal, float dtmsRevolution) : base(dtmsRevolution)
        {
            this.dgUpdateVkSpotlight = dgUpdateVkSpotlight;
            this.agdRadius = agdRadius;
            this.vkNormal = vkNormal;
        }

        public void SetVkNormal(Vector3 vkNormal)
        {
            Debug.Assert(vkNormal.IsNormalized);
            this.vkNormal = vkNormal;
        }

        public void SetAgdRadius(float agdRadius)
        {
            Debug.Assert(agdRadius >= 0 && agdRadius < 360);
            this.agdRadius = agdRadius;
        }

        public void UpdateVkSpotlight(float dtms)
        {
            UpdateDtms(dtms);
            Vector3 ptCenter = Vector3.Zero;
            Vector3 ptInitial = new Vector3(0, (float)Math.Tan(MathUtil.DegreesToRadians(agdRadius / 2)), 0);
            Vector3 vkSpotlight = (RailOrbit.PtGet(ptInitial, ptCenter, vkNormal, dtmsCur, dtmsRevolution) + vkNormal).Normalized();
            dgUpdateVkSpotlight(vkSpotlight);
        }
    }

    public class RailHover : RailOrbit
    {
        private readonly Fractal3d fractal;
        private readonly float sfTravelMax, duHoverMin, duHoverMax;
        private float dtmsPrev, duAdjustPrev;

        public RailHover(
            DgUpdatePt dgUpdatePt, 
            Fractal3d fractal,
            Vector3 ptCenter,
            Vector3 ptInitial,
            Vector3 vkNormal,
            float dtmsRevolution,
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
            this.dtmsPrev = duAdjustPrev = 0;
        }

        public override void UpdateDtms(float dtms)
        {
            dtmsPrev = dtmsCur;
            base.UpdateDtms(dtms);
        }

        private Vector3 AdjustedTowardsCenter(Vector3 pt, float duAdjust) => pt + (pt - ptCenter).Normalized()*duAdjust;

        public override Vector3 PtCur()
        {
            Vector3 ptPrev = AdjustedTowardsCenter(PtFromDtms(dtmsPrev), duAdjustPrev);
            Vector3 ptCur = AdjustedTowardsCenter(PtFromDtms(dtmsCur), duAdjustPrev);
            double duDE = fractal.DuDeFractalOrCache(ptCur);

            // Calculate distance we will travel along that arc
            float duOrbitTravel = sfTravelMax * (ptPrev - ptCur).Length();

            // Calculate the maximum amount we should adjust the location towards the center of the fractal
            float duAdjustMax = duHoverMax - (float)duDE;
            int sign = Math.Sign(duAdjustMax);
            float duAdjustMaxAbs = Math.Abs(duAdjustMax);

            // Computes the final hover adjustment. 
            // Can be less than the amount needed to bring the point to within duHoverMax
            // However, it will always ensure that the point is not closer than duHoverMin
            float duAdjustFinal =
                sign *
                    (duAdjustMaxAbs > duOrbitTravel
                        ? sign < 0
                            ? duOrbitTravel
                            : duOrbitTravel < duHoverMin ? duHoverMin : duOrbitTravel
                        : duAdjustMaxAbs);

            ptCur = AdjustedTowardsCenter(ptCur, duAdjustFinal);
            duAdjustPrev += duAdjustFinal;
            return ptCur;
        }
    }

    public class RailLinear : RailPt
    {
        private readonly Vector3 ptStart, ptEnd;
        private readonly float dtms;
        public RailLinear(Vector3 ptStart, Vector3 ptEnd, float dtms, DgUpdatePt dgUpdatePt) : base(dgUpdatePt, float.PositiveInfinity)
        {
            this.ptStart = ptStart;
            this.ptEnd = ptEnd;
            this.dtms = dtms;
        }

        public override Vector3 PtCur()
        {
            float tms = Math.Min(dtmsCur, dtms);
            float fr = tms/dtms;
            return (1 - fr)*ptStart + fr*ptEnd;
        }
    }
}