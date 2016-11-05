using System;
using SharpDX.Direct3D11;

namespace Fractals
{
    public abstract class FPLGenBase : IDisposable
    {
        public string StShaderPath()
        {
            return "_gen/" + GetType().Name + ".gen.hlsl";
        }

        public virtual void ResetInputs() { }

        protected virtual void InitializeBuffer(Device device, DeviceContext deviceContext) { }
        protected virtual void UpdateBuffer(Device device, DeviceContext deviceContext) { }

        public virtual void Initialize(Device device, DeviceContext deviceContext)
        {
            InitializeBuffer(device, deviceContext);
        }

        public virtual void Update(Device device, DeviceContext deviceContext)
        {
            UpdateBuffer(device, deviceContext);
        }

        public abstract void Dispose();
    }
}
