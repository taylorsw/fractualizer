using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D11;
using Util;

namespace Fractals
{
    public abstract class Camera
    {
        public abstract void ResetCamera();
        public abstract void MoveTo(Vector3 pt);
        public abstract void MoveBy(Vector3 vk);
        public abstract void RotateCamera(float dagrUp, float dagrRight);
        public abstract void RotateCamera(Vector3 vkAxis, float dagr);
        public abstract void RollBy(float dagd);
        public abstract void Orbit(Vector3 axis, float dagd);
        public abstract void LookAt(Vector3 pt);

        public abstract Vector3 ptCamera { get; }
        public abstract Vector3 vkCamera { get; }
        public abstract Vector3 vkCameraOrtho { get; }
        public abstract Vector3 ptPlaneCenter { get; }
        public abstract Vector3 vkCameraRight { get; }
        public abstract Vector2 rsScreen { get; }
        public abstract Vector2 rsViewPlane { get; }
        public abstract float duNear { get; }
    }

    partial class RaytracerFractal
    {
        public override Camera camera => cameraRF;

        #region CameraRF
        private CameraRF cameraRF;
        private class CameraRF : Camera
        {
            private RaytracerFractal raytracer;

            public CameraRF(RaytracerFractal raytracer)
            {
                this.raytracer = raytracer;
            }

            public override void ResetCamera()
            {
                raytracer._raytracerfractal = Initial(raytracer.width, raytracer.height);
            }

            public override void MoveTo(Vector3 pt)
            {
                raytracer._raytracerfractal.ptCamera = pt;
            }

            public override void MoveBy(Vector3 vk)
            {
                raytracer._raytracerfractal.ptCamera = (Vector3)raytracer._raytracerfractal.ptCamera + vk;
            }

            public override void RotateCamera(float dagrUp, float dagrRight)
            {
                RotateCamera(vkAxis: vkCameraRight, dagr: dagrUp);
                RotateCamera(vkAxis: raytracer._raytracerfractal.vkCameraOrtho, dagr: dagrRight);
            }

            public override void RotateCamera(Vector3 vkAxis, float dagr)
            {
                var matrix = Matrix.RotationAxis(vkAxis, dagr);
                Vector4 vkCameraRotated = Vector3.Transform(raytracer._raytracerfractal.vkCamera, matrix);
                Vector4 vkCameraOrthoRotated = Vector3.Transform(raytracer._raytracerfractal.vkCameraOrtho, matrix);
                raytracer._raytracerfractal.vkCamera = vkCameraRotated.PerspectiveDivide();
                raytracer._raytracerfractal.vkCameraOrtho = vkCameraOrthoRotated.PerspectiveDivide();
            }

            public override void RollBy(float dagd)
            {
                Matrix matRotate = Matrix.RotationAxis(raytracer._raytracerfractal.vkCamera, MathUtil.DegreesToRadians(dagd));
                raytracer._raytracerfractal.vkCameraOrtho = Vector3.Transform(raytracer._raytracerfractal.vkCameraOrtho, matRotate).PerspectiveDivide();
                Debug.Assert(Math.Abs(Vector3.Dot(raytracer._raytracerfractal.vkCamera, raytracer._raytracerfractal.vkCameraOrtho)) < 0.0001);
            }

            public override void Orbit(Vector3 axis, float dagd)
            {
                Matrix matRotate = Matrix.RotationAxis(axis, MathUtil.DegreesToRadians(dagd));
                raytracer._raytracerfractal.ptCamera = Vector3.Transform(raytracer._raytracerfractal.ptCamera, matRotate).PerspectiveDivide();
            }

