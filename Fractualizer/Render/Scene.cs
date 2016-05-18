using System;
using System.Runtime.InteropServices;
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
        private D3D11.Buffer cameraBuffer;

        private Camera camera;

        private struct Camera
        {
            private readonly Vector4 cameraPos;
            private readonly Vector4 cameraView;
            private readonly Vector4 cameraRoll;
            private readonly float nearDist;
            private readonly float ignore;
            private readonly Vector2 viewDimension;

            public Camera(Vector4 cameraPos, Vector4 cameraView, Vector4 cameraRoll, float nearDist, Vector2 viewDimension)
            {
                this.cameraPos = cameraPos;
                this.cameraView = cameraView;
                this.nearDist = nearDist;
                this.cameraRoll = cameraRoll;
                this.ignore = float.NaN;
                this.viewDimension = viewDimension;
            }
        }

        public readonly Fractal fractal;

        public Scene(int width, int height, Fractal fractal)
        {
            this.fractal = fractal;
            this.camera = new Camera(new Vector4(4, 0, 0, 1), new Vector4(-1, 0, 0, 1), new Vector4(0, 1, 0, 1), 0.5f, new Vector2(width, height));
        }

        public void Initialize(Device device, DeviceContext deviceContext)
        {
            cameraBuffer = D3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref camera);
            deviceContext.PixelShader.SetConstantBuffer(0, cameraBuffer);

            fractal.InitializeFractal(device, deviceContext);
        }

        public void Dispose()
        {
            cameraBuffer.Dispose();
            fractal.Dispose();
        }
    }
}
