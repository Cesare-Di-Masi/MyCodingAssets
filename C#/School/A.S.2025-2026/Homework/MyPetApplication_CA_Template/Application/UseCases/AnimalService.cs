using Application.Dto;
using Application.Interfaces;
using Application.Mappers;
using Domain.Model.Entities;

namespace Application.UseCases
{
    /// <summary>
    /// Contiene gli use case relativi agli animali.
    /// Lavora con i DTO a livello di input/output.
    /// Usa i mapper per tradurre da/verso il dominio.
    /// </summary>
    public class AnimalService
    {
        private readonly IAnimalRepository _repository;

        //Dependency Injection
        public AnimalService(IAnimalRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Crea un nuovo animale.
        /// </summary>
        public void Create(AnimalDto dto)
        {
            // Validazione base
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Il nome dell'animale è obbligatorio.");

            // Verifica se esiste già (business rule → livello application)
            var existing = _repository.GetByName(dto.Name);
            if (existing != null)
                throw new InvalidOperationException($"Un animale con nome '{dto.Name}' esiste già.");

            // Mapping verso dominio --> mappo AnimalDto in Animal
            Animal entity = dto.ToEntity();

            // Persistenza
            _repository.Add(entity);
        }

        /// <summary>
        /// Aggiorna un animale esistente in base al nome.
        /// </summary>
        public void AddVisit(string name, VeterinaryVisitDto visit)
        {
            //recupero dalla repo l'oggetto animal
            Animal? animal = _repository.GetByName(name);
            if (animal == null)
                throw new InvalidOperationException($"Impossibile aggiungere una visita: animale '{name}' non trovato.");
            
            //aggiungo all'oggetto animal la visita
            animal.AddVisit(visit.ToEntity(animal));
            //rendo persistente la modifica
            _repository.Update(animal);           
        }

        /// <summary>
        /// Rimuove un animale per nome.
        /// </summary>
        public void Remove(string name)
        {
            //recupera l'oggetto dalla repo
            Animal? existing = _repository.GetByName(name);
            if (existing == null)
                throw new InvalidOperationException($"Impossibile rimuovere: animale '{name}' non trovato.");
            //rendo la rimozione persistente
            _repository.Remove(name);
        }

        /// <summary>
        /// Recupera un animale per nome e lo restituisce sotto forma di DTO
        /// </summary>
        public AnimalDto? GetByName(string name)
        {
            //recupero l'animale dalla repo
            var entity = _repository.GetByName(name);
            //lo mappo in un dto e lo restituisco alla presentation
            return entity?.ToDto();
        }

        /// <summary>
        /// Recupera tutte le visite di un animale comprese in un intervallo di date
        /// e le restituisce sotto forma di DTO
        /// </summary>
        public IEnumerable<VeterinaryVisitDto> GetVisitsByDateRange(string animalName, DateTime from, DateTime to)
        {
            //recupera l'animale dalla repo 
            var animal = _repository.GetByName(animalName);
            if (animal == null)
                throw new InvalidOperationException($"Animale '{animalName}' non trovato.");

            //creo la lista di dto da restituire
            List<VeterinaryVisitDto> visits = new List<VeterinaryVisitDto>();
            
            //ciclo sugli oggetti visita dell'animale
            foreach(VeterinaryVisit v in  animal.VisitList)
            {
                //se la visita è nel range di interesse
                if (v.Date >= from && v.Date <= to)
                { //aggiungo la visita trasformata in dto alla lista da restituire
                    visits.Add(v.ToDto());
                }
            }

            /*
            var filtered = animal.VisitList
                .Where(v => v.Date >= from && v.Date <= to)
                .Select(v => v.ToDto())
                .ToList();
            */
            return visits;
        }

        /// <summary>
        /// restituisce il nuemro di visite dell'animale
        /// se devo restituire un semplice numero non ho bisogno del DTO
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public int GetNumberOfVisits(string name)
        {
            //recupero l'animale dalla repo
            var animal = _repository.GetByName(name);
            if (animal == null)
                throw new InvalidOperationException($"Animale '{animal}' non trovato.");

            //conto quante sono le visite
            return animal.VisitList.Count;
        }

        /// <summary>
        /// Restituisce tutti gli animali registrati sotto forma di DTO.
        /// </summary>
        public IEnumerable<AnimalDto> GetAll()
        {
            //creo la lista di dto da restituire alla presentation
            List<AnimalDto> animals = new List<AnimalDto>();

            //ciclo su tutti gli oggetti animal presenti nella repo
            foreach (var animal in _repository.GetAll())
            {
                //aggiungo l'animale trasformato in dto alla lista
                animals.Add(animal.ToDto());
            }
            return animals;
            //return _repository.GetAll().Select(a => a.ToDto()).ToList();
        }
    }
}
