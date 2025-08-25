using WeaponGenerator;

namespace WeaponDNA
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Weapon DNA Crossover Example");
            var rng = new Random(42);
            var a = WeaponDna.GenerateRandom(520, rng);
            var b = WeaponDna.GenerateRandom(520, rng);

            a.Validate(out var issuesA);
            b.Validate(out var issuesB);

            var child = WeaponDna.Crossover(a, b, rng);
            child.Validate(out var issuesChild);

            Console.WriteLine($"A genes: {a.Genes.Count} id={a.Id} issues={issuesA.Count}");
            Console.WriteLine($"B genes: {b.Genes.Count} id={b.Id} issues={issuesB.Count}");
            Console.WriteLine($"Child genes: {child.Genes.Count} id={child.Id} issues={issuesChild.Count}");
            Console.WriteLine(child.ToJson(indented: true));
        }
    }
}