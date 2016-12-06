using System;
using System.Windows.Forms;
using Audio;
using Evtc;
using SharpDX;

namespace Mandelbasic
{
    public abstract class EvtcAudio : Evtc
    {
        private readonly AudioProcessor processor;

        public EvtcAudio(Form form, Controller controller) : base(form, controller)
        {
            processor = new AudioProcessor();
        }

        protected abstract string StSong();
        public override void Setup()
        {
            base.Setup();
            processor.StartProcessor("Resources/" + StSong());
        }

        public override void DoEvents(float dtms)
        {
            bool fBeat = processor.fBeat;
            if (!fBeat)
            {
                return;
            }

            for (int i = 1; i < lightManager.clight; i++)
            {
                var light = lightManager[i];
                light.rgbLight = Vector3.One - light.rgbLight;
            }
        }
    }
}
