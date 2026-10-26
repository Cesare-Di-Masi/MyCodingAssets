using System;
using System.Collections.Generic;
using System.Linq;
using BossGeneratorLib;
using BossGeneratorLib.BossGeneratorLib;
using System.Text;

namespace BossTrainingConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Imposta i colori della console
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            // Mostra il titolo con stile
            PrintTitle();

            Console.WriteLine("\nInizializzazione del sistema...\n");

            // 1. Crea gli effetti di stato disponibili
            var statusEffects = CreateStatusEffects();

            // 2. Crea il trainer con gli effetti di stato
            var trainer = new Trainer(statusEffects);

            // 3. Configura l'addestramento in modo interattivo
            var config = CreateInteractiveTrainingConfig();

            // 4. Mostra la configurazione scelta
            DisplayConfiguration(config);

            // 5. Esegui l'addestramento
            Console.WriteLine("\nAvvio dell'addestramento...\n");
            var result = trainer.TrainBosses(config);

            // 6. Mostra i risultati
            DisplayResults(result);

            // 7. Test di battaglia tra i boss finali
            TestFinalBosses(result.FinalBosses);

            // Ripristina i colori originali
            Console.ResetColor();
            Console.WriteLine("\nPremi un tasto per uscire...");
            Console.ReadKey();
        }

        private static void PrintTitle()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                           BOSS TRAINING SYSTEM                          ║");
            Console.WriteLine("║                          CONFIGURAZIONE AVANZATA                         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        private static void PrintSectionTitle(string title)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n╠═══════════════════════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║ {title.PadRight(65)} ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        private static void PrintOption(string number, string description)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"║ {number}. ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(description.PadRight(60) + " ║");
        }

        private static void PrintInputPrompt()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("║ Scelta: ");
            Console.ResetColor();
        }

        private static List<StatusEffect> CreateStatusEffects()
        {
            return new List<StatusEffect>
            {
                // Effetti dannosi
                new StatusEffect("Poison", false, false, 5, 1.0f, 10, 1, 0.3f, 0,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                new StatusEffect("Burn", false, false, 8, 1.2f, 8, 1, 0.25f, 0,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                new StatusEffect("Freeze", false, false, 3, 0.8f, 5, 2, 0.2f, 2,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                new StatusEffect("Bleed", false, false, 4, 1.1f, 12, 1, 0.35f, 0,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                // Effetti benefici
                new StatusEffect("Regen", true, false, 10, 1.0f, 5, 1, 0.4f, 0,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                new StatusEffect("DefenseUp", true, true, 15, 1.5f, 10, 0, 0.5f, 1,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                new StatusEffect("SpeedBoost", true, true, 20, 1.2f, 8, 0, 0.4f, 2,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                new StatusEffect("StrengthUp", true, true, 25, 1.3f, 12, 0, 0.3f, 3,
                    new Dictionary<string, string>(), new List<StatusEffect>())
            };
        }

        private static TrainingConfig CreateInteractiveTrainingConfig()
        {
            var config = new TrainingConfig();
            var random = new Random();

            PrintSectionTitle("CONFIGURAZIONE DELL'ADDESTRAMENTO");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Per ogni parametro, scegli tra le opzioni disponibili:");
            Console.ResetColor();

            // PopulationSize
            PrintOption("1", "Random (valore casuale)");
            PrintOption("2", "Default (100)");
            PrintOption("3", "Personalizzato");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════╣");
            Console.Write("║ Dimensione della popolazione iniziale: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("                              ║");
            Console.ResetColor();
            PrintInputPrompt();
            config.PopulationSize = GetIntParameter(
                "Dimensione della popolazione iniziale",
                10, 1000000000, 100, random);

            // EliteSize
            PrintOption("1", "Random (valore casuale)");
            PrintOption("2", "Default (10)");
            PrintOption("3", "Personalizzato");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════╣");
            Console.Write("║ Numero di elite da selezionare per generazione: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("                    ║");
            Console.ResetColor();
            PrintInputPrompt();
            config.EliteSize = GetIntParameter(
                "Numero di elite da selezionare per generazione",
                1, 100, 10, random);

            // Generations
            PrintOption("1", "Random (valore casuale)");
            PrintOption("2", "Default (50)");
            PrintOption("3", "Personalizzato");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════╣");
            Console.Write("║ Numero massimo di generazioni: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("                              ║");
            Console.ResetColor();
            PrintInputPrompt();
            config.Generations = GetIntParameter(
                "Numero massimo di generazioni",
                1, 1000000000, 50, random);

            // MutationRate
            PrintOption("1", "Random (valore casuale)");
            PrintOption("2", "Default (0.15)");
            PrintOption("3", "Personalizzato");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════╣");
            Console.Write("║ Tasso di mutazione (0.0 - 1.0): ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("                              ║");
            Console.ResetColor();
            PrintInputPrompt();
            config.MutationRate = GetFloatParameter(
                "Tasso di mutazione (0.0 - 1.0)",
                0.0f, 1.0f, 0.15f, random);

            // RulesetBreakChance
            PrintOption("1", "Random (valore casuale)");
            PrintOption("2", "Default (0.01)");
            PrintOption("3", "Personalizzato");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════╣");
            Console.Write("║ Probabilità di ruleset breaking (0.0 - 1.0): ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("                        ║");
            Console.ResetColor();
            PrintInputPrompt();
            config.RulesetBreakChance = GetFloatParameter(
                "Probabilità di ruleset breaking (0.0 - 1.0)",
                0.0f, 0.1f, 0.01f, random);

            // FinalBossCount
            PrintOption("1", "Random (valore casuale)");
            PrintOption("2", "Default (5)");
            PrintOption("3", "Personalizzato");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════╣");
            Console.Write("║ Numero di boss finali da selezionare: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("                              ║");
            Console.ResetColor();
            PrintInputPrompt();
            config.FinalBossCount = GetIntParameter(
                "Numero di boss finali da selezionare",
                1, 50, 5, random);

            // BattleEvaluation
            PrintOption("1", "Random");
            PrintOption("2", "Default (true)");
            PrintOption("3", "Personalizzato");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════╣");
            Console.Write("║ Valutazione tramite battaglie: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("                              ║");
            Console.ResetColor();
            PrintInputPrompt();
            config.BattleEvaluation = GetBoolParameter(
                "Valutazione tramite battaglie",
                true, random);

            // BattleCount
            if (config.BattleEvaluation)
            {
                PrintOption("1", "Random (valore casuale)");
                PrintOption("2", "Default (5)");
                PrintOption("3", "Personalizzato");
                Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════╣");
                Console.Write("║ Numero di battaglie per valutazione: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("                              ║");
                Console.ResetColor();
                PrintInputPrompt();
                config.BattleCount = GetIntParameter(
                    "Numero di battaglie per valutazione",
                    1, 100, 5, random);
            }
            else
            {
                config.BattleCount = 0;
            }

            // BattleWeight
            if (config.BattleEvaluation)
            {
                PrintOption("1", "Random (valore casuale)");
                PrintOption("2", "Default (0.7)");
                PrintOption("3", "Personalizzato");
                Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════╣");
                Console.Write("║ Peso della valutazione tramite battaglie (0.0 - 1.0): ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("                ║");
                Console.ResetColor();
                PrintInputPrompt();
                config.BattleWeight = GetFloatParameter(
                    "Peso della valutazione tramite battaglie (0.0 - 1.0)",
                    0.0f, 1.0f, 0.7f, random);
            }
            else
            {
                config.BattleWeight = 0.0f;
            }

            // AttributeLimits
            PrintSectionTitle("LIMITI DEGLI ATTRIBUTI");
            config.AttributeLimits = GetDictionaryParameter(
                "Limiti degli attributi",
                GetDefaultAttributeLimits(),
                random);

            // TargetAttributes
            PrintSectionTitle("TARGET DEGLI ATTRIBUTI");
            config.TargetAttributes = GetTargetAttributesParameter(random);

            // TargetBehaviors
            PrintSectionTitle("TARGET DEI COMPORTAMENTI");
            config.TargetBehaviors = GetTargetBehaviorsParameter(random);

            return config;
        }

        private static int GetIntParameter(string paramName, int min, int max, int defaultValue, Random random)
        {
            Console.Write("║ Scelta: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    // Assicurati che min <= max
                    int actualMin = Math.Min(min, max);
                    int actualMax = Math.Max(min, max);
                    int randomValue = random.Next(actualMin, actualMax + 1);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"║ → Valore casuale generato: {randomValue}");
                    Console.ResetColor();
                    return randomValue;

                case "2":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"║ → Valore default: {defaultValue}");
                    Console.ResetColor();
                    return defaultValue;

                case "3":
                    return GetIntInput(paramName, min, max);

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("║ → Scelta non valida, uso il default");
                    Console.ResetColor();
                    return defaultValue;
            }
        }

        private static float GetFloatParameter(string paramName, float min, float max, float defaultValue, Random random)
        {
            Console.Write("║ Scelta: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    // Assicurati che min <= max
                    float actualMin = Math.Min(min, max);
                    float actualMax = Math.Max(min, max);
                    float randomValue = (float)(random.NextDouble() * (actualMax - actualMin) + actualMin);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"║ → Valore casuale generato: {randomValue:F2}");
                    Console.ResetColor();
                    return randomValue;

                case "2":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"║ → Valore default: {defaultValue:F2}");
                    Console.ResetColor();
                    return defaultValue;

                case "3":
                    return GetFloatInput(paramName, min, max);

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("║ → Scelta non valida, uso il default");
                    Console.ResetColor();
                    return defaultValue;
            }
        }

        private static bool GetBoolParameter(string paramName, bool defaultValue, Random random)
        {
            Console.Write("║ Scelta: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    bool randomValue = random.Next(2) == 1;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"║ → Valore casuale generato: {randomValue}");
                    Console.ResetColor();
                    return randomValue;

                case "2":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"║ → Valore default: {defaultValue}");
                    Console.ResetColor();
                    return defaultValue;

                case "3":
                    return GetBoolInput(paramName);

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("║ → Scelta non valida, uso il default");
                    Console.ResetColor();
                    return defaultValue;
            }
        }

        private static Dictionary<string, (int Min, int Max)> GetDictionaryParameter(
            string paramName,
            Dictionary<string, (int Min, int Max)> defaultDict,
            Random random)
        {
            Console.WriteLine("║ 1. Random per tutti gli attributi");
            Console.WriteLine("║ 2. Default per tutti gli attributi");
            Console.WriteLine("║ 3. Personalizzato per ogni attributo");
            Console.Write("║ Scelta: ");
            string choice = Console.ReadLine();

            var result = new Dictionary<string, (int Min, int Max)>();

            switch (choice)
            {
                case "1":
                    foreach (var kvp in defaultDict)
                    {
                        // Assicurati che i valori siano validi
                        int minVal = Math.Min(kvp.Value.Min, kvp.Value.Max);
                        int maxVal = Math.Max(kvp.Value.Min, kvp.Value.Max);
                        int randomMin = random.Next(minVal / 2, maxVal * 2);
                        int randomMax = random.Next(minVal, maxVal * 3);

                        // Assicurati che randomMin <= randomMax
                        if (randomMin > randomMax)
                        {
                            int temp = randomMin;
                            randomMin = randomMax;
                            randomMax = temp;
                        }

                        result[kvp.Key] = (randomMin, randomMax);
                    }
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("║ → Valori casuali generati per tutti gli attributi");
                    Console.ResetColor();
                    break;

                case "2":
                    result = new Dictionary<string, (int Min, int Max)>(defaultDict);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("║ → Valori default applicati a tutti gli attributi");
                    Console.ResetColor();
                    break;

                case "3":
                    foreach (var kvp in defaultDict)
                    {
                        Console.WriteLine($"\n║ Attributo: {kvp.Key}");
                        Console.WriteLine("║ 1. Random");
                        Console.WriteLine($"║ 2. Default ({kvp.Value.Min}-{kvp.Value.Max})");
                        Console.WriteLine("║ 3. Personalizzato");
                        Console.Write("║ Scelta: ");
                        string attrChoice = Console.ReadLine();

                        switch (attrChoice)
                        {
                            case "1":
                                int minVal = Math.Min(kvp.Value.Min, kvp.Value.Max);
                                int maxVal = Math.Max(kvp.Value.Min, kvp.Value.Max);
                                int randomMin = random.Next(minVal / 2, maxVal * 2);
                                int randomMax = random.Next(minVal, maxVal * 3);

                                // Assicurati che randomMin <= randomMax
                                if (randomMin > randomMax)
                                {
                                    int temp = randomMin;
                                    randomMin = randomMax;
                                    randomMax = temp;
                                }

                                result[kvp.Key] = (randomMin, randomMax);
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"║ → Valori casuali generati per {kvp.Key}");
                                Console.ResetColor();
                                break;

                            case "2":
                                result[kvp.Key] = kvp.Value;
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"║ → Valori default applicati a {kvp.Key}");
                                Console.ResetColor();
                                break;

                            case "3":
                                int min = GetIntInput($"║ Valore minimo per {kvp.Key}", 1, 1000);
                                int max = GetIntInput($"║ Valore massimo per {kvp.Key}", min, 2000);
                                result[kvp.Key] = (min, max);
                                break;

                            default:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"║ → Scelta non valida, uso il default per {kvp.Key}");
                                Console.ResetColor();
                                result[kvp.Key] = kvp.Value;
                                break;
                        }
                    }
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("║ → Scelta non valida, uso il default per tutti gli attributi");
                    Console.ResetColor();
                    result = new Dictionary<string, (int Min, int Max)>(defaultDict);
                    break;
            }

            return result;
        }

        private static Dictionary<string, int> GetTargetAttributesParameter(Random random)
        {
            Console.WriteLine("║ 1. Random (valori casuali)");
            Console.WriteLine("║ 2. Nessun target (evoluzione libera)");
            Console.WriteLine("║ 3. Personalizzato");
            Console.Write("║ Scelta: ");
            string choice = Console.ReadLine();

            var result = new Dictionary<string, int>();

            switch (choice)
            {
                case "1":
                    var attributes = new[] { "Hp", "Mana", "Stamina", "Strength", "Intelligence", "Defence", "Speed", "Wisdom", "TrueDefence", "MaxEquipLoad" };
                    foreach (var attr in attributes)
                    {
                        result[attr] = random.Next(20, 100);
                    }
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("║ → Target casuali generati per tutti gli attributi");
                    Console.ResetColor();
                    break;

                case "2":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("║ → Nessun target impostato (evoluzione libera)");
                    Console.ResetColor();
                    return null;

                case "3":
                    Console.WriteLine("║ Inserisci i valori target (lascia vuoto per saltare):");
                    attributes = new[] { "Hp", "Mana", "Stamina", "Strength", "Intelligence", "Defence", "Speed", "Wisdom", "TrueDefence", "MaxEquipLoad" };
                    foreach (var attr in attributes)
                    {
                        Console.Write($"║ {attr}: ");
                        string input = Console.ReadLine();
                        if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int value))
                        {
                            result[attr] = value;
                        }
                    }
                    if (result.Count > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("║ → Target personalizzati impostati");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("║ → Nessun target impostato (evoluzione libera)");
                        Console.ResetColor();
                        return null;
                    }
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("║ → Scelta non valida, nessun target impostato");
                    Console.ResetColor();
                    return null;
            }

            return result.Count > 0 ? result : null;
        }

        private static Dictionary<string, float> GetTargetBehaviorsParameter(Random random)
        {
            Console.WriteLine("║ 1. Random (valori casuali)");
            Console.WriteLine("║ 2. Nessun target (evoluzione libera)");
            Console.WriteLine("║ 3. Personalizzato");
            Console.Write("║ Scelta: ");
            string choice = Console.ReadLine();

            var result = new Dictionary<string, float>();

            switch (choice)
            {
                case "1":
                    var behaviors = new[] { "Aggressiveness", "RiskTaking", "Adaptiveness", "LearningFactor", "Pacing", "BurstUsage" };
                    foreach (var behavior in behaviors)
                    {
                        result[behavior] = (float)random.NextDouble();
                    }
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("║ → Target casuali generati per tutti i comportamenti");
                    Console.ResetColor();
                    break;

                case "2":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("║ → Nessun target impostato (evoluzione libera)");
                    Console.ResetColor();
                    return null;

                case "3":
                    Console.WriteLine("║ Inserisci i valori target (0.0 - 1.0, lascia vuoto per saltare):");
                    behaviors = new[] { "Aggressiveness", "RiskTaking", "Adaptiveness", "LearningFactor", "Pacing", "BurstUsage" };
                    foreach (var behavior in behaviors)
                    {
                        Console.Write($"║ {behavior}: ");
                        string input = Console.ReadLine();
                        if (!string.IsNullOrEmpty(input) && float.TryParse(input, out float value))
                        {
                            result[behavior] = Math.Max(0f, Math.Min(1f, value));
                        }
                    }
                    if (result.Count > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("║ → Target personalizzati impostati");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("║ → Nessun target impostato (evoluzione libera)");
                        Console.ResetColor();
                        return null;
                    }
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("║ → Scelta non valida, nessun target impostato");
                    Console.ResetColor();
                    return null;
            }

            return result.Count > 0 ? result : null;
        }

        // Metodi di input helper
        private static int GetIntInput(string paramName, int min, int max)
        {
            while (true)
            {
                Console.Write($"║ {paramName} ({min}-{max}): ");
                string input = Console.ReadLine();
                if (int.TryParse(input, out int value))
                {
                    if (min <= max)
                    {
                        if (value >= min && value <= max)
                        {
                            return value;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("║ → Valore fuori range, riprova");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        // Se min > max, accetta qualsiasi valore
                        return value;
                    }
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("║ → Valore non valido, riprova");
                Console.ResetColor();
            }
        }

        private static float GetFloatInput(string paramName, float min, float max)
        {
            while (true)
            {
                Console.Write($"║ {paramName} ({min:F2}-{max:F2}): ");
                string input = Console.ReadLine();
                if (float.TryParse(input, out float value))
                {
                    if (min <= max)
                    {
                        if (value >= min && value <= max)
                        {
                            return value;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("║ → Valore fuori range, riprova");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        // Se min > max, accetta qualsiasi valore
                        return value;
                    }
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("║ → Valore non valido, riprova");
                Console.ResetColor();
            }
        }

        private static bool GetBoolInput(string paramName)
        {
            while (true)
            {
                Console.Write($"║ {paramName} (true/false): ");
                string input = Console.ReadLine();
                if (bool.TryParse(input, out bool value))
                {
                    return value;
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("║ → Valore non valido, inserisci true o false");
                Console.ResetColor();
            }
        }

        private static Dictionary<string, (int Min, int Max)> GetDefaultAttributeLimits()
        {
            return new Dictionary<string, (int Min, int Max)>
            {
                ["Hp"] = (200, 1000),
                ["Mana"] = (50, 1000),
                ["Stamina"] = (50, 1000),
                ["Strength"] = (20, 100),
                ["Intelligence"] = (20, 100),
                ["Defence"] = (20, 100),
                ["Speed"] = (20, 100),
                ["Wisdom"] = (20, 100),
                ["TrueDefence"] = (5, 30),
                ["MaxEquipLoad"] = (150, 1000),
                ["ArsenalSize"] = (1, 10)
            };
        }

        private static void DisplayConfiguration(TrainingConfig config)
        {
            PrintSectionTitle("CONFIGURAZIONE SCELTA");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Parametri di Addestramento:");
            Console.ResetColor();
            Console.WriteLine($"║ • Dimensione popolazione: {config.PopulationSize}");
            Console.WriteLine($"║ • Elite per generazione: {config.EliteSize}");
            Console.WriteLine($"║ • Numero generazioni: {config.Generations}");
            Console.WriteLine($"║ • Tasso mutazione: {config.MutationRate:F2}");
            Console.WriteLine($"║ • Probabilità ruleset breaking: {config.RulesetBreakChance:F2}");
            Console.WriteLine($"║ • Boss finali: {config.FinalBossCount}");
            Console.WriteLine($"║ • Valutazione battaglie: {config.BattleEvaluation}");

            if (config.BattleEvaluation)
            {
                Console.WriteLine($"║ • Battaglie per valutazione: {config.BattleCount}");
                Console.WriteLine($"║ • Peso valutazione battaglie: {config.BattleWeight:F2}");
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nLimiti Attributi:");
            Console.ResetColor();
            foreach (var kvp in config.AttributeLimits)
            {
                Console.WriteLine($"║ • {kvp.Key}: {kvp.Value.Min}-{kvp.Value.Max}");
            }

            if (config.TargetAttributes != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\nTarget Attributi:");
                Console.ResetColor();
                foreach (var kvp in config.TargetAttributes)
                {
                    Console.WriteLine($"║ • {kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nNessun target per gli attributi (evoluzione libera)");
                Console.ResetColor();
            }

            if (config.TargetBehaviors != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\nTarget Comportamenti:");
                Console.ResetColor();
                foreach (var kvp in config.TargetBehaviors)
                {
                    Console.WriteLine($"║ • {kvp.Key}: {kvp.Value:F2}");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nNessun target per i comportamenti (evoluzione libera)");
                Console.ResetColor();
            }
        }

        private static void DisplayResults(TrainingResult result)
        {
            PrintSectionTitle("RISULTATI DELL'ADDESTRAMENTO");

            // Mostra statistiche finali
            var finalStats = result.GenerationStats.Last();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Statistiche Finali:");
            Console.ResetColor();
            Console.WriteLine($"║ • Fitness medio: {finalStats.AverageFitness:F2}");
            Console.WriteLine($"║ • Fitness massimo: {finalStats.MaxFitness:F2}");
            Console.WriteLine($"║ • HP medio: {finalStats.AverageHp:F0}");
            Console.WriteLine($"║ • Forza media: {finalStats.AverageStrength:F0}");
            Console.WriteLine($"║ • Difesa media: {finalStats.AverageDefense:F0}");
            Console.WriteLine($"║ • Velocità media: {finalStats.AverageSpeed:F0}");

            // Mostra i migliori boss
            PrintSectionTitle($"MIGLIORI {result.FinalBosses.Count} BOSS");

            for (int i = 0; i < result.FinalBosses.Count; i++)
            {
                var boss = result.FinalBosses[i];
                var fitness = new TrainRuler().CalculateFitness(boss);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Boss #{i + 1}: {boss.ID}");
                Console.ResetColor();
                Console.WriteLine($"║ • Classe: {boss.BossClass.ClassName}");
                Console.WriteLine($"║ • Fitness: {fitness:F2}");
                Console.WriteLine($"║ • Statistiche: HP:{boss.Hp} STR:{boss.Strength} INT:{boss.Intelligence} DEF:{boss.Defence} SPD:{boss.Speed} WIS:{boss.Wisdom}");
                Console.WriteLine($"║ • Armi equipaggiate: {boss.CurrentWeapons.Count}");

                foreach (var weapon in boss.CurrentWeapons.Where(w => w != null))
                {
                    Console.WriteLine($"║   - {weapon.Name} ({weapon.WeaponType}) - Danno: {weapon.BaseDamage}");
                }

                Console.WriteLine($"║ • Abilità: {boss.CurrentSkills.Count}");
                foreach (var skill in boss.CurrentSkills.Where(s => s != null))
                {
                    Console.WriteLine($"║   - {skill.Id} - Potenza: {skill.BasePower}");
                }

                if (i < result.FinalBosses.Count - 1)
                {
                    Console.WriteLine("║");
                }
            }
        }

        private static void TestFinalBosses(List<Boss> bosses)
        {
            PrintSectionTitle("TEST DI BATTAGLIA TRA I BOSS FINALI");

            var battleSystem = new BattleSystem();

            for (int i = 0; i < bosses.Count; i++)
            {
                for (int j = i + 1; j < bosses.Count; j++)
                {
                    var boss1 = bosses[i];
                    var boss2 = bosses[j];

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Battaglia: {boss1.ID} vs {boss2.ID}");
                    Console.ResetColor();

                    var result = battleSystem.SimulateBattle(boss1, boss2);

                    if (result.Winner != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"║ → Vincitore: {result.Winner.ID}");
                        Console.ResetColor();
                        Console.WriteLine($"║ • Danni inflitti: {result.DamageDealt:F0}");
                        Console.WriteLine($"║ • Danni subiti: {result.DamageTaken:F0}");
                        Console.WriteLine($"║ • Tempo di sopravvivenza: {result.SurvivalTime:F0} turni");
                        Console.WriteLine($"║ • Efficienza risorse: {result.ResourceEfficiency:P0}");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("║ → Errore: Nessun vincitore determinato");
                        Console.ResetColor();
                    }

                    if (j < bosses.Count - 1 || i < bosses.Count - 2)
                    {
                        Console.WriteLine("║");
                    }
                }
            }
        }
    }
}