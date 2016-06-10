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
