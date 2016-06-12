using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Render;
using Audio;

namespace Mandelbasic
{
    class EvtcAudio : EvtcExplorer
    {
        private readonly AudioProcessor processor;

        public EvtcAudio(Form form, Scene scene) : base(form, scene)
        {
            processor = new AudioProcessor();
            processor.StartProcessor("Resources/moby.mp3");
        }

        public override void DoEvents()
        {
            scene.camera.param = 100 * processor.val;
            base.DoEvents();
        }
    }
}
