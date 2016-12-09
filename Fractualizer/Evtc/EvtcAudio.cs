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
            form.KeyUp += OnKeyUpKludge;
            processor = new AudioProcessor();
        }

        protected bool fDrop { get; private set; }
        private void OnKeyUpKludge(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Space)
            {
                fDrop = !fDrop;
                if (fDrop)
                    OnDropBegin();
                else
                    OnDropEnd();
            }
        }

        public abstract string StSong();
        protected virtual void OnBeat() { }
        protected virtual void OnDropBegin() { }
        protected virtual void OnDropEnd() { }

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
