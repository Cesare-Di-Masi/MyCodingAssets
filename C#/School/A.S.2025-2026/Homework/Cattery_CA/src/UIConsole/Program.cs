using Application.Dto;
using Application.UseCases;
using Infrastructure.Repositories;
class Program
{
    static void Main(string[] args)
    {
        var catRepo = new JsonCatRepository();
        var adopterRepo = new JsonAdopterRepository();
        var adoptionRepo = new JsonAdoptionRepository();

        var service = new CatteryService(catRepo, adopterRepo, adoptionRepo);

        var newCat = new CatDto("test1", true, DateOnly.FromDateTime(DateTime.Now), null, null, "A friendly cat",null);

        try
        { 
        
            service.RegisterNewCat(newCat);
            Console.WriteLine("New cat registered successfully.");

            var cat = catRepo.GetByName("test1");
            Console.WriteLine(cat != null ? $"Retrieved cat: {cat}" : "Cat not found.");
        }
            catch(Exception ex) { Console.WriteLine(ex.ToString()); }



    }
}