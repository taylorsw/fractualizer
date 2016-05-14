using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbasic
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Controller controller = new Controller())
            {
                controller.Run();
            }
        }
    }
}
