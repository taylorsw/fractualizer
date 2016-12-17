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

//        public override void Initialize(Device device, DeviceContext deviceContext)
//        {
//            Texture2D texture = TextureLoader.CreateTexture2DFromBitmap(device, TextureLoader.LoadBitmap(new SharpDX.WIC.ImagingFactory2(), "Textures/skysphere.jpg"));
//            ShaderResourceView textureView = new ShaderResourceView(device, texture);
//            deviceContext.PixelShader.SetShaderResource(0, textureView);
//
//            base.Initialize(device, deviceContext);
//        }

        public override void Update(Device device, DeviceContext deviceContext)
        {
            lightManager.SyncWithBuffer();
            base.Update(device, deviceContext);
        }
    }
}
