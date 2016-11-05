using System;
using SharpDX.Direct3D11;

namespace Fractals
{
    public abstract class Raytracer : FPLGenBase
    {
        public readonly Scene scene;
        public readonly Camera camera;

        protected Fractal3d fractal => scene.fractal;

        protected Raytracer(Scene scene, int width, int height)
        {
            this.scene = scene;
            this.camera = new Camera(width, height);
        }

        public override void Initialize(Device device, DeviceContext deviceContext)
        {
            base.Initialize(device, deviceContext);
            camera.Initialize(device, deviceContext);
            scene.Initialize(device, deviceContext);
        }

        public override void Update(Device device, DeviceContext deviceContext)
        {
            base.Update(device, deviceContext);
            camera.UpdateBuffers(device, deviceContext);
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

        public void ResetSceneAndCamera()
        {
            scene.ResetScene();
            camera.ResetCamera();
        }

        public override void Dispose()
        {
            camera.Dispose();
            scene.Dispose();
        }
    }

    public class RaytracerDummy : Raytracer
    {
        public RaytracerDummy(Scene scene, int width, int height) : base(scene, width, height) { }

        protected override Vector4d RgbaTrace(Vector4d pos)
        {
            throw new NotImplementedException();
        }
    }

    public class Scene : IDisposable
    {
        public readonly Random rand = new Random(1984);

        public readonly Fractal3d fractal;

        public Scene(Fractal3d fractal)
        {
            this.fractal = fractal;
        }

        internal void Initialize(Device device, DeviceContext deviceContext)
        {
            fractal.Initialize(device, deviceContext);
        }

        internal void UpdateBuffers(Device device, DeviceContext deviceContext)
        {
            fractal.Update(device, deviceContext);
        }

        public void ResetScene()
        {
            fractal.ResetInputs();
        }

        public void Dispose()
        {
            fractal.Dispose();
        }
    }
}
