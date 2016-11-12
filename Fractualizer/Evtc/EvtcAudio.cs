using System;
using System.Windows.Forms;
using Audio;
using Evtc;
using Fractals;

namespace Mandelbasic
{
    public class EvtcAudio : EvtcAnim
    {
        private readonly AudioProcessor processor;

        public EvtcAudio(Form form, Controller controller) : base(form, controller)
        {
            processor = new AudioProcessor();
            processor.StartProcessor("Resources/lovesosa.mp3");
        }

        public override void DoEvents(float dtms)
        {
            base.DoEvents(dtms);
            float duRange = processor.max - processor.min;
            if (scene.fractal.cinputFloat > 0)
            {
                scene.fractal.SetInputFloat(0,
                    scene.fractal.GetInputFloat(0) +
                    du*Math.Abs(0.015f*(processor.energy - duRange/2)/duRange));
            }
        }
    }
}
