﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Direct3D11;

namespace Fractals
{
    public abstract class Fractal3d : IDisposable
    {
        private const int cduCacheMax = 20;
        private readonly Dictionary<Vector3d, double> mppt_duDe = new Dictionary<Vector3d, double>(cduCacheMax);
        private Queue<Vector3d> rgpt = new Queue<Vector3d>(cduCacheMax);
        private int cduCache = 0;
        public double DuEstimate(Vector3d pt)
        {
            //Debug.WriteLine("cache size: " + cduCache);
            double duDeCached;
            if (mppt_duDe.TryGetValue(pt, out duDeCached))
            {
                //Debug.WriteLine("cache hit");
                return duDeCached;
            }

            if (cduCache == cduCacheMax)
            {
                cduCache--;
                Vector3d ptRemoved = rgpt.Dequeue();
                bool fRemoved = mppt_duDe.Remove(ptRemoved);
                Debug.Assert(fRemoved);
            }

            double duDe = DuEstimateI(pt);
            mppt_duDe[pt] = duDe;
            rgpt.Enqueue(pt);
            cduCache++;

            return duDe;
        }

        protected abstract double DuEstimateI(Vector3d pt);

        public string StShaderPath()
        {
            return "_gen/" + GetType().Name + ".hlsl";
        }

        public virtual void ResetInputs() { }

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

        public virtual void InitializeBuffer(Device device, DeviceContext deviceContext) { }

        public virtual void UpdateBuffer(Device device, DeviceContext deviceContext) { }

        public void Dispose()
        {
            DisposeI();
        }

        protected virtual void DisposeI() { }
    }
}
