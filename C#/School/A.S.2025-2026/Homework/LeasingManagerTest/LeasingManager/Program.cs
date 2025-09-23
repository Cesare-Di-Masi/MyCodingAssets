using LeasingManagerLib;
using System;

public class Program
{
    public static void Main(string[] args)
    {
        LeasingSystem leasingSystem = new LeasingSystem();

        // Inizializzazione con dati di esempio
        leasingSystem.AddVehicle(new Car("CAR001", 50.0, 5, 10, 3));
        leasingSystem.AddVehicle(new Car("CAR002", 75.0, 7, 15, 5));
        leasingSystem.AddVehicle(new Bike("BIKE001", 20.0, 5, 2));
        leasingSystem.AddVehicle(new Bike("BIKE002", 25.0, 10, 3));
        leasingSystem.AddVehicle(new Motorbike("MOTO001", 80.0, HelmetTypes.FullFace, 20, 4));
        leasingSystem.AddVehicle(new Motorbike("MOTO002", 100.0, HelmetTypes.HalfHelmet, 25, 5));

        bool running = true;

        while (running)
        {
            Console.Clear();
            Console.WriteLine("=== SISTEMA DI LEASING VEICOLI ===");
            Console.WriteLine("1. Visualizza tutti i veicoli");
            Console.WriteLine("2. Aggiungi nuovo veicolo");
            Console.WriteLine("3. Noleggia un veicolo");
            Console.WriteLine("4. Restituisci un veicolo");
            Console.WriteLine("5. Esci");
            Console.Write("Seleziona un'opzione: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1": // Visualizza tutti i veicoli
                    Console.Clear();
                    Console.WriteLine("=== ELENCO VEICOLI ===");

                    if (leasingSystem.Vehicles.Count == 0)
                    {
                        Console.WriteLine("Nessun veicolo disponibile nel sistema.");
                    }
                    else
                    {
                        foreach (var vehicle in leasingSystem.Vehicles)
                        {
                            string status = vehicle.IsAvailable ? "Disponibile" : "Noleggiato";
                            Console.WriteLine($"{vehicle.Description()} - Stato: {status}");
                        }
                    }

                    Console.WriteLine("\nPremi un tasto per tornare al menu...");
                    Console.ReadKey();
                    break;

                case "2": // Aggiungi nuovo veicolo
                    Console.Clear();
                    Console.WriteLine("=== AGGIUNGI NUOVO VEICOLO ===");
                    Console.WriteLine("1. Auto");
                    Console.WriteLine("2. Bicicletta");
                    Console.WriteLine("3. Motociclo");
                    Console.WriteLine("4. Annulla");
                    Console.Write("Seleziona tipo veicolo: ");

                    string vehicleType = Console.ReadLine();

                    try
                    {
                        switch (vehicleType)
                        {
                            case "1": // Auto
                                Console.Write("Inserisci targa: ");
                                string carPlate = Console.ReadLine();

                                Console.Write("Inserisci prezzo al giorno: ");
                                double carPrice = double.Parse(Console.ReadLine());

                                Console.Write("Inserisci numero posti: ");
                                int carSeats = int.Parse(Console.ReadLine());

                                Console.Write("Inserisci percentuale sconto: ");
                                int carDiscount = int.Parse(Console.ReadLine());

                                Console.Write("Inserisci giorni per sconto: ");
                                int carDiscountDays = int.Parse(Console.ReadLine());

                                leasingSystem.AddVehicle(new Car(carPlate, carPrice, carSeats, carDiscount, carDiscountDays));
                                Console.WriteLine("Auto aggiunta con successo!");
                                break;

                            case "2": // Bicicletta
                                Console.Write("Inserisci targa: ");
                                string bikePlate = Console.ReadLine();

                                Console.Write("Inserisci prezzo al giorno: ");
                                double bikePrice = double.Parse(Console.ReadLine());

                                Console.Write("Inserisci percentuale sconto: ");
                                int bikeDiscount = int.Parse(Console.ReadLine());

                                Console.Write("Inserisci giorni per sconto: ");
                                int bikeDiscountDays = int.Parse(Console.ReadLine());

                                leasingSystem.AddVehicle(new Bike(bikePlate, bikePrice, bikeDiscount, bikeDiscountDays));
                                Console.WriteLine("Bicicletta aggiunta con successo!");
                                break;

                            case "3": // Motociclo
                                Console.Write("Inserisci targa: ");
                                string motoPlate = Console.ReadLine();

                                Console.Write("Inserisci prezzo al giorno: ");
                                double motoPrice = double.Parse(Console.ReadLine());

                                Console.WriteLine("Tipi casco disponibili:");
                                Console.WriteLine("1. FullFace");
                                Console.WriteLine("2. HalfHelmet");
                                Console.WriteLine("3. OpenFace");
                                Console.Write("Seleziona tipo casco: ");

                                HelmetTypes helmetType = (HelmetTypes)(int.Parse(Console.ReadLine()) - 1);

                                Console.Write("Inserisci percentuale sconto: ");
                                int motoDiscount = int.Parse(Console.ReadLine());

                                Console.Write("Inserisci giorni per sconto: ");
                                int motoDiscountDays = int.Parse(Console.ReadLine());

                                leasingSystem.AddVehicle(new Motorbike(motoPlate, motoPrice, helmetType, motoDiscount, motoDiscountDays));
                                Console.WriteLine("Motociclo aggiunto con successo!");
                                break;

                            case "4": // Annulla
                                break;

                            default:
                                Console.WriteLine("Scelta non valida.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore: {ex.Message}");
                    }

                    Console.WriteLine("\nPremi un tasto per continuare...");
                    Console.ReadKey();
                    break;

                case "3": // Noleggia un veicolo
                    Console.Clear();
                    Console.WriteLine("=== NOLEGGIO VEICOLO ===");
                    Console.Write("Inserisci targa veicolo: ");
                    string leasePlate = Console.ReadLine();

                    Console.Write("Inserisci numero giorni noleggio: ");
                    int days = int.Parse(Console.ReadLine());

                    try
                    {
                        leasingSystem.LeaseVehicle(leasePlate, days);
                        Console.WriteLine("Veicolo noleggiato con successo!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore: {ex.Message}");
                    }

                    Console.WriteLine("\nPremi un tasto per continuare...");
                    Console.ReadKey();
                    break;

                case "4": // Restituisci un veicolo
                    Console.Clear();
                    Console.WriteLine("=== RESTITUZIONE VEICOLO ===");
                    Console.Write("Inserisci targa veicolo: ");
                    string returnPlate = Console.ReadLine();

                    try
                    {
                        leasingSystem.ReturnVehicle(returnPlate);
                        Console.WriteLine("Veicolo restituito con successo!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore: {ex.Message}");
                    }

                    Console.WriteLine("\nPremi un tasto per continuare...");
                    Console.ReadKey();
                    break;

                case "5": // Esci
                    running = false;
                    break;

                default:
                    Console.WriteLine("Scelta non valida. Premi un tasto per continuare...");
                    Console.ReadKey();
                    break;
            }
        }
    }
}