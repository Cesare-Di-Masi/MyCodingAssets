namespace TickAndTock
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Thread tickThread = new Thread(Clock.Tick);
            Thread tockThread = new Thread(Clock.Tock);

            tickThread.Start();
            tockThread.Start();
        }
    }
}
