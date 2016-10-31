﻿using System;
using System.Windows.Forms;
using Fractals;
using SharpDX;

namespace Mandelbasic
{
    public class EvtcAnim : Evtc
    {
        private readonly RailHover railCam;
        private readonly RailOrbit railLight1;
        private readonly RailHover railLight2;
        
        public EvtcAnim(Form form, Raytracer raytracer) : base(form, raytracer)
        {
            railCam = new RailHover(
                pt => raytracer.camera.MoveTo(pt), 
                scene.fractal,
                ptCenter: Vector3.Zero, 
                vkNormal: new Vector3(scene.rand.NextFloat(-1.0f, 1.0f), scene.rand.NextFloat(-1.0f, 1.0f), scene.rand.NextFloat(-1.0f, 1.0f)), 
                agd_dtms: 20f / 1000,
                duHoverMin: 0.05f,
                duHoverMax: 0.1f,
                sfTravelMax: 2);

            railLight1 = new RailOrbit(pt => camera.cameraData.ptLight = pt, Vector3.Zero, new Vector3(1, 1, 1), 60f / 1000);
            railLight2 = new RailHover(pt => camera.cameraData.ptLight2 = pt, scene.fractal, Vector3.Zero, new Vector3(0.3f, 0.4f, 0.7f), 0.1f, 0.1f, 0.3f, 2.0f);
        }

        protected float du = -1;
        protected float du2 = -1;

        public override void DoEvents(float dtms)
        {
//            scene.camera.param += du * 0.0014f;
//
//            if (scene.camera.param < 2)
//                du = 1;
//            else if (scene.camera.param > 8.5)
//                du = -1;
//
//            scene.camera.param2 += du2 * 0.000014f;
//
//            if (scene.camera.param2 < 1.0)
//                du2 = 1;
//            else if (scene.camera.param2 > 3.0)
//                du2 = -1;

            railCam.UpdatePt(camera.ptCamera, dtms);
            railLight1.UpdatePt(camera.cameraData.ptLight, dtms);
            railLight2.UpdatePt(camera.cameraData.ptLight2, dtms);

            const float dagdRoll = 0.01f;
            camera.RollBy(dagdRoll);

            camera.LookAt(Vector3.Zero);
            camera.RotateCamera(camera.vkCameraRight, MathUtil.DegreesToRadians(45));
        }
    }
}
