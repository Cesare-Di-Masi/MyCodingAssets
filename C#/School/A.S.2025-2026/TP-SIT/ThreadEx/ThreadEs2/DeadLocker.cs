using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsDeadLock
{
    public static class DeadLocker
    {
        static object resource1 = new object();
        static object resource2 = new object();
        public static void Function1()
        {
            lock (resource1)
            {
                Thread.Sleep(5000); // Simula un'operazione
                lock (resource2)
                {
                    Thread.Sleep(5000); // Simula un'operazione
                }
            }
        }

        public static void Function2()
        {
            lock (resource2)
            {
                Thread.Sleep(5000); // Simula un'operazione
                lock (resource1)
                {
                    Thread.Sleep(5000); // Simula un'operazione
                }
            }
        }
    }
}
