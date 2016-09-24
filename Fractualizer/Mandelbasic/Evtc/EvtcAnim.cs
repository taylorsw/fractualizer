using System.Windows.Forms;
using Render;
using SharpDX;

namespace Mandelbasic
{
    class EvtcAnim : Evtc
    {
        private readonly RailOrbit railCam;
        private readonly RailOrbit railLight;

        public EvtcAnim(Form form, Scene scene) : base(form, scene)
        {
            railCam = new RailOrbit(pt => scene.camera.ptCamera = pt, Vector3.Zero, Vector3.UnitX, 12f / 1000);
            railLight = new RailOrbit(pt => scene.camera.ptLight = pt, Vector3.Zero, new Vector3(1, 1, 1), 60f / 1000);
        }

        protected float du = -1;
        protected float du2 = -1;

        public override void DoEvents(float dtms)
        {
            scene.camera.param += du * 0.008f;

            if (scene.camera.param < 1.2)
                du = 1;
            else if (scene.camera.param > 10.0)
                du = -1;

            scene.camera.param2 += du2 * 0.0003f;

            if (scene.camera.param2 < 1.0)
                du2 = 1;
            else if (scene.camera.param2 > 3.0)
                du2 = -1;

            railCam.UpdatePt(scene.camera.ptCamera, dtms);
            railLight.UpdatePt(scene.camera.ptLight, dtms);

            const float dagdRoll = 0.05f;
            scene.camera.RollBy(dagdRoll);

            scene.camera.LookAt(Vector3.Zero);
        }
    }
}
