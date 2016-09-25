using System.Windows.Forms;
using Render;
using SharpDX;

namespace Mandelbasic
{
    class EvtcAnim : Evtc
    {
        private readonly RailHover railCam;
        private readonly RailOrbit railLight;

        public EvtcAnim(Form form, Scene scene) : base(form, scene)
        {
            railCam = new RailHover(
                pt => scene.camera.ptCamera = pt, 
                scene.fractal, 
                ptCenter: Vector3.Zero, 
                vkNormal: new Vector3(-1f, 0.5f, 1f), 
                agd_dtms: 20f / 1000,
                duHoverMin: 0.001f,
                duHoverMax: 0.05f,
                sfTravelMax: 2);

            railLight = new RailOrbit(pt => scene.camera.ptLight = pt, Vector3.Zero, new Vector3(1, 1, 1), 60f / 1000);
        }

        protected float du = -1;
        protected float du2 = -1;

        public override void DoEvents(float dtms)
        {
//            scene.camera.param += du * 0.0008f;
//
//            if (scene.camera.param < 1.2)
//                du = 1;
//            else if (scene.camera.param > 10.0)
//                du = -1;
//
//            scene.camera.param2 += du2 * 0.00003f;
//
//            if (scene.camera.param2 < 1.0)
//                du2 = 1;
//            else if (scene.camera.param2 > 3.0)
//                du2 = -1;

            railCam.UpdatePt(scene.camera.ptCamera, dtms);
            railLight.UpdatePt(scene.camera.ptLight, dtms);

            const float dagdRoll = 0.05f;
            scene.camera.RollBy(dagdRoll);

            scene.camera.LookAt(Vector3.Zero);
            scene.camera.RotateCamera(scene.camera.vkCameraRight, MathUtil.DegreesToRadians(45));
        }
    }
}
