using Application.Dto;
using Application.UseCases;
using Infrastructure.Repositories;

internal class Program
{
    private static void Main(string[] args)
    {
        var catRepo = new JsonCatRepository();
        var adopterRepo = new JsonAdopterRepository();
        var adoptionRepo = new JsonAdoptionRepository();

        var service = new CatteryService(catRepo, adopterRepo, adoptionRepo);

        var newCat = new CatDto("test1", true, new DateOnly(2020, 5, 21), null, "friendly cat", "Palico", "12345M2020ASD");

        try
        {
            service.RegisterNewCat(newCat);
            Console.WriteLine("Cat registered successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering cat: {ex.Message}");
        }
    }
}