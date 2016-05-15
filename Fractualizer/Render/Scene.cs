using System;
using SharpDX.Direct3D11;
using D3D11 = SharpDX.Direct3D11;

namespace Render
{
    public interface IHaveScene
    {
        Scene scene { get; }
    }

    public class Scene : IDisposable
    {
        public readonly Fractal fractal;

        public Scene(Fractal fractal)
        {
            this.fractal = fractal;
        }

        public void Initialize(Device device, DeviceContext deviceContext)
        {
            fractal.InitializeFractal(device, deviceContext);
        }

        public void Dispose()
        {
            fractal.Dispose();
        }
    }
}
