﻿using System.Windows.Forms;
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
            processor.StartProcessor("Resources/moby.mp3");
        }

        public override void DoEvents()
        {
            base.DoEvents();
            float duRange = processor.max - processor.min;
            scene.camera.param += du * 0.03f * (processor.val - duRange / 2) / duRange;
        }
    }
}
