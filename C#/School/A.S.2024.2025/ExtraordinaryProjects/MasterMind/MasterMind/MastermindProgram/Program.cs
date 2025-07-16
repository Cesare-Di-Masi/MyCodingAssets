using MastermindLib;

namespace MastermindProgram
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("***Mastermind***");
            Console.WriteLine("Scelga la modalità con cui giocare");
            Console.WriteLine("Facile 6 colori, codice lungo 6, 5 tentativi == 1");
            Console.WriteLine("Difficile 6 colori, codice lungo 6, 5 tentativi == 2");
            Console.WriteLine("Custom, impostazioni personalizzate == 3");
            Console.WriteLine("developer, codice pre impostato nel codice con impostazioni facili == 4");

            bool error = false;
            int choice = 0;
            GameManager game;
            int codeLength = 0, nColours = 0, nAttempts = 0, codeComplexity = 0;
            FixedColoursGenerator fix = new FixedColoursGenerator();

            do
            {
                try
                {
                    choice = int.Parse(Console.ReadLine());
                    if (choice < 1 || choice > 4)
                        throw new Exception("input errato");
                }
                catch (Exception)
                {
                    Console.WriteLine("qualcosa è andato storto, riprovare");
                    error = false;
                }
            } while (error == true);

            if (choice == 1)
            {
                game = new GameManager(false, 6, 6, 5, 1);
            }
            else if (choice == 2)
            {
                game = new GameManager(false, 10, 6, 5, 1);
            }
            else if (choice == 3)
            {
                do
                {
                    try
                    {
                        error = false;
                        Console.WriteLine("***Impostazioni Custom***");
                        Console.WriteLine("1:Lunghezza del codice (4 - 12)");
                        Console.WriteLine("2:Numero di colori possibili (4 - 12)");
                        Console.WriteLine("3: Numero di tentativi (1- infinito)");
                        Console.WriteLine("4:Complessità del codice (1 - 5)");
                        Console.WriteLine("inserire i valori nell'ordine dato");

                        codeLength = int.Parse(Console.ReadLine());
                        nColours = int.Parse(Console.ReadLine());
                        nAttempts = int.Parse(Console.ReadLine());
                        codeComplexity = int.Parse(Console.ReadLine());
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Qualcosa è andato storto, riprovare");
                        error = true;
                    }
                } while (error == true);

                game = new GameManager(false, codeLength, nColours, nAttempts, codeComplexity);
            }
            else
            {
                game = new GameManager(false, 4, 4, 5, 1, fix);
            }

            Console.WriteLine("***Play the Game***");

            GameStatus status = GameStatus.Playing;
            int counter = 0;
            bool rivedere = true;
            Colours col = Colours.Red;

            do
            {
                Colours[] sol = new Colours[game.CodeLength];
                if (rivedere == true)
                {
                    rivedere = false;
                    Console.WriteLine("Scrivi il codice basandoti sui seguenti colori, scrivere il numero vicino al colore");

                    for (int i = 0; i < game.NColours; i++)
                    {
                        Console.WriteLine($"{i}: {col}");
                        col++;
                    }
                    col = 0;
                }

                do
                {
                    try
                    {
                        error = false;
                        for (int i = 0; i < game.CodeLength; i++)
                        {
                            Console.WriteLine($"scrivere altri {sol.Length - i} colori");

                            sol[i] = col + int.Parse(Console.ReadLine());
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("qualcosa è andato storto, riprovare");
                        error = true;
                    }
                } while (error == true);
                status = game.EndOfTheTurn(sol);
                Console.WriteLine($"è tutto sbagliato = {game.IsAllWrong}");
                Console.WriteLine($"colori nella posizione giusta = {game.RightPosition}");
                Console.WriteLine($"colori nella posizione sbagliata = {game.WrongPosition}");
            } while (status == GameStatus.Playing);

            if (status == GameStatus.Lost)
            {
                Console.WriteLine("You have lost :(");
            }
            else
                Console.WriteLine("you have won, congrats. :)");
        }
    }
}