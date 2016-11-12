using System;
using System.Windows.Forms;
using Audio;
using Evtc;
using Fractals;
using SharpDX;

namespace Mandelbasic
{
    public class EvtcAudio : EvtcAnim
    {
        private readonly AudioProcessor processor;

        public EvtcAudio(Form form, Controller controller) : base(form, controller)
        {
            processor = new AudioProcessor();
            processor.StartProcessor("Resources/dontletmedown.mp3");
        }

        public override void DoEvents(float dtms)
        {
            base.DoEvents(dtms);
            bool fBeat = processor.fBeat;
            if (!fBeat)
            {
                return;
            }

            for (int i = 0; i < raytracer.lightManager.clight; i++)
            {
                var light = raytracer.lightManager[i];
                light.rgbLight = Vector3.One - light.rgbLight;
            }
        }
    }
}
