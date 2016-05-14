using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Render;

namespace Mandelbasic
{
    class Controller : IDisposable
    {
        readonly Renderer renderer;

        public Controller()
        {
            renderer = new Renderer();
        }

        public void DoEventLoop()
        {
            renderer.Run();
        }

        public void Dispose()
        {
            renderer.Dispose();
        }
    }
}
