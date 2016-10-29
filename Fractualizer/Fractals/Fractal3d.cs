using System;
using SharpDX.Direct3D11;

namespace Fractals
{
    public abstract class Fractal3d : IDisposable
    {
        // todo make this not virtual
        public virtual string StShaderPath()
        {
            return "Shaders/" + GetType().Name + ".hlsl";
        }

        // todo cache results
        public abstract double DuEstimate(Vector3d pt);

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
