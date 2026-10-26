
using EsDeadLock;
namespace EsDeadLock
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //realizzare delle funzioni che mandino in deadlock i thread
            Thread thread1 = new Thread(DeadLocker.Function1);
            Thread thread2 = new Thread(DeadLocker.Function2);

            thread1.Start();
            thread2.Start();

            thread1.Join();
            thread2.Join();

        }

    }
}


