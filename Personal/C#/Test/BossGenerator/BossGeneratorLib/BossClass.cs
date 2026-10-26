using System;
using System.Collections.Generic;

namespace BossGeneratorLib
{
    public class BossClass
    {
        // Dizionario statico che mappa i nomi delle classi di boss alle liste di tipi di armi consentiti
        private static Dictionary<string, List<string>> _classWeaponRestrictions = new Dictionary<string, List<string>>();

        // Inizializzazione delle classi di boss predefinite
        static BossClass()
        {
            // Inizializzazione con le classi di boss predefinite
            InitializeDefaultClasses();
        }

        private static void InitializeDefaultClasses()
        {
            // Classe Guerriero - specializzato in armi corpo a corpo pesanti
            _classWeaponRestrictions["Warrior"] = new List<string>
            {
                "Sword", "Axe", "Mace", "Hammer", "Spear"
            };

            // Classe Mago - specializzato in armi magiche
            _classWeaponRestrictions["Mage"] = new List<string>
            {
                "Staff", "Wand"
            };

            // Classe Ladro - specializzato in armi leggere e a distanza
            _classWeaponRestrictions["Rogue"] = new List<string>
            {
                "Dagger", "Bow"
            };

            // Classe Paladino - ibrido tra guerriero e mago
            _classWeaponRestrictions["Paladin"] = new List<string>
            {
                "Sword", "Mace", "Staff"
            };

            // Classe Necromante - specializzato in armi magiche oscure
            _classWeaponRestrictions["Necromancer"] = new List<string>
            {
                "Staff", "Wand"
            };

            // Classe Bestiamante - specializzato in armi da caccia
            _classWeaponRestrictions["Beastmaster"] = new List<string>
            {
                "Bow", "Spear", "Dagger"
            };

            // Classe Berserker - specializzato in armi a due mani
            _classWeaponRestrictions["Berserker"] = new List<string>
            {
                "Axe", "Hammer", "Sword"
            };

            // Classe Arciere - specializzato esclusivamente in archi
            _classWeaponRestrictions["Archer"] = new List<string>
            {
                "Bow"
            };

            // Classe Chiaro - senza restrizioni (può usare qualsiasi arma)
            _classWeaponRestrictions["Generic"] = new List<string>();
        }

        // Proprietà per ottenere o impostare il nome della classe del boss
        public string ClassName { get; set; } = "Generic";

        // Metodo per verificare se un tipo di arma è consentito per questa classe
        public bool IsWeaponAllowed(string weaponType)
        {
            // Se la classe non è nel dizionario, usa la classe generica
            string effectiveClass = _classWeaponRestrictions.ContainsKey(ClassName) ? ClassName : "Generic";

            // Ottieni la lista delle armi consentite per questa classe
            if (_classWeaponRestrictions.TryGetValue(effectiveClass, out var allowedWeapons))
            {
                // Se la lista è vuota, tutte le armi sono consentite
                if (allowedWeapons.Count == 0)
                    return true;

                // Altrimenti verifica se il tipo di arma è nella lista
                return allowedWeapons.Contains(weaponType);
            }

            // Se la classe non è trovata, consenti tutte le armi (comportamento predefinito)
            return true;
        }

        // Metodo per ottenere la lista delle armi consentite per questa classe
        public List<string> GetAllowedWeapons()
        {
            // Se la classe non è nel dizionario, usa la classe generica
            string effectiveClass = _classWeaponRestrictions.ContainsKey(ClassName) ? ClassName : "Generic";

            if (_classWeaponRestrictions.TryGetValue(effectiveClass, out var allowedWeapons))
            {
                return new List<string>(allowedWeapons);
            }

            // Se la classe non è trovata, restituisci lista vuota (tutte le armi consentite)
            return new List<string>();
        }

        // Metodo statico per aggiungere una nuova classe di boss con le sue restrizioni
        public static void AddBossClass(string className, List<string> allowedWeapons)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Il nome della classe non può essere vuoto", nameof(className));

            _classWeaponRestrictions[className] = allowedWeapons ?? new List<string>();
        }

        // Metodo statico per rimuovere una classe di boss
        public static void RemoveBossClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Il nome della classe non può essere vuoto", nameof(className));

            _classWeaponRestrictions.Remove(className);
        }

        // Metodo statico per aggiornare le restrizioni di una classe esistente
        public static void UpdateClassRestrictions(string className, List<string> allowedWeapons)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Il nome della classe non può essere vuoto", nameof(className));

            if (!_classWeaponRestrictions.ContainsKey(className))
                throw new KeyNotFoundException($"La classe '{className}' non esiste");

            _classWeaponRestrictions[className] = allowedWeapons ?? new List<string>();
        }

        // Metodo statico per ottenere tutte le classi di boss disponibili
        public static List<string> GetAvailableClasses()
        {
            return new List<string>(_classWeaponRestrictions.Keys);
        }

        // Metodo statico per ottenere le restrizioni di una classe specifica
        public static List<string> GetClassRestrictions(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Il nome della classe non può essere vuoto", nameof(className));

            if (_classWeaponRestrictions.TryGetValue(className, out var restrictions))
            {
                return new List<string>(restrictions);
            }

            return new List<string>();
        }

        // Metodo per ottenere una descrizione testuale della classe
        public string GetClassDescription()
        {
            switch (ClassName)
            {
                case "Warrior":
                    return "Guerriero - Maestro delle armi corpo a corpo pesanti";

                case "Mage":
                    return "Mago - Specializzato nella magia e nelle armi arcane";

                case "Rogue":
                    return "Ladro - Esperto di furtività e combattimento a distanza";

                case "Paladin":
                    return "Paladino - Sacro guerriero con abilità magiche";

                case "Necromancer":
                    return "Necromante - Maestro della magia oscura e della morte";

                case "Beastmaster":
                    return "Bestiamante - Comandante delle creature selvagge";

                case "Berserker":
                    return "Berserker - Guerriero furioso che brandisce armi a due mani";

                case "Archer":
                    return "Arciere - Specializzato nel combattimento a distanza con l'arco";

                case "Generic":
                    return "Generico - Nessuna specializzazione particolare";

                default:
                    return "Classe personalizzata";
            }
        }

        // Override di ToString per una rappresentazione testuale utile
        public override string ToString()
        {
            var allowedWeapons = GetAllowedWeapons();
            var weaponList = allowedWeapons.Count > 0
                ? string.Join(", ", allowedWeapons)
                : "Tutte le armi";

            return $"{ClassName} - Armi consentite: {weaponList}";
        }
    }
}