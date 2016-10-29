using System;
using System.Windows.Forms;
using Render;
using Audio;

namespace Mandelbasic
{
    class EvtcAudio : EvtcAnim
    {
        private readonly AudioProcessor processor;

        public EvtcAudio(Form form, Scene scene) : base(form, scene)
        {
            processor = new AudioProcessor();
            processor.StartProcessor("Resources/lovesosa.mp3");
        }

        public override void DoEvents(float dtms)
        {
            base.DoEvents(dtms);
            float duRange = processor.max - processor.min;
            if (scene.fractalRenderer.fractal.cinputFloat > 0)
            {
                scene.fractalRenderer.fractal.SetInputFloat(0,
                    scene.fractalRenderer.fractal.GetInputFloat(0) +
                    du*Math.Abs(0.015f*(processor.val - duRange/2)/duRange));
            }
        }
    }
}
