using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scanner
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ControllerScanner controllerScanner = new ControllerScanner())
            {
                controllerScanner.Run();
            }
        }
    }
}
