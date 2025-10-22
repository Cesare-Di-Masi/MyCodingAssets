using Application.Dto;
using Application.UseCases;
using Infrastructure.Persistence.Repositories;

class Program
{
    /*
Flusso completo

Console → legge input → costruisce AnimalDto.

Console → chiama AnimalService.AddAnimal(dto).

Service → controlla regole di business (nome unico) → converte DTO → Entity.

Service → chiama _animalRepository.Add(animal).

Repository JSON → aggiorna cache e salva in file animals.json.
     */
    static void Main()
    {
        var repo = new JsonAnimalRepository();
        var service = new AnimalService(repo);

        // Input simulato
        var newDog = new AnimalDto(
            Name: "MOXA",
            Breed: "Border Collie",
            Birthday: new DateTime(2024, 2, 14),
            FavouriteGame: "Pallina",
            FavouriteFood: "Crocchette",
            FavouriteChewing: "Nerbo di bue",
            Type: "Dog",
            Visits: new List<VeterinaryVisitDto>()
        );
        
        try
        {
            // Chiamata allo use case
            service.Create(newDog);

            Console.WriteLine("Animale inserito con successo!");

            // Recupero e stampa
            var animal = service.GetByName("Moxa");
            Console.WriteLine($"Trovato: {animal?.Name}, razza: {animal?.Breed}");
        }catch(Exception ex) { Console.WriteLine(ex.ToString()); }


        // Input simulato
        var newCat = new AnimalDto(
            Name: "PETRA",
            Breed: "gatto tricolore",
            Birthday: new DateTime(2022, 5, 1),
            FavouriteGame: "Pallina",
            FavouriteFood: "Crocchette",
            FavouriteChewing: null,
            Type: "Cat",
            Visits: new List<VeterinaryVisitDto>()
        );

        try
        {
            // Chiamata allo use case
            service.Create(newCat);

            Console.WriteLine("Animale inserito con successo!");
            // Recupero e stampa
            var animal = service.GetByName("Petra");
            Console.WriteLine($"Trovato: {animal?.Name}, razza: {animal?.Breed}");
        }catch (Exception ex) { Console.WriteLine( ex.ToString()); }

        var myAnimals = service.GetAll();
        foreach(var animal in myAnimals)
        {
            Console.WriteLine($" {animal?.Name}, razza: {animal?.Breed}");
        }
    }
}
