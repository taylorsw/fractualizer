using System;

namespace Mandelbasic
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (ControllerMandelbasic controllerMandelbasic = new ControllerMandelbasic())
                {
                    controllerMandelbasic.Run();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.Read();
                throw;
            }
        }
    }
}
