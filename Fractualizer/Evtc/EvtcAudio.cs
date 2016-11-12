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
            processor.StartProcessor("Resources/callonme.mp3");
        }

        const float duHoverMin = 0.1f;
        const float duHoverMax = 0.3f;
        const float duHover = 0.05f;
        public override void DoEvents(float dtms)
        {
            base.DoEvents(dtms);
            bool fBeat = processor.fBeat;
            if (!fBeat)
            {
                foreach (var railHover in rgrailHoverBallLight)
                {
                    if (railHover.duHoverMin > duHoverMin)
                    {
                        railHover.duHoverMin -= duHover;
                        railHover.duHoverMax -= duHover;
                    }
                }
                return;
            }

            for (int i = 0; i < raytracer.lightManager.clight; i++)
            {
                var light = raytracer.lightManager[i];
                light.rgbLight = Vector3.One - light.rgbLight;
            }

            foreach (var railHover in rgrailHoverBallLight)
            {
                railHover.duHoverMin = duHoverMin * 3;
                railHover.duHoverMax = duHoverMax * 3;
            }
        }
    }
}
