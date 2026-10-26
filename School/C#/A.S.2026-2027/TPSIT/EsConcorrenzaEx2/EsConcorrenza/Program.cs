class Program
{
    /*
        Un e-commerce di prodotti alimentari vende un solo prodotto "in promozione" (alla volta) in quantità illimitata. 
        Un operatore entra periodicamente nel sito, sostituisce il prodotto in promozione con uno nuovo in un tempo finito ed esce. 
        Il cliente entra nel sito e compra, sempre in un tempo finito, il prodotto in promozione.
        L'operatore può entrare nel sito, per aggiornare il prodotto, solo se non ci sono dei clienti all'interno.
        I clienti possono entrare nel sito per comprare il prodotto solo se non c'è l'operatore all'interno e solo fino ad un massimo di N (numero randomico <= "numero registro" + 10) 
        clienti nello stesso momento.
        L'operatore sceglie il prodotto da mettere in promozione da un array finito dei prodotti vendibili.
        Operatore e clienti accedono quindi in concorrenza al prodotto con thread separati.
        Ad ogni modifica da parte dell'operatore, quest'ultimo non può rimettere in promozione il prodotto attuale.
     */

    const int MAX_OPERAZIONI = 10; //Max numero di operazioni che l'operatore e i clienti possono fare

    static int numeroRegistro = 3;
    static int maxClienti = numeroRegistro + 10;
    static int ProdottoInPromozione = -1;

    const int numeroProdotti = 5; // Numero totale di prodotti disponibili che si scambieranno in promozione

    static int[] ProdottiDisponibili = new int[numeroProdotti]; // Array dei prodotti disponibili

    static SemaphoreSlim semaforoOperatore = new SemaphoreSlim(0); //s1
    static SemaphoreSlim semaforoCliente = new SemaphoreSlim(0); //s2


    static void Operatore()
    {
        for(int i =0; i<MAX_OPERAZIONI; i++)
        {
            int newProd = -1; // Variabile per il nuovo prodotto da mettere in promozione
            do {
                newProd = new Random().Next(0, numeroProdotti); // Sceglie un prodotto casuale da mettere in promozione
            } while (newProd == ProdottoInPromozione); // Assicura che il nuovo prodotto non sia lo stesso del prodotto attuale in promozione

            ProdottoInPromozione = newProd;

            Console.WriteLine($"Operatore: Sostituisco il prodotto in promozione con il prodotto {newProd} \n");

            semaforoCliente.Release();
            semaforoOperatore.Wait();

        }
    }


    static void Cliente()
    {
        for (int i = 0; i < MAX_OPERAZIONI; i++)
        {
            semaforoCliente.Wait();

            int numeroClientiAttuali = new Random().Next(1, maxClienti + 1); // Numero casuale di clienti che entrano nel sito per acquistare il prodotto in promozione

            Console.WriteLine($"Cliente: Numero di clienti attuali: {numeroClientiAttuali}");

            for (int j = 0; j< numeroClientiAttuali; j++) // Simula l'acquisto del prodotto in promozione da parte di un numero massimo di clienti
            {
                Console.WriteLine($"Cliente {j} ha acquistato prodotto {ProdottoInPromozione}");
            }
            Console.WriteLine("\n ------------------------------------ \n");
            
            semaforoOperatore.Release();

        }
    }

    static void InizializzaProdotti()
    {
        for (int i = 0; i < numeroProdotti; i++)
        {
            ProdottiDisponibili[i] = i; // Inizializza l'array dei prodotti disponibili con valori da 0 a numeroProdotti-1
        }
    }


    static void Main(string[] args)
    {
        InizializzaProdotti();
        Parallel.Invoke(
            () => Operatore(),
            () => Cliente()
        );
    }
}
