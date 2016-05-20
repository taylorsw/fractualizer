using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX;
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
        private const float dxView = 1;
        private D3D11.Buffer cameraBuffer;

        public Camera camera;
        public readonly Fractal3d fractal;

        public struct Camera
        {
            public Vector4 ptCamera;
            public Vector4 vkCamera;
            public Vector4 vkCameraDown;
            public Vector2 rsScreen;
            public Vector2 rsViewPlane;
            public readonly float duNear;
            private readonly float ignore;
            private readonly float ignore2;
            private readonly float ignore3;

            public Vector4 ptPlaneCenter => ptCamera + vkCamera * duNear;
            public Vector4 vkCameraRight => vkCameraDown.Cross3(vkCamera);

            public Camera(Vector4 ptCamera, Vector4 vkCamera, Vector4 vkCameraDown, float duNear, Vector2 rsScreen, Vector2 rsViewPlane)
            {
                this.ptCamera = ptCamera;
                this.vkCamera = vkCamera;
                this.duNear = duNear;
                this.vkCameraDown = vkCameraDown;
                this.ignore = float.NaN;
                this.ignore2 = float.NaN;
                this.ignore3 = float.NaN;
                this.rsScreen = rsScreen;
                this.rsViewPlane = rsViewPlane;
            }
        }

        public Scene(int width, int height, Fractal3d fractal)
        {
            this.fractal = fractal;
            this.camera = new Camera(
                ptCamera: new Vector4(4, 0, 0, 1),
                vkCamera: new Vector4(-1, 0, 0, 1),
                vkCameraDown: new Vector4(0, 1, 0, 1),
                duNear: 0.5f,
                rsScreen: new Vector2(width, height),
                rsViewPlane: new Vector2(dxView, dxView * height / width));
        }

        public void Initialize(Device device, DeviceContext deviceContext)
        {
            cameraBuffer = D3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref camera);
            deviceContext.PixelShader.SetConstantBuffer(0, cameraBuffer);

            fractal.InitializeFractal(device, deviceContext);
        }

        public void UpdateBuffers(Device device, DeviceContext deviceContext)
        {
            deviceContext.UpdateSubresource(ref camera, cameraBuffer);
        }

        public void Dispose()
        {
            cameraBuffer.Dispose();
            fractal.Dispose();
        }
    }
}
