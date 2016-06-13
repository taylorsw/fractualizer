using System.Windows.Forms;
using Render;
using SharpDX;

namespace Mandelbasic
{
    class EvtcAnim : Evtc
    {
        public EvtcAnim(Form form, Scene scene) : base(form, scene)
        {
        }

        protected float du = -1;
        protected float du2 = -1;

        public override void DoEvents()
        {
            scene.camera.param += du*0.008f;
            
            if (scene.camera.param < 0.0)
                du = 1;
            else if (scene.camera.param > 10.0)
                du = -1;
            
            scene.camera.param2 += du2*0.0003f;
            
            if (scene.camera.param2 < 1.0)
                du2 = 1;
            else if (scene.camera.param2 > 3.0)
                du2 = -1;

            const float dagdX = -0.1f;
            const float dagdY = -0.04f;
            const float dagdZ = -0.2f;
            scene.camera.Orbit(Vector3.UnitX, dagdX);
            scene.camera.Orbit(Vector3.UnitY, dagdY);
            scene.camera.Orbit(Vector3.UnitZ, dagdZ);

            const float dagdRoll = 0.05f;
            scene.camera.RollBy(dagdRoll);

            scene.camera.LookAt(Vector3.Zero);
        }
    }
}
