using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickAndTock
{
    public static class Clock
    {
        public static AutoResetEvent tickEvent = new AutoResetEvent(true);


        public static void Tick(object obj)
        {
            while (true)
            {
                tickEvent.WaitOne();
                Console.WriteLine("Tick");
                Thread.Sleep(1000);
                tickEvent.Set();
            }
        }

        public static void Tock(object obj)
        {
            while (true)
            {
                tickEvent.WaitOne();
                Console.WriteLine("Tock");
                Thread.Sleep(1000);
                tickEvent.Set();
            }
        }

    }
}
