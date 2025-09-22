using LeasingManagerLib;
using System;

public class Program
{
    public static void Main(string[] args)
    {
        LeasingSystem leasingSystem = new LeasingSystem();
        bool exit = false;
        string plate;

        Console.WriteLine("=== SISTEMA DI LEASING VEICOLI ===");
        Console.WriteLine("Benvenuto nel sistema di gestione leasing veicoli!");

        while (!exit)
        {
            Console.WriteLine("\n=== MENU PRINCIPALE ===");
            Console.WriteLine("1. Aggiungi nuovo veicolo");
            Console.WriteLine("2. Cerca veicolo per targa");
            Console.WriteLine("3. Calcola prezzo leasing");
            Console.WriteLine("4. Effettua leasing");
            Console.WriteLine("5. Restituisci veicolo");
            Console.WriteLine("6. Mostra descrizione veicolo");
            Console.WriteLine("7. Esci");
            Console.Write("Seleziona un'opzione: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.WriteLine("\n=== AGGIUNGI NUOVO VEICOLO ===");
                    Console.WriteLine("Seleziona tipo veicolo:");
                    Console.WriteLine("1. Auto (Car)");
                    Console.WriteLine("2. Motociclo (Motorbike)");
                    Console.WriteLine("3. Bicicletta (Bike)");
                    Console.Write("Scelta: ");

                    string typeChoice = Console.ReadLine();

                    try
                    {
                        Console.Write("Inserisci targa: ");
                        plate = Console.ReadLine();

                        Console.Write("Inserisci prezzo giornaliero: ");
                        double price = double.Parse(Console.ReadLine());

                        switch (typeChoice)
                        {
                            case "1":
                                Console.Write("Inserisci numero posti: ");
                                int seats = int.Parse(Console.ReadLine());
                                leasingSystem.AddVehicle(new Car(plate, price, seats));
                                Console.WriteLine("Auto aggiunta con successo!");
                                break;

                            case "2":
                                Console.WriteLine("Seleziona tipo casco:");
                                Console.WriteLine("1. FullFace");
                                Console.WriteLine("2. OpenFace");
                                Console.WriteLine("3. HalfHelmet");
                                Console.Write("Scelta: ");
                                string helmetChoice = Console.ReadLine();
                                HelmetTypes helmet = (HelmetTypes)(int.Parse(helmetChoice) - 1);
                                leasingSystem.AddVehicle(new Motorbike(plate, price, helmet));
                                Console.WriteLine("Motociclo aggiunto con successo!");
                                break;

                            case "3":
                                leasingSystem.AddVehicle(new Bike(plate, price));
                                Console.WriteLine("Bicicletta aggiunta con successo!");
                                break;

                            default:
                                Console.WriteLine("Tipo veicolo non valido.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore: {ex.Message}");
                    }
                    break;

                case "2":
                    Console.WriteLine("\n=== CERCA VEICOLO ===");
                    Console.Write("Inserisci targa veicolo: ");
                    plate = Console.ReadLine();

                    try
                    {
                        Vehicle vehicle = leasingSystem.FindVehicle(plate);
                        if (vehicle != null)
                        {
                            Console.WriteLine("Veicolo trovato:");
                            Console.WriteLine(vehicle.Description());
                        }
                        else
                        {
                            Console.WriteLine("Nessun veicolo trovato con questa targa.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore: {ex.Message}");
                    }
                    break;

                case "3":
                    Console.WriteLine("\n=== CALCOLA PREZZO LEASING ===");
                    Console.Write("Inserisci targa veicolo: ");
                    plate = Console.ReadLine();

                    Console.Write("Inserisci numero giorni: ");
                    string daysInput = Console.ReadLine();

                    try
                    {
                        int days = int.Parse(daysInput);
                        double price = leasingSystem.CalculateTotalPrice(plate, days);
                        Console.WriteLine($"Prezzo totale per {days} giorni: {price:C}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore: {ex.Message}");
                    }
                    break;

                case "4":
                    Console.WriteLine("\n=== EFFETTUA LEASING ===");
                    Console.Write("Inserisci targa veicolo: ");
                    plate = Console.ReadLine();

                    try
                    {
                        leasingSystem.LeaseVehicle(plate);
                        Console.WriteLine("Leasing effettuato con successo!");
                        Console.WriteLine($"Stato attuale: {leasingSystem.GetVehicleDescription(plate)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore: {ex.Message}");
                    }
                    break;

                case "5":
                    Console.WriteLine("\n=== RESTITUISCI VEICOLO ===");
                    Console.Write("Inserisci targa veicolo: ");
                    plate = Console.ReadLine();

                    try
                    {
                        leasingSystem.ReturnVehicle(plate);
                        Console.WriteLine("Veicolo restituito con successo!");
                        Console.WriteLine($"Stato attuale: {leasingSystem.GetVehicleDescription(plate)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore: {ex.Message}");
                    }
                    break;

                case "6":
                    Console.WriteLine("\n=== DESCRIZIONE VEICOLO ===");
                    Console.Write("Inserisci targa veicolo: ");
                    plate = Console.ReadLine();

                    try
                    {
                        string description = leasingSystem.GetVehicleDescription(plate);
                        Console.WriteLine(description);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore: {ex.Message}");
                    }
                    break;

                case "7":
                    exit = true;
                    Console.WriteLine("Grazie per aver utilizzato il sistema. Arrivederci!");
                    break;

                default:
                    Console.WriteLine("Opzione non valida. Riprova.");
                    break;
            }
        }
    }
}