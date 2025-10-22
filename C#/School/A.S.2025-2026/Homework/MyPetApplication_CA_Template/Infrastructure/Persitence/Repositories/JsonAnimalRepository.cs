using Application.Interfaces;
using Domain.Model.Entities;
using Infrastructure.Persistence.Mappers;
using Infrastructure.Persitence.Dto;
using Infrastructure.Persitence.Mapper;
using System.Text.Json;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository responsabile della persistenza degli animali.
    /// Conosce il domain per costruire entità, ma il domain non conosce l'infrastructure.
    /// Cache interna implementata tramite Dictionary per accesso efficiente per nome.
    /// </summary>
    public class JsonAnimalRepository : IAnimalRepository
    {
        private readonly string _filePath = "animals.json";
        //La chiave del dizionario è il nome dell’animale (string). Il valore è l’entità di dominio Animal.
        //Le operazioni di recupero, aggiornamento o rimozione di un animale vengono svolte in O(1) 
        //StringComparer.OrdinalIgnoreCase Serve a rendere la chiave case-insensitive.
        private readonly Dictionary<string, Animal> _cache = new(StringComparer.OrdinalIgnoreCase);
        private bool _initialized = false;

        /// <summary>
        /// Garantisce che la cache sia popolata leggendo il file JSON solo una volta.
        /// </summary>
        private void EnsureLoaded()
        {
            if (_initialized) return;

            if (!File.Exists(_filePath))
            {
                _initialized = true;
                return;
            }

            //se non ho ancora letto il file
            //tiro fuori il contenuto dle file sotto forma di stringa
            var json = File.ReadAllText(_filePath);
            //deserializzo il contenuto del file in una lista di dto
            var dtos = JsonSerializer.Deserialize<List<AnimalPersistenceDto>>(json) ?? new List<AnimalPersistenceDto>();

            //per ogni dto
            foreach (var dto in dtos)
            {
                //lo strasformo in oggetto animale (cat o dog)
                Animal animal = dto.ToEntity(); // Mapper Persistence DTO -> Domain
               //lo aggiungo alla cache
                _cache[animal.Name] = animal;
            }

            _initialized = true;
        }

        public void Add(Animal animal)
        {
            EnsureLoaded();

            // Controllo duplicati case-insensitive - Repository non fa logica di business si limita a sollevare una exception
            if (_cache.ContainsKey(animal.Name))
                throw new InvalidOperationException($"Animale '{animal.Name}' già esistente.");
            
            //aggiungo l'animale alla cache
            _cache[animal.Name] = animal;
            //rendo persistente l'aggiunta nel file
            SaveToFile();
        }

        public void Update(Animal animal)
        {
            EnsureLoaded();

            if (!_cache.ContainsKey(animal.Name))
                throw new InvalidOperationException($"Animale '{animal.Name}' non trovato per l'aggiornamento.");

            _cache[animal.Name] = animal;
            SaveToFile();
        }

        public void Remove(string name)
        {
            EnsureLoaded();

            if (!_cache.Remove(name))
                throw new InvalidOperationException($"Animale '{name}' non trovato per la rimozione.");

            SaveToFile();
        }

        public Animal? GetByName(string name)
        {
            EnsureLoaded();

            Animal? animal;
            _cache.TryGetValue(name, out animal);
            return animal;
        }

        public IEnumerable<Animal> GetAll()
        {
            EnsureLoaded();
            return _cache.Values;
        }

        private void SaveToFile()
        {
            /*
             * _cache.Values --> tutti gli animal del dictionary
             * a => a.ToPersistenceDto() --> per ogni animal restituisce il dto di persistenza
             * dtos è la lista di tutti i dto degli animal presenti nella cache
             */
            var dtos = _cache.Values.Select(a => a.ToPersistenceDto()).ToList();
            var json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}
