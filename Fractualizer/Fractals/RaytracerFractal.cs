using SharpDX;
using SharpDX.Direct3D11;
using Util;

namespace Fractals
{
    partial class RaytracerFractal
    {
        public RaytracerFractal(Scene scene, int width, int height) : base(scene, width, height)
        {
            _raytracerfractal = new _RaytracerFractal();
            lightManager = new LightManager(this);
            cameraRF = new CameraRF(this, width, height);
        }

        public override void Update(Device device, DeviceContext deviceContext)
        {
            lightManager.SyncWithBuffer();
            base.Update(device, deviceContext);
        }
    }
}
