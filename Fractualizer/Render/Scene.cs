using System;
using System.Diagnostics;
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
            // Unit Vector
            public Vector3 vkCamera;

            [FieldOffset(32)]
            // Unit Vector
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

            [FieldOffset(76)]
            public float fogA;

            public Vector3 ptPlaneCenter => ptCamera + vkCamera * duNear;

            // Unit Vector
            public Vector3 vkCameraRight => Vector3.Cross(vkCameraDown, vkCamera).Normalized();

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
                this.fogA = 1.0f;
            }

            public void RotateCamera(float dagrUp, float dagrRight)
            {
                RotateCamera(vkCameraRight, dagrUp);
                RotateCamera(vkCameraDown, dagrRight);
            }

            public void RotateCamera(Vector3 vkAxis, float dagr)
            {
                var matrix = Matrix.RotationAxis(vkAxis, dagr);
                Vector4 vkCameraRotated = Vector3.Transform(vkCamera, matrix);
                Vector4 vkCameraDownRotated = Vector3.Transform(vkCameraDown, matrix);
                vkCamera = vkCameraRotated.PerspectiveDivide();
                vkCameraDown = vkCameraDownRotated.PerspectiveDivide();
            }

            public void RollBy(float dagd)
            {
                Matrix matRotate = Matrix.RotationAxis(vkCamera, MathUtil.DegreesToRadians(dagd));
                vkCameraDown = Vector3.Transform(vkCameraDown, matRotate).PerspectiveDivide();
                Debug.Assert(Math.Abs(Vector3.Dot(vkCamera, vkCameraDown)) < 0.0001);
            }

            public void Orbit(Vector3 axis, float dagd)
            {
                Matrix matRotate = Matrix.RotationAxis(axis, MathUtil.DegreesToRadians(dagd));
                ptCamera = Vector3.Transform(ptCamera, matRotate).PerspectiveDivide();
            }

            public void LookAt(Vector3 pt)
            {
                // Get the vector from the eye to the target point
                Vector3 vkToTarget = (pt - ptCamera).Normalized();

                // Project that vector onto plane P with normal vkCameraUp
                Vector3 vkCameraUp = -vkCameraDown;
                Vector3 vkProjected = 
                    Vector3.Cross(
                        Vector3.Cross(vkCameraUp, vkToTarget),
                        vkCameraUp).Normalized();
                Debug.Assert(vkProjected.IsOrthogonalTo(vkCameraUp));

                // Compute the angle between the projected vector and vkCamera (which are coplanar in P)
                // This is the amount horizontal "swivel" that must take place to avoid roll
                float dot = Vector3.Dot(vkProjected, vkCamera);
                float dagrBetween = (float)Math.Acos(dot);

                // Check if there is any swivel required.
                Vector3 vkCameraRightNew;
                if (!float.IsNaN(dagrBetween) && dagrBetween != 0)
                {
                    // Now rotate vkCamera around vkCameraUp by the computed angle
                    Matrix matRotate = Matrix.RotationAxis(vkCameraUp, dagrBetween);
                    Vector3 vkCameraSwiveled = Vector3.Transform(vkCamera, matRotate).PerspectiveDivide();
                    Debug.Assert(vkCameraSwiveled.IsOrthogonalTo(vkCameraDown));

                    // Compute a new vkCameraRight (swivel the old vkCameraRight the same amount as vkCamera)
                    vkCameraRightNew = Vector3.Transform(vkCameraRight, matRotate).PerspectiveDivide();
                }
                else
                    vkCameraRightNew = vkCameraRight;
                
                // Finally, compute the new vkCameraDown by crossing the new vkCamera and the new vkCameraRight
                // todo: does this need to cross in a different order depending on dot(target, rightNew)???
                Vector3 vkCameraDownNew = Vector3.Cross(vkToTarget, vkCameraRightNew);

                this.vkCamera = vkToTarget.Normalized();
                this.vkCameraDown = vkCameraDownNew.Normalized();
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
