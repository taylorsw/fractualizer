namespace Mandelbasic
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ControllerMandelbasic controllerMandelbasic = new ControllerMandelbasic())
            {
                controllerMandelbasic.Run();
            }
        }
    }
}
