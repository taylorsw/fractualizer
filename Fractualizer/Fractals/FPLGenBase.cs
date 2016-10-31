using System;
using SharpDX.Direct3D11;

namespace Fractals
{
    public abstract class FPLGenBase : IDisposable
    {
        public string StShaderPath()
        {
            return "_gen/" + GetType().Name + ".hlsl";
        }

        public virtual void ResetInputs() { }

        internal virtual void InitializeBuffer(Device device, DeviceContext deviceContext) { }
        internal virtual void UpdateBuffer(Device device, DeviceContext deviceContext) { }

        public abstract void Dispose();
    }
}
