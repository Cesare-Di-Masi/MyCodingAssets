using System;
using System.Threading;

class Program
{

    static int NumValori;
    static int buffer;
    static int min = 1; //valore minimo generato dal random
    static int max = 100; //valore massimo generato dal random
    static Dictionary<int , bool>exist = new Dictionary<int, bool>(); //dizionario per memorizzare i numeri già generati

    static SemaphoreSlim bufferVuoto = new SemaphoreSlim(0); //s1
    static SemaphoreSlim bufferPieno = new SemaphoreSlim(0); //s2


    static void Metti()
    {
        for (int i = 0; i < NumValori; i++) //ripetiamo la funzione per il numero di valori da inserire
        {
            int numeroS = 0;
            do //ciclo di controllo generazione numero casuale, se il numero è già presente continua a generare un nuovo numero finché non ne trova uno che non è presente nel dizionario
            {
                numeroS = Random.Shared.Next(min, max)*2; // generiamo numeri SOLO pari
            } while (exist.ContainsKey(numeroS));
                exist.Add(numeroS, true ); //aggiungiamo il numero generato al dizionario per evitare duplicati

            Console.WriteLine("Inserito: " + numeroS);
            buffer = numeroS; //inseriamo il numero generato nel buffer
            
            bufferPieno.Release(); //s2.v()
            bufferVuoto.Wait(); //s1.p()
        }
    }


    static void Togli()
    {
        for (int i = 0; i < NumValori; i++)
        {
            bufferPieno.Wait(); //s2.p() //se la funzione Togli() viene eseguita prima di Metti() il thread si blocca qui in attesa che Metti() inserisca un valore nel buffer
            int numeroL = buffer;
            Console.WriteLine("Tolto: " + numeroL); //stampa il numero tolto dal buffer
            bufferVuoto.Release(); //s1.v()
        }
    }


    static void Main(string[] args)
    {
        NumValori = Random.Shared.Next(min, max / 2);
        Parallel.Invoke(Metti, Togli);
        
    }
}
