using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;

namespace Fractals
{
    public class Camera : IDisposable
    {
        public readonly int width, height;
        // todo make private
        public CameraData cameraData;
        private SharpDX.Direct3D11.Buffer cameraBuffer;

        public Camera(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.cameraData = CameraData.Initial(width, height);
        }

        internal void Initialize(Device device, DeviceContext deviceContext)
        {
            cameraBuffer = Util.BufferCreate(device, deviceContext, 0, ref cameraData);
        }

        internal void UpdateBuffers(Device device, DeviceContext deviceContext)
        {
            Util.UpdateBuffer(device, deviceContext, cameraBuffer, ref cameraData);
        }

        public void ResetCamera()
        {
            cameraData = CameraData.Initial(width, height);
        }

        public void MoveTo(Vector3 pt)
        {
            cameraData.ptCamera = pt;
        }

        public void MoveBy(Vector3 vk)
        {
            cameraData.ptCamera = cameraData.ptCamera + vk;
        }

        public void RotateCamera(float dagrUp, float dagrRight)
        {
            RotateCamera(vkCameraRight, dagrUp);
            RotateCamera(cameraData.vkCameraDown, dagrRight);
        }

        public void RotateCamera(Vector3 vkAxis, float dagr)
        {
            var matrix = Matrix.RotationAxis(vkAxis, dagr);
            Vector4 vkCameraRotated = Vector3.Transform(cameraData.vkCamera, matrix);
            Vector4 vkCameraDownRotated = Vector3.Transform(cameraData.vkCameraDown, matrix);
            cameraData.vkCamera = vkCameraRotated.PerspectiveDivide();
            cameraData.vkCameraDown = vkCameraDownRotated.PerspectiveDivide();
        }

        public void RollBy(float dagd)
        {
            Matrix matRotate = Matrix.RotationAxis(cameraData.vkCamera, MathUtil.DegreesToRadians(dagd));
            cameraData.vkCameraDown = Vector3.Transform(cameraData.vkCameraDown, matRotate).PerspectiveDivide();
            Debug.Assert(Math.Abs(Vector3.Dot(cameraData.vkCamera, cameraData.vkCameraDown)) < 0.0001);
        }

        public void Orbit(Vector3 axis, float dagd)
        {
            Matrix matRotate = Matrix.RotationAxis(axis, MathUtil.DegreesToRadians(dagd));
            cameraData.ptCamera = Vector3.Transform(cameraData.ptCamera, matRotate).PerspectiveDivide();
        }

        public void LookAt(Vector3 pt)
        {
            // Get the vector from the eye to the target point
            Vector3 vkToTarget = (pt - cameraData.ptCamera).Normalized();

            // Project that vector onto plane P with normal vkCameraUp
            Vector3 vkCameraUp = -cameraData.vkCameraDown;
            Vector3 vkProjected =
                Vector3.Cross(
                    Vector3.Cross(vkCameraUp, vkToTarget),
                    vkCameraUp).Normalized();
            Debug.Assert(vkProjected.IsOrthogonalTo(vkCameraUp));

            // Compute the angle between the projected vector and vkCamera (which are coplanar in P)
            // This is the amount horizontal "swivel" that must take place to avoid roll
            float dot = Vector3.Dot(vkProjected, cameraData.vkCamera);
            float dagrBetween = (float)Math.Acos(dot);

            // Check if there is any swivel required.
            Vector3 vkCameraRightNew;
            if (!float.IsNaN(dagrBetween) && dagrBetween != 0)
            {
                // Now rotate vkCamera around vkCameraUp by the computed angle
                Matrix matRotate = Matrix.RotationAxis(vkCameraUp, dagrBetween);
                Vector3 vkCameraSwiveled = Vector3.Transform(cameraData.vkCamera, matRotate).PerspectiveDivide();
                Debug.Assert(vkCameraSwiveled.IsOrthogonalTo(cameraData.vkCameraDown));

                // Compute a new vkCameraRight (swivel the old vkCameraRight the same amount as vkCamera)
                vkCameraRightNew = Vector3.Transform(vkCameraRight, matRotate).PerspectiveDivide();
            }
            else
                vkCameraRightNew = vkCameraRight;

            // Finally, compute the new vkCameraDown by crossing the new vkCamera and the new vkCameraRight
            // todo: does this need to cross in a different order depending on dot(target, rightNew)???
            Vector3 vkCameraDownNew = Vector3.Cross(vkToTarget, vkCameraRightNew);

            cameraData.vkCamera = vkToTarget.Normalized();
            cameraData.vkCameraDown = vkCameraDownNew.Normalized();
        }

        public Vector3 ptCamera => cameraData.ptCamera;

        public Vector3 vkCamera => cameraData.vkCamera;

        public Vector3 vkCameraDown => cameraData.vkCameraDown;

        public Vector3 ptPlaneCenter => cameraData.ptCamera + cameraData.vkCamera * cameraData.duNear;

        public Vector3 vkCameraRight => Vector3.Cross(cameraData.vkCameraDown, cameraData.vkCamera).Normalized();

        public Vector2 rsScreen => cameraData.rsScreen;

        public Vector2 rsViewPlane => cameraData.rsViewPlane;

        public float duNear => cameraData.duNear;

        [StructLayout(LayoutKind.Explicit, Size = 96)]
        public struct CameraData
        {
            private const float dxView = 1;
            public static CameraData Initial(int width, int height)
            {
                return new CameraData(
                    ptCamera: new Vector3(0, 0, -1.5f),
                    vkCamera: new Vector3(0, 0, 1),
                    vkCameraDown: new Vector3(0, 1, 0),
                    duNear: 0.5f,
                    rsScreen: new Vector2(width, height),
                    rsViewPlane: new Vector2(dxView, dxView * height / width),
                    ptLight: new Vector3(2, 0, -1),
                    ptLight2: new Vector3(-2, 0, -1.5f));
            }

            [FieldOffset(0)]
            internal Vector3 ptCamera;

            [FieldOffset(12)]
            internal readonly float duNear;

            [FieldOffset(16)]
            // Unit Vector
            internal Vector3 vkCamera;

            [FieldOffset(28)]
            internal int cLight;

            [FieldOffset(32)]
            // Unit Vector
            internal Vector3 vkCameraDown;

            [FieldOffset(44)]
            internal float fogA;

            [FieldOffset(48)]
            internal Vector2 rsScreen;

            [FieldOffset(56)]
            internal Vector2 rsViewPlane;

            [FieldOffset(64)]
            public Vector3 ptLight;

            [FieldOffset(80)]
            public Vector3 ptLight2;

            public CameraData(Vector3 ptCamera, Vector3 vkCamera, Vector3 vkCameraDown, float duNear, Vector2 rsScreen, Vector2 rsViewPlane, Vector3 ptLight, Vector3 ptLight2)
            {
                this.ptCamera = ptCamera;
                this.vkCamera = vkCamera;
                this.duNear = duNear;
                this.vkCameraDown = vkCameraDown;
                this.rsScreen = rsScreen;
                this.rsViewPlane = rsViewPlane;
                this.fogA = 1.0f;
                this.ptLight = ptLight;
                this.ptLight2 = ptLight2;
                this.cLight = 2;
            }
        }

        public void Dispose()
        {
            cameraBuffer.Dispose();
        }
    }
}
