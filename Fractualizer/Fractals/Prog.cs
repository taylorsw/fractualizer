using System;
using SharpDX.Direct3D11;

namespace Fractals
{
    public abstract class Prog : IDisposable
    {
        public string StShaderPath()
        {
            return "_gen/" + GetType().Name + ".hlsl";
        }

        public virtual void ResetInputs() { }

        public virtual void InitializeBuffer(Device device, DeviceContext deviceContext) { }
        public virtual void UpdateBuffer(Device device, DeviceContext deviceContext) { }

        public void Dispose() { DisposeI(); }
        protected virtual void DisposeI() { }
    }
}
