using SharpDX;
using System;
using System.Diagnostics;

namespace Render
{
    public class Raytracer
    {
        public static Vector4 Raytrace(Scene scene, Vector2 ptScreen)
        {
            var camera = scene.camera;
            Vector3 ptPlaneCenter = camera.ptCamera + camera.vkCamera * camera.duNear;
            Vector3 vkDown = camera.vkCameraDown;
            Vector3 vkRight = Vector3.Cross(vkDown, camera.vkCamera);

            Vector2 vkFromScreenCenter = ptScreen - camera.rsScreen / 2;
            Vector2 vkFromPlaneCenter = new Vector2(vkFromScreenCenter.X * camera.rsViewPlane.X / camera.rsScreen.X, vkFromScreenCenter.Y * camera.rsViewPlane.Y / camera.rsScreen.Y);
            Vector3 ptPlane = ptPlaneCenter + vkRight * vkFromPlaneCenter.X + vkDown * vkFromPlaneCenter.Y;

            Vector3 vkRay = (ptPlane - camera.ptCamera).Normalized();

            Vector4 marched = ray_marching(camera, camera.ptCamera, vkRay);
            return marched;
        }

        static Vector4 ColorFromVec(Vector3 v)
        {
            return new Vector4(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z), 1);
        }

        static Vector4 ray_marching(Scene.Camera camera, Vector3 pt, Vector3 vk)
        {
            float duPixelRadius = camera.rsViewPlane.X / camera.rsScreen.X;
            float duTotal = 0;
            for (int i = 0; i < 48; ++i)
            {
                float du = DE_sphere(pt);
                pt += du * vk;
                duTotal += du;
                float duEpsilon = (float)(0.5 * duTotal / camera.duNear * duPixelRadius);
                if (du < duEpsilon)
                    return new Vector4(pt, i);
            }
            return new Vector4(pt, -1);
        }

        static float DE_sphere(Vector3 pos)
        {
            return pos.Length() - 1;
        }
    }
}