            public override void LookAt(Vector3 pt)
            {
                // Get the vector from the eye to the target point
                Vector3 vkToTarget = (pt - (Vector3)raytracer._raytracerfractal.ptCamera).Normalized();

                // Project that vector onto plane P with normal vkCameraUp
                Vector3 vkCameraUp = -raytracer._raytracerfractal.vkCameraOrtho;
                Vector3 vkProjected =
                    Vector3.Cross(
                        Vector3.Cross(vkCameraUp, vkToTarget),
                        vkCameraUp).Normalized();
                Debug.Assert(vkProjected.IsOrthogonalTo(vkCameraUp));

                // Compute the angle between the projected vector and vkCamera (which are coplanar in P)
                // This is the amount horizontal "swivel" that must take place to avoid roll
                float dot = Vector3.Dot(vkProjected, raytracer._raytracerfractal.vkCamera);
                float dagrBetween = (float)Math.Acos(dot);

                // Check if there is any swivel required.
                Vector3 vkCameraRightNew;
                if (!float.IsNaN(dagrBetween) && dagrBetween != 0)
                {
                    // Now rotate vkCamera around vkCameraUp by the computed angle
                    Matrix matRotate = Matrix.RotationAxis(vkCameraUp, dagrBetween);
                    Vector3 vkCameraSwiveled = Vector3.Transform(raytracer._raytracerfractal.vkCamera, matRotate).PerspectiveDivide();
                    Debug.Assert(vkCameraSwiveled.IsOrthogonalTo(raytracer._raytracerfractal.vkCameraOrtho));

                    // Compute a new vkCameraRight (swivel the old vkCameraRight the same amount as vkCamera)
                    vkCameraRightNew = Vector3.Transform(vkCameraRight, matRotate).PerspectiveDivide();
                }
                else
                    vkCameraRightNew = vkCameraRight;

                // Finally, compute the new vkCameraOrtho by crossing the new vkCamera and the new vkCameraRight
                // todo: does this need to cross in a different order depending on dot(target, rightNew)???
                Vector3 vkCameraOrthoNew = Vector3.Cross(vkToTarget, vkCameraRightNew);

                raytracer._raytracerfractal.vkCamera = vkToTarget.Normalized();
                raytracer._raytracerfractal.vkCameraOrtho = vkCameraOrthoNew.Normalized();
            }

            public override Vector3 ptCamera => raytracer._raytracerfractal.ptCamera;

            public override Vector3 vkCamera => raytracer._raytracerfractal.vkCamera;

            public override Vector3 vkCameraOrtho => raytracer._raytracerfractal.vkCameraOrtho;

            public override Vector3 ptPlaneCenter => raytracer._raytracerfractal.ptCamera + raytracer._raytracerfractal.vkCamera * raytracer._raytracerfractal.duNear;

            public override Vector3 vkCameraRight => Vector3.Cross(raytracer._raytracerfractal.vkCameraOrtho, raytracer._raytracerfractal.vkCamera).Normalized();

            public override Vector2 rsScreen => raytracer._raytracerfractal.rsScreen;

            public override Vector2 rsViewPlane => raytracer._raytracerfractal.rsViewPlane;

            public override float duNear => raytracer._raytracerfractal.duNear;
        }
        #endregion

        public override void Initialize(Device device, DeviceContext deviceContext)
        {
            cameraRF = new CameraRF(this);
            _raytracerfractal = Initial(width, height);
            base.Initialize(device, deviceContext);
        }

        private const float dxView = 1;
        public static _RaytracerFractal Initial(int width, int height) => new _RaytracerFractal(
            ptCamera: new Vector3(0, 0, -1.5f),
            vkCamera: new Vector3(0, 0, 1),
            vkCameraOrtho: new Vector3(0, 1, 0),
            duNear: 0.5f,
            rsScreen: new Vector2(width, height),
            rsViewPlane: new Vector2(dxView, dxView*height/width),
            cLight: 1,
            fogA: 1.0f,
            ptLight: new Vector3f(2, 0, -1), 
            ptLight2: new Vector3f(-2, 0, -1.5f));

        public override void Dispose()
        {
            buffer.Dispose();
            base.Dispose();
        }
    }
}
