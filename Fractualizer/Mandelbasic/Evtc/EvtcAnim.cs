using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private float du = -1;
        private float du2 = -1;
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

            const float dagRotateX = 0.008f;
            const float dagRotateY = 0.2f;
            scene.camera.RotateCamera(new Vector3(1, 0, 0), dagRotateX);
            scene.camera.RotateCamera(new Vector3(0, 1, 0), dagRotateY);

            //scene.camera.LookAt(new Vector3(0, 0, 0));
        }
    }
}
