using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;

namespace Fractals
{
    public abstract class Raytracer : Prog
    {
        public readonly Scene scene;
        public Raytracer(Scene scene)
        {
            this.scene = scene;
        }

        public void Initialize(Device device, DeviceContext deviceContext)
        {
            scene.Initialize(device, deviceContext);
        }

        public void UpdateBuffers(Device device, DeviceContext deviceContext)
        {
            scene.UpdateBuffers(device, deviceContext);
        }

//        public void CPURender()
//        {
//            int width = (int)scene.camera.rsScreen.X;
//            int height = (int)scene.camera.rsScreen.Y;
//            Bitmap bitmap = new Bitmap(width, height);
//            for (int x = 0; x < width; x++)
//            {
//                for (int y = 0; y < height; y++)
//                {
//                    var color = Raytracer.Raytrace(scene, new SharpDX.Vector2(x, y));
//                    bitmap.SetPixel(x, y, Color.FromArgb(IntComponentFromDouble(color.X), IntComponentFromDouble(color.Y), IntComponentFromDouble(color.Z)));
//                }
//            }
//
//            bitmap.Save("test.jpg");
//        }
//
//        private static int IntComponentFromDouble(double component) => (int)(255 * Saturate((float)Math.Abs(component)));

        public static float Saturate(float x) => Math.Max(0, Math.Min(1, x));

        protected abstract Vector4d RgbaTrace(Vector4d pos);
    }

    public class RaytracerDummy : Raytracer
    {
        public RaytracerDummy(Scene scene) : base(scene)
        {
        }

        protected override Vector4d RgbaTrace(Vector4d pos)
        {
            throw new NotImplementedException();
        }
    }

    public class Scene : IDisposable
    {
        public readonly Random rand = new Random(1984);

        private const float dxView = 1;

        private readonly int width, height;

        private SharpDX.Direct3D11.Buffer cameraBuffer;
        public Camera camera;
        public readonly Fractal3d fractal;

        [StructLayout(LayoutKind.Explicit, Size = 96)]
        public struct Camera
        {
            public static Camera Initial(int width, int height)
            {
                return new Camera(
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
            public Vector3 ptCamera;

            [FieldOffset(12)]
            public readonly float duNear;

            [FieldOffset(16)]
            // Unit Vector
            public Vector3 vkCamera;

            [FieldOffset(28)]
            public int cLight;

            [FieldOffset(32)]
            // Unit Vector
            public Vector3 vkCameraDown;

            [FieldOffset(44)]
            public float fogA;

            [FieldOffset(48)]
            public Vector2 rsScreen;

            [FieldOffset(56)]
            public Vector2 rsViewPlane;

            [FieldOffset(64)]
            public Vector3 ptLight;

            [FieldOffset(80)]
            public Vector3 ptLight2;

            public Vector3 ptPlaneCenter => ptCamera + vkCamera * duNear;

            // Unit Vector
            public Vector3 vkCameraRight => Vector3.Cross(vkCameraDown, vkCamera).Normalized();

            public Camera(Vector3 ptCamera, Vector3 vkCamera, Vector3 vkCameraDown, float duNear, Vector2 rsScreen, Vector2 rsViewPlane, Vector3 ptLight, Vector3 ptLight2)
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
            this.width = width;
            this.height = height;
            this.fractal = fractal;
            this.camera = Camera.Initial(width, height);
        }

        internal void Initialize(Device device, DeviceContext deviceContext)
        {
            cameraBuffer = Fractals.Util.BufferCreate(device, deviceContext, 0, ref camera);
            fractal.InitializeFractal(device, deviceContext);
        }

        internal void UpdateBuffers(Device device, DeviceContext deviceContext)
        {
            Fractals.Util.UpdateBuffer(device, deviceContext, cameraBuffer, ref camera);
            fractal.UpdateBuffer(device, deviceContext);
        }

        public void ResetScene()
        {
            camera = Camera.Initial(width, height);
            fractal.ResetInputs();
        }

        public void Dispose()
        {
            cameraBuffer.Dispose();
            fractal.Dispose();
        }
    }
}
