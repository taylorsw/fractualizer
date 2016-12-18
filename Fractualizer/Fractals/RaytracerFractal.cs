using SharpDX;
using SharpDX.Direct3D11;
using Util;

namespace Fractals
{
    partial class RaytracerFractal
    {
        public RaytracerFractal(Scene scene, int width, int height, Rgparam rgparam = null) : base(scene, width, height, rgparam ?? Rgparam.Default)
        {
            _raytracerfractal = new _RaytracerFractal();
            lightManager = new LightManager(this);
            cameraRF = new CameraRF(this, width, height);
        }

        partial class _RaytracerFractal
        {
            public Texture CreateTexture_skysphere(RaytracerFractal raytracer, Device device, DeviceContext deviceContext, int slot)
            {
                return new Texture(device, deviceContext, raytracer.rgparam.stPathSkysphere, slot);
            }
        }

        public override void Update(Device device, DeviceContext deviceContext)
        {
            lightManager.SyncWithBuffer();
            base.Update(device, deviceContext);
        }
    }
}
