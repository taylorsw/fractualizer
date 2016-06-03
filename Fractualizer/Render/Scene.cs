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

        [StructLayout(LayoutKind.Explicit, Size=80)]
        public struct Camera
        {
            public static Camera Initial(int width, int height)
            {
                return new Camera(
                    ptCamera: new Vector3(2.4f, 0, 0),
                    vkCamera: new Vector3(-1, 0, 0),
                    vkCameraDown: new Vector3(0, 1, 0),
                    duNear: 0.5f,
                    rsScreen: new Vector2(width, height),
                    rsViewPlane: new Vector2(dxView, dxView*height/width));
            }

            [FieldOffset(0)]
            public Vector3 ptCamera;

            [FieldOffset(16)]
            public Vector3 vkCamera;

            [FieldOffset(32)]
            public Vector3 vkCameraDown;

            [FieldOffset(48)]
            public Vector2 rsScreen;

            [FieldOffset(56)]
            public Vector2 rsViewPlane;

            [FieldOffset(64)]
            public readonly float duNear;

            [FieldOffset(68)]
            public float param;

            [FieldOffset(72)]
            public float param2;

            public Vector3 ptPlaneCenter => ptCamera + vkCamera * duNear;
            public Vector3 vkCameraRight => Vector3.Cross(vkCameraDown, vkCamera);

            public Camera(Vector3 ptCamera, Vector3 vkCamera, Vector3 vkCameraDown, float duNear, Vector2 rsScreen, Vector2 rsViewPlane)
            {
                this.ptCamera = ptCamera;
                this.vkCamera = vkCamera;
                this.duNear = duNear;
                this.vkCameraDown = vkCameraDown;
                this.rsScreen = rsScreen;
                this.rsViewPlane = rsViewPlane;
                this.param = 8.0f;
                this.param2 = 1.0f;
            }

            public void RotateAbout(Vector3 vkAxis, float dag)
            {
                Vector4 ptCamRotated = Vector3.Transform(ptCamera, Matrix.RotationAxis(vkAxis, dag * (float)Math.PI / 180f));
                ptCamera = ptCamRotated.Xyz() / ptCamRotated.W;
                vkCamera = (Vector3.Zero - ptCamera).Normalized();
            }
        }

        public Scene(int width, int height, Fractal3d fractal)
        {
            this.fractal = fractal;
            this.camera = Camera.Initial(width, height);
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
