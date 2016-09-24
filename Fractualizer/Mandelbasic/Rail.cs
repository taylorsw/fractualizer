using System.Diagnostics;
using Render;
using SharpDX;

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

        public void UpdatePt(Vector3 ptCur, float dtms) => dgUpdatePt(PtUpdated(ptCur, dtms));
        protected abstract Vector3 PtUpdated(Vector3 ptCur, float dtms);
    }

    public class RailOrbit : Rail
    {
        private readonly Vector3 ptCenter;
        private readonly Vector3 vkNormal;
        private readonly float agd_dtms;

        public RailOrbit(DgUpdatePt dgUpdatePt, Vector3 ptCenter, Vector3 vkNormal, float agd_dtms) : base(dgUpdatePt)
        {
            this.ptCenter = ptCenter;
            this.vkNormal = vkNormal;
            this.agd_dtms = agd_dtms;
        }

        protected override Vector3 PtUpdated(Vector3 ptCur, float dtms)
        {
            Vector3 vkFromCenter = ptCur - ptCenter;
            Matrix matRotate = Matrix.RotationAxis(vkNormal, MathUtil.DegreesToRadians(dtms * agd_dtms));
            Vector3 vkRotated = Vector3.Transform(vkFromCenter, matRotate).PerspectiveDivide();
            Debug.WriteLine(ptCenter + vkRotated);
            return ptCenter + vkRotated;
        }
    }
}