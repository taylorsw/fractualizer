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
        protected virtual void OnBeat() { }
        protected float DtmsBeatInterval() => processor.dtmsBeatInterval;
        public override void Setup()
        {
            base.Setup();
            processor.StartProcessor("Resources/" + StSong());
        }

        public override void DoEvents(float dtms)
        {
            if (processor.fBeat)
            {
                OnBeat();
            }
        }
    }
}
