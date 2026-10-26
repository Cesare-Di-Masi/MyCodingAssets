using Application.Dto;
using Application.Interface;
using Application.UseCase;
using Infrastructure.Repo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleBlog
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped<IBlogPostRepo, BlogPostJsonRepo>(); //addScoped perché vogliamo una nuova istanza del repository per ogni ciclo di esecuzione del menu, in modo da avere sempre i dati aggiornati dal file json
                    services.AddScoped<BlogPostService>();
                })
                .Build();

            await RunApp(host.Services);
        }

        private static async Task RunApp(IServiceProvider services)
        {
            var blogService = services.GetRequiredService<BlogPostService>();

            while (true)
            {
                MostraMenu();

                var scelta = Console.ReadLine()?.Trim();
                Console.Clear();

                try
                {
                    switch (scelta)
                    {
                        case "1": await Crea(blogService); break;
                        case "2": await Lista(blogService); break;
                        case "3": await Visualizza(blogService); break;
                        case "4": await Modifica(blogService); break;
                        case "5": await Elimina(blogService); break;
                        case "6": await TestCompleto(blogService); break;
                        case "0": return;
                        default: Console.WriteLine("Scelta non valida."); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERRORE: {ex.Message}");
                }

                Console.WriteLine("\nPremi un tasto...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        private static void MostraMenu()
        {
            Console.WriteLine("========== BLOG MANAGER ==========");
            Console.WriteLine("1. Crea articolo");
            Console.WriteLine("2. Lista articoli");
            Console.WriteLine("3. Visualizza articolo");
            Console.WriteLine("4. Modifica articolo");
            Console.WriteLine("5. Elimina articolo");
            Console.WriteLine("6. Test completo CRUD");
            Console.WriteLine("0. Esci");
            Console.Write("Scelta: ");
        }

        private static async Task Crea(BlogPostService service)
        {
            Console.Write("Titolo: ");
            var title = Console.ReadLine() ?? "";

            Console.Write("Contenuto: ");
            var content = Console.ReadLine() ?? "";

            await service.CreateArticleAsync(new BlogPostDto(title, content));
            Console.WriteLine("Articolo creato.");
        }

        private static async Task Lista(BlogPostService service)
        {
            var posts = await service.GetAllArticlesAync();

            if (!posts.Any())
            {
                Console.WriteLine("Nessun articolo.");
                return;
            }

            foreach (var p in posts)
            {
                Console.WriteLine($"\nID: {p.Id}");
                Console.WriteLine($"Titolo: {p.Title}");
                Console.WriteLine($"Data: {p.CreatedAt}");
                Console.WriteLine($"Contenuto: {p.Content}");
                Console.WriteLine(new string('-', 50));
            }
        }

        private static async Task Visualizza(BlogPostService service)
        {
            Console.Write("ID: ");
            var id = Console.ReadLine()!;

            var post = await service.SearchById(id);

            if (post == null)
            {
                Console.WriteLine("Articolo non trovato.");
                return;
            }

            Console.WriteLine($"\nTitolo: {post.Title}");
            Console.WriteLine($"Creato: {post.CreatedAt}");
            Console.WriteLine($"\n{post.Content}");
        }

        private static async Task Modifica(BlogPostService service)
        {
            var posts = await service.GetAllArticlesAync();
            foreach (var p in posts)
            {
                Console.WriteLine($"\nID: {p.Id}");
                Console.WriteLine($"Titolo: {p.Title}");
                Console.WriteLine($"Data: {p.CreatedAt}");
                Console.WriteLine($"Contenuto: {p.Content}");
                Console.WriteLine(new string('-', 50));
            }

            Console.WriteLine("\nScegli l'articolo da modificare:");

            Console.Write("ID: ");
            var id = Console.ReadLine()!;

            Console.Write("Nuovo titolo: ");
            var title = Console.ReadLine() ?? "";

            Console.Write("Nuovo contenuto: ");
            var content = Console.ReadLine() ?? "";

            await service.UpdatePostAsync(id, new BlogPostDto(title, content));
            Console.WriteLine("Articolo aggiornato.");
        }

        private static async Task Elimina(BlogPostService service)
        {
            var posts = await service.GetAllArticlesAync();
            foreach (var p in posts)
            {
                Console.WriteLine($"\nID: {p.Id}");
                Console.WriteLine($"Titolo: {p.Title}");
                Console.WriteLine($"Data: {p.CreatedAt}");
                Console.WriteLine($"Contenuto: {p.Content}");
                Console.WriteLine(new string('-', 50));
            }

            Console.WriteLine("\nScegli l'articolo da eliminare:");

            Console.Write("ID: ");
            var id = Console.ReadLine()!;

            await service.DeleteArticleAsync(id);
            Console.WriteLine("Articolo eliminato.");
        }

        private static async Task TestCompleto(BlogPostService service)
        {
            Console.WriteLine("TEST CRUD COMPLETO\n");

            var dto = new BlogPostDto("Test Title", "Test Content");
            await service.CreateArticleAsync(dto);

            var all = await service.GetAllArticlesAync();
            var id = all.Last().Id!;

            Console.WriteLine($"Creato: {id}");

            var loaded = await service.SearchById(id);
            Console.WriteLine($"Letto: {loaded?.Title}");

            await service.UpdatePostAsync(id, new BlogPostDto("Updated", "Updated Content"));
            Console.WriteLine("Aggiornato.");

            await service.DeleteArticleAsync(id);
            Console.WriteLine("Eliminato.");

            Console.WriteLine("\nTEST COMPLETATO");
        }
    }
}