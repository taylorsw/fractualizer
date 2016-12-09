using System;
using System.Collections.Generic;
using System.Diagnostics;
using Util;

namespace Fractals
{
    public abstract class Fractal3d : FPLGenBase
    {
        #region Distance Estimation
        public abstract double DuDeFractal(Vector3d pt);
        #endregion

        #region Inputs
        public virtual int cinputInt => 0;
        public virtual int cinputFloat => 0;

        protected void CheckInputInt(int iinputInt)
        {
            if (iinputInt > cinputInt - 1)
                throw new IndexOutOfRangeException();
        }

        protected void CheckInputFloat(int iinputFloat)
        {
            if (iinputFloat > cinputFloat - 1)
                throw new IndexOutOfRangeException();
        }

        public virtual float GetInputFloat(int iinputFloat)
        {
            CheckInputFloat(iinputFloat);
            return float.NaN;
        }

        public virtual void SetInputFloat(int iinputFloat, float val)
        {
            CheckInputFloat(iinputFloat);
        }

        public virtual int GetInputInt(int iinputInt)
        {
            CheckInputInt(iinputInt);
            return int.MinValue;
        }

        public virtual void SetInputInt(int iinputInt, int val)
        {
            CheckInputInt(iinputInt);
        }
        #endregion

        protected internal abstract Vector3d Color(Vector3d pt);

        public override void Dispose()
        {
        }
    }
}
