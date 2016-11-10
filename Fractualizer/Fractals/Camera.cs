using SharpDX;

namespace Fractals
{
    public abstract class Camera
    {
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
}
