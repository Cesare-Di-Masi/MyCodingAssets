namespace BossGeneratorLib
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace BossGeneratorLib
    {
        // Classe per le regole iniziali - ora separata per essere accessibile ovunque
        public class StarterRules
        {
            public int InitialPopulationSize { get; set; } = 100;
            public int EliteSize { get; set; } = 10;
            public int Generations { get; set; } = 50;
            public double MutationRate { get; set; } = 0.15;
            public double RulesetBreakChance { get; set; } = 0.01;

            public Dictionary<string, (int Min, int Max)> AttributeLimits { get; set; } = new()
            {
                ["Hp"] = (100, 1000),
                ["Mana"] = (50, 500),
                ["Stamina"] = (50, 500),
                ["Strength"] = (10, 100),
                ["Intelligence"] = (10, 100),
                ["Defence"] = (10, 100),
                ["Speed"] = (10, 100),
                ["Wisdom"] = (10, 100),
                ["TrueDefence"] = (5, 50),
                ["MaxEquipLoad"] = (20, 200),
                ["ArsenalSize"] = (1, 5)
            };

            public Dictionary<string, (float Min, float Max)> AiPatternLimits { get; set; } = new();

            public Dictionary<string, double> MutationRates { get; set; } = new()
            {
                ["Attributes"] = 0.1,
                ["AiPattern"] = 0.2,
                ["Weapons"] = 0.15,
                ["Skills"] = 0.15,
                ["StatusEffects"] = 0.05
            };

            // Limiti per la generazione di armi
            public Dictionary<string, (int Min, int Max)> WeaponLimits { get; set; } = new()
            {
                ["BaseDamage"] = (10, 100),
                ["Range"] = (1, 10),
                ["Weight"] = (1, 20),
                ["UpgradeSlots"] = (0, 5)
            };

            // Limiti per la generazione di abilità
            public Dictionary<string, (int Min, int Max)> SkillLimits { get; set; } = new()
            {
                ["BasePower"] = (10, 100),
                ["ManaCost"] = (0, 100),
                ["StaminaCost"] = (0, 100),
                ["Cooldown"] = (1, 30)
            };
        }

        // Classe per le regole finali
        public class EnderRules
        {
            public Dictionary<string, int> TargetAttributes { get; set; } = new();
            public Dictionary<string, float> TargetBehaviors { get; set; } = new();

            public Dictionary<string, double> TargetWeights { get; set; } = new()
            {
                ["Attributes"] = 0.4,
                ["Behaviors"] = 0.6
            };

            public bool IsEmpty => TargetAttributes.Count == 0 && TargetBehaviors.Count == 0;
        }

        // Estensione per Random per generare numeri gaussiani
        public static class RandomExtensions
        {
            public static double NextGaussian(this Random random, double mean = 0, double stdDev = 1)
            {
                double u1 = 1.0 - random.NextDouble();
                double u2 = 1.0 - random.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                return mean + stdDev * randStdNormal;
            }
        }

        // AssetsGenerator come classe statica
        public static class AssetsGenerator
        {
            #region Weapon Generation

            public static Weapon GenerateWeapon(StarterRules starterRules, Random random, List<StatusEffect> availableStatusEffects, Boss boss = null)
            {
                string[] weaponTypes = { "Sword", "Axe", "Mace", "Dagger", "Bow", "Staff", "Wand", "Spear", "Hammer" };
                string weaponType = weaponTypes[random.Next(weaponTypes.Length)];

                var damageLimits = starterRules.WeaponLimits["BaseDamage"];
                var rangeLimits = starterRules.WeaponLimits["Range"];
                var weightLimits = starterRules.WeaponLimits["Weight"];
                var slotLimits = starterRules.WeaponLimits["UpgradeSlots"];

                // Generazione requisiti basati sulle statistiche del boss (se fornito)
                int reqStr = boss != null ? (int)(boss.Strength * (0.3 + random.NextDouble() * 0.4)) : random.Next(5, 50);
                int reqInt = boss != null ? (int)(boss.Intelligence * (0.3 + random.NextDouble() * 0.4)) : random.Next(5, 50);
                int reqDef = boss != null ? (int)(boss.Defence * (0.3 + random.NextDouble() * 0.4)) : random.Next(5, 50);
                int reqSpd = boss != null ? (int)(boss.Speed * (0.3 + random.NextDouble() * 0.4)) : random.Next(5, 50);

                // Generazione statistiche arma
                int baseDmg = random.Next(damageLimits.Min, damageLimits.Max + 1);
                float dmgScaling = (float)(0.5 + random.NextDouble() * 1.5); // 0.5 - 2.0
                float atkSpeed = (float)(0.5 + random.NextDouble() * 1.5); // 0.5 - 2.0
                int range = random.Next(rangeLimits.Min, rangeLimits.Max + 1);
                int weight = random.Next(weightLimits.Min, weightLimits.Max + 1);
                float critChance = (float)(random.NextDouble() * 0.3); // 0 - 0.3
                float critMul = (float)(1.5 + random.NextDouble() * 1.0); // 1.5 - 2.5
                int upgradeSlots = random.Next(slotLimits.Min, slotLimits.Max + 1);

                // Generazione abilità arma
                var weaponSkill = GenerateSkill(starterRules, random, availableStatusEffects);

                // Generazione effetti di stato
                var statusEffects = GenerateRandomStatusEffects(availableStatusEffects, random, 1, 3);

                return new Weapon(
                    $"Random_{weaponType}_{random.Next(1000)}",
                    weaponType,
                    reqStr, reqInt, reqDef, reqSpd,
                    baseDmg, (int)dmgScaling, (int)atkSpeed,
                    range, weight, critChance, critMul,
                    weaponSkill, statusEffects, upgradeSlots);
            }

            public static Weapon GenerateWeapon(StarterRules starterRules, Random random, List<StatusEffect> availableStatusEffects, Weapon previous)
            {
                // Crea una copia dell'arma precedente
                var newWeapon = new Weapon(previous);

                // Modifica alcune proprietà in modo casuale
                if (random.NextDouble() < 0.7) // 70% chance di modificare il danno base
                {
                    var limits = starterRules.WeaponLimits["BaseDamage"];
                    var mutation = (int)(random.NextGaussian() * (limits.Max - limits.Min) * 0.1);
                    newWeapon.BaseDamage = Math.Max(limits.Min, Math.Min(limits.Max, newWeapon.BaseDamage + mutation));
                }

                if (random.NextDouble() < 0.5) // 50% chance di modificare la velocità di attacco
                {
                    newWeapon.AttackSpeed *= (float)(0.9 + random.NextDouble() * 0.2); // ±10%
                }

                if (random.NextDouble() < 0.4) // 40% chance di modificare la chance di critico
                {
                    newWeapon.CritChance = Math.Max(0, Math.Min(1, newWeapon.CritChance + (float)(random.NextGaussian() * 0.05)));
                }

                if (random.NextDouble() < 0.4) // 40% chance di modificare il moltiplicatore di critico
                {
                    newWeapon.CritMultiplier *= (float)(0.95 + random.NextDouble() * 0.1); // ±5%
                }

                if (random.NextDouble() < 0.3) // 30% chance di modificare il peso
                {
                    var limits = starterRules.WeaponLimits["Weight"];
                    var mutation = (int)(random.NextGaussian() * (limits.Max - limits.Min) * 0.1);
                    newWeapon.Weight = Math.Max(limits.Min, Math.Min(limits.Max, newWeapon.Weight + mutation));
                }

                // Possibilità di aggiungere/rimuovere effetti di stato
                if (random.NextDouble() < 0.2 && newWeapon.StatusEffectsDmg.Count > 0)
                {
                    newWeapon.StatusEffectsDmg.RemoveAt(random.Next(newWeapon.StatusEffectsDmg.Count));
                }
                else if (random.NextDouble() < 0.2 && availableStatusEffects.Count > 0)
                {
                    newWeapon.StatusEffectsDmg.Add(availableStatusEffects[random.Next(availableStatusEffects.Count)]);
                }

                return newWeapon;
            }

            #endregion Weapon Generation

            #region Skill Generation

            public static Skill GenerateSkill(StarterRules starterRules, Random random, List<StatusEffect> availableStatusEffects, Boss boss = null)
            {
                string[] skillTypes = { "Fireball", "IceSpike", "LightningBolt", "Heal", "Shield", "Berserk", "Stealth", "Teleport" };
                string skillType = skillTypes[random.Next(skillTypes.Length)];

                var powerLimits = starterRules.SkillLimits["BasePower"];
                var manaLimits = starterRules.SkillLimits["ManaCost"];
                var staminaLimits = starterRules.SkillLimits["StaminaCost"];
                var cooldownLimits = starterRules.SkillLimits["Cooldown"];

                int basePower = random.Next(powerLimits.Min, powerLimits.Max + 1);
                float scaling = (float)(0.5 + random.NextDouble() * 1.5); // 0.5 - 2.0
                int manaCost = random.Next(manaLimits.Min, manaLimits.Max + 1);
                int staminaCost = random.Next(staminaLimits.Min, staminaLimits.Max + 1);
                int cooldown = random.Next(cooldownLimits.Min, cooldownLimits.Max + 1);

                float? duration = random.NextDouble() < 0.5 ? null : (float?)random.Next(1, 10);
                float? aoe = random.NextDouble() < 0.5 ? null : (float?)random.Next(1, 10);
                float critChance = (float)(random.NextDouble() * 0.3);
                float critMul = (float)(1.5 + random.NextDouble() * 1.0);

                var statusEffects = GenerateRandomStatusEffects(availableStatusEffects, random, 0, 2);

                return new Skill(
                    $"Random_{skillType}_{random.Next(1000)}",
                    basePower, scaling, manaCost, staminaCost, cooldown,
                    duration, aoe, critChance, critMul, statusEffects);
            }

            public static Skill GenerateSkill(StarterRules starterRules, Random random, List<StatusEffect> availableStatusEffects, Skill previous)
            {
                // Crea una copia dell'abilità precedente
                var newSkill = new Skill(previous);

                // Modifica alcune proprietà in modo casuale
                if (random.NextDouble() < 0.7) // 70% chance di modificare la potenza base
                {
                    var limits = starterRules.SkillLimits["BasePower"];
                    var mutation = (int)(random.NextGaussian() * (limits.Max - limits.Min) * 0.1);
                    newSkill.BasePower = Math.Max(limits.Min, Math.Min(limits.Max, newSkill.BasePower + mutation));
                }

                if (random.NextDouble() < 0.5) // 50% chance di modificare lo scaling
                {
                    newSkill.Scaling *= (float)(0.9 + random.NextDouble() * 0.2); // ±10%
                }

                if (random.NextDouble() < 0.4) // 40% chance di modificare il costo mana
                {
                    var limits = starterRules.SkillLimits["ManaCost"];
                    var mutation = (int)(random.NextGaussian() * limits.Max * 0.1);
                    newSkill.ManaCost = Math.Max(limits.Min, Math.Min(limits.Max, newSkill.ManaCost + mutation));
                }

                if (random.NextDouble() < 0.4) // 40% chance di modificare il cooldown
                {
                    var limits = starterRules.SkillLimits["Cooldown"];
                    var mutation = (int)(random.NextGaussian() * limits.Max * 0.1);
                    newSkill.Cooldown = Math.Max(limits.Min, Math.Min(limits.Max, newSkill.Cooldown + mutation));
                }

                if (random.NextDouble() < 0.3) // 30% chance di modificare la durata
                {
                    if (newSkill.Duration.HasValue)
                    {
                        newSkill.Duration = Math.Max(1, Math.Min(30, newSkill.Duration.Value + (float)(random.NextGaussian() * 2)));
                    }
                    else if (random.NextDouble() < 0.5)
                    {
                        newSkill.Duration = (float?)random.Next(1, 10);
                    }
                }

                // Possibilità di aggiungere/rimuovere effetti di stato
                if (random.NextDouble() < 0.2 && newSkill.StatusEffects.Count > 0)
                {
                    newSkill.StatusEffects.RemoveAt(random.Next(newSkill.StatusEffects.Count));
                }
                else if (random.NextDouble() < 0.2 && availableStatusEffects.Count > 0)
                {
                    newSkill.StatusEffects.Add(availableStatusEffects[random.Next(availableStatusEffects.Count)]);
                }

                return newSkill;
            }

            #endregion Skill Generation

            #region Boss Generation

            public static Boss GenerateBoss(StarterRules starterRules, Random random, List<StatusEffect> availableStatusEffects)
            {
                string[] bossClasses = { "Warrior", "Mage", "Rogue", "Paladin", "Necromancer", "Beastmaster" };
                string bossClass = bossClasses[random.Next(bossClasses.Length)];

                var boss = new Boss(
                    $"Boss_{random.Next(1000)}", true,
                    GetRandomValue(starterRules, random, "Hp"),
                    GetRandomValue(starterRules, random, "Mana"),
                    GetRandomValue(starterRules, random, "Stamina"),
                    GetRandomValue(starterRules, random, "Strength"),
                    GetRandomValue(starterRules, random, "Intelligence"),
                    GetRandomValue(starterRules, random, "Defence"),
                    GetRandomValue(starterRules, random, "Speed"),
                    GetRandomValue(starterRules, random, "Wisdom"),
                    GetRandomValue(starterRules, random, "TrueDefence"),
                    GetRandomValue(starterRules, random, "MaxEquipLoad"), 0,
                    GetRandomValue(starterRules, random, "ArsenalSize"), 0,
                    new Dictionary<string, float?>(), new List<string?>(),
                    new List<string?>(), GenerateRandomAiPattern(starterRules, random),
                    new BossClass(), new List<Weapon?>(), new List<Skill?>());

                // Generazione armi casuali
                int numWeapons = random.Next(1, boss.ArsenalSize + 1);
                for (int i = 0; i < numWeapons; i++)
                {
                    var weapon = GenerateWeapon(starterRules, random, availableStatusEffects, boss);
                    try
                    {
                        boss.AddWeapon(weapon);
                    }
                    catch (InvalidOperationException)
                    {
                        // Se non può essere aggiunta, la ignoriamo
                    }
                }

                // Generazione abilità casuali
                int numSkills = random.Next(1, 5);
                for (int i = 0; i < numSkills; i++)
                {
                    var skill = GenerateSkill(starterRules, random, availableStatusEffects, boss);
                    boss.AddSkill(skill);
                }

                return boss;
            }

            public static Boss GenerateBoss(StarterRules starterRules, Random random, List<StatusEffect> availableStatusEffects, Boss previous)
            {
                // Crea una copia del boss precedente
                var newBoss = new Boss(previous);

                // Modifica alcuni attributi in modo casuale
                var attributes = new[] { "Hp", "Mana", "Stamina", "Strength",
                "Intelligence", "Defence", "Speed", "Wisdom", "TrueDefence" };

                foreach (var attr in attributes)
                {
                    if (random.NextDouble() < 0.3) // 30% chance per attributo
                    {
                        var currentValue = (int)typeof(Boss).GetProperty(attr).GetValue(newBoss);
                        var limits = starterRules.AttributeLimits[attr];

                        var mutation = (int)(random.NextGaussian() * (limits.Max - limits.Min) * 0.1);
                        var newValue = Math.Max(limits.Min, Math.Min(limits.Max, currentValue + mutation));

                        typeof(Boss).GetProperty(attr).SetValue(newBoss, newValue);
                    }
                }

                // Modifica il pattern di IA
                foreach (var key in newBoss.AiPattern.Keys.ToList())
                {
                    if (random.NextDouble() < 0.2) // 20% chance per comportamento
                    {
                        if (starterRules.AiPatternLimits.ContainsKey(key))
                        {
                            var limits = starterRules.AiPatternLimits[key];
                            var mutation = (float)(random.NextGaussian() * (limits.Max - limits.Min) * 0.1);
                            var newValue = Math.Max(limits.Min, Math.Min(limits.Max, newBoss.AiPattern[key] + mutation));
                            newBoss.AiPattern[key] = newValue;
                        }
                    }
                }

                // Modifica le armi
                for (int i = 0; i < newBoss.CurrentWeapons.Count; i++)
                {
                    if (newBoss.CurrentWeapons[i] != null && random.NextDouble() < 0.5)
                    {
                        newBoss.CurrentWeapons[i] = GenerateWeapon(starterRules, random, availableStatusEffects, newBoss.CurrentWeapons[i]);
                    }
                }

                // Modifica le abilità
                for (int i = 0; i < newBoss.CurrentSkills.Count; i++)
                {
                    if (newBoss.CurrentSkills[i] != null && random.NextDouble() < 0.5)
                    {
                        newBoss.CurrentSkills[i] = GenerateSkill(starterRules, random, availableStatusEffects, newBoss.CurrentSkills[i]);
                    }
                }

                // Possibilità di aggiungere/rimuovere armi
                if (random.NextDouble() < 0.2 && newBoss.CurrentWeapons.Count > 0)
                {
                    int index = random.Next(newBoss.CurrentWeapons.Count);
                    newBoss.RemoveWeapon(newBoss.CurrentWeapons[index]);
                }
                else if (random.NextDouble() < 0.2 && newBoss.CurrentWeapons.Count < newBoss.ArsenalSize)
                {
                    var weapon = GenerateWeapon(starterRules, random, availableStatusEffects, newBoss);
                    try
                    {
                        newBoss.AddWeapon(weapon);
                    }
                    catch (InvalidOperationException)
                    {
                        // Se non può essere aggiunta, la ignoriamo
                    }
                }

                // Possibilità di aggiungere/rimuovere abilità
                if (random.NextDouble() < 0.2 && newBoss.CurrentSkills.Count > 0)
                {
                    int index = random.Next(newBoss.CurrentSkills.Count);
                    newBoss.RemoveSkill(newBoss.CurrentSkills[index]);
                }
                else if (random.NextDouble() < 0.2)
                {
                    var skill = GenerateSkill(starterRules, random, availableStatusEffects, newBoss);
                    newBoss.AddSkill(skill);
                }

                return newBoss;
            }

            #endregion Boss Generation

            #region Helper Methods

            private static List<StatusEffect> GenerateRandomStatusEffects(List<StatusEffect> availableStatusEffects, Random random, int min, int max)
            {
                var effects = new List<StatusEffect>();
                if (availableStatusEffects.Count == 0) return effects;

                int count = random.Next(min, max + 1);
                for (int i = 0; i < count && i < availableStatusEffects.Count; i++)
                {
                    effects.Add(availableStatusEffects[random.Next(availableStatusEffects.Count)]);
                }
                return effects;
            }

            private static int GetRandomValue(StarterRules starterRules, Random random, string attribute)
            {
                var limits = starterRules.AttributeLimits[attribute];
                return random.Next(limits.Min, limits.Max + 1);
            }

            private static Dictionary<string, float> GenerateRandomAiPattern(StarterRules starterRules, Random random)
            {
                var aiPattern = new Dictionary<string, float>();

                foreach (var key in starterRules.AiPatternLimits.Keys)
                {
                    var limits = starterRules.AiPatternLimits[key];
                    aiPattern[key] = (float)(random.NextDouble() * (limits.Max - limits.Min) + limits.Min);
                }

                return aiPattern;
            }

            #endregion Helper Methods
        }

        public class TrainRuler
        {
            public StarterRules Starter { get; set; } = new StarterRules();
            public EnderRules Ender { get; set; } = new EnderRules();
            public List<StatusEffect> AvailableStatusEffects { get; set; } = new List<StatusEffect>();

            private readonly Random _random = new Random();

            #region Mutator

            public class Mutator
            {
                private readonly StarterRules _starterRules;
                private readonly Random _random;

                public Mutator(StarterRules starterRules, Random random)
                {
                    _starterRules = starterRules;
                    _random = random;
                }

                public void MutateBoss(Boss boss)
                {
                    // Mutazione attributi
                    if (_random.NextDouble() < _starterRules.MutationRates["Attributes"])
                    {
                        MutateAttributes(boss);
                    }

                    // Mutazione AiPattern
                    if (_random.NextDouble() < _starterRules.MutationRates["AiPattern"])
                    {
                        MutateAiPattern(boss);
                    }

                    // Mutazione armi
                    if (_random.NextDouble() < _starterRules.MutationRates["Weapons"])
                    {
                        MutateWeapons(boss);
                    }

                    // Mutazione abilità
                    if (_random.NextDouble() < _starterRules.MutationRates["Skills"])
                    {
                        MutateSkills(boss);
                    }
                }

                private void MutateAttributes(Boss boss)
                {
                    var attributes = new[] { "Hp", "Mana", "Stamina", "Strength",
                    "Intelligence", "Defence", "Speed", "Wisdom", "TrueDefence" };

                    foreach (var attr in attributes)
                    {
                        if (_random.NextDouble() < _starterRules.MutationRate)
                        {
                            var currentValue = (int)typeof(Boss).GetProperty(attr).GetValue(boss);
                            var limits = _starterRules.AttributeLimits[attr];

                            // Mutazione gaussiana
                            var mutation = (int)Math.Round(_random.NextGaussian() * (limits.Max - limits.Min) * 0.1);
                            var newValue = Math.Max(limits.Min, Math.Min(limits.Max, currentValue + mutation));

                            typeof(Boss).GetProperty(attr).SetValue(boss, newValue);
                        }
                    }
                }

                private void MutateAiPattern(Boss boss)
                {
                    foreach (var key in boss.AiPattern.Keys.ToList())
                    {
                        if (_random.NextDouble() < _starterRules.MutationRate)
                        {
                            if (_starterRules.AiPatternLimits.ContainsKey(key))
                            {
                                var limits = _starterRules.AiPatternLimits[key];

                                // Mutazione gaussiana
                                var mutation = (float)_random.NextGaussian() * (limits.Max - limits.Min) * 0.1f;
                                var newValue = Math.Max(limits.Min, Math.Min(limits.Max, boss.AiPattern[key] + mutation));

                                boss.AiPattern[key] = newValue;
                            }
                        }
                    }
                }

                private void MutateWeapons(Boss boss)
                {
                    // Rimozione arma casuale
                    if (_random.NextDouble() < 0.3 && boss.CurrentWeapons.Count > 0)
                    {
                        int index = _random.Next(boss.CurrentWeapons.Count);
                        boss.RemoveWeapon(boss.CurrentWeapons[index]);
                    }

                    // Modifica arma esistente
                    if (_random.NextDouble() < 0.4 && boss.CurrentWeapons.Count > 0)
                    {
                        var weapon = boss.CurrentWeapons[_random.Next(boss.CurrentWeapons.Count)];
                        if (weapon != null)
                        {
                            MutateWeapon(weapon);
                        }
                    }
                }

                private void MutateWeapon(Weapon weapon)
                {
                    // Mutazione danno base
                    if (_random.NextDouble() < 0.3)
                    {
                        var limits = _starterRules.WeaponLimits["BaseDamage"];
                        var mutation = (int)Math.Round(_random.NextGaussian() * (limits.Max - limits.Min) * 0.1);
                        weapon.BaseDamage = Math.Max(limits.Min, Math.Min(limits.Max, weapon.BaseDamage + mutation));
                    }

                    // Mutazione velocità attacco
                    if (_random.NextDouble() < 0.3)
                    {
                        weapon.AttackSpeed *= (float)(0.9 + _random.NextDouble() * 0.2); // ±10%
                    }

                    // Mutazione chance critico
                    if (_random.NextDouble() < 0.2)
                    {
                        weapon.CritChance = Math.Max(0, Math.Min(1, weapon.CritChance + (float)(_random.NextGaussian() * 0.05)));
                    }

                    // Mutazione moltiplicatore critico
                    if (_random.NextDouble() < 0.2)
                    {
                        weapon.CritMultiplier *= (float)(0.95 + _random.NextDouble() * 0.1); // ±5%
                    }
                }

                private void MutateSkills(Boss boss)
                {
                    // Rimozione abilità casuale
                    if (_random.NextDouble() < 0.3 && boss.CurrentSkills.Count > 0)
                    {
                        int index = _random.Next(boss.CurrentSkills.Count);
                        boss.RemoveSkill(boss.CurrentSkills[index]);
                    }

                    // Modifica abilità esistente
                    if (_random.NextDouble() < 0.4 && boss.CurrentSkills.Count > 0)
                    {
                        var skill = boss.CurrentSkills[_random.Next(boss.CurrentSkills.Count)];
                        if (skill != null)
                        {
                            MutateSkill(skill);
                        }
                    }
                }

                private void MutateSkill(Skill skill)
                {
                    // Mutazione potenza base
                    if (_random.NextDouble() < 0.3)
                    {
                        var limits = _starterRules.SkillLimits["BasePower"];
                        var mutation = (int)Math.Round(_random.NextGaussian() * (limits.Max - limits.Min) * 0.1);
                        skill.BasePower = Math.Max(limits.Min, Math.Min(limits.Max, skill.BasePower + mutation));
                    }

                    // Mutazione scaling
                    if (_random.NextDouble() < 0.3)
                    {
                        skill.Scaling *= (float)(0.9 + _random.NextDouble() * 0.2); // ±10%
                    }

                    // Mutazione costo mana
                    if (_random.NextDouble() < 0.2)
                    {
                        var limits = _starterRules.SkillLimits["ManaCost"];
                        var mutation = (int)Math.Round(_random.NextGaussian() * limits.Max * 0.1);
                        skill.ManaCost = Math.Max(limits.Min, Math.Min(limits.Max, skill.ManaCost + mutation));
                    }

                    // Mutazione cooldown
                    if (_random.NextDouble() < 0.2)
                    {
                        var limits = _starterRules.SkillLimits["Cooldown"];
                        var mutation = (int)Math.Round(_random.NextGaussian() * limits.Max * 0.1);
                        skill.Cooldown = Math.Max(limits.Min, Math.Min(limits.Max, skill.Cooldown + mutation));
                    }
                }
            }

            #endregion Mutator

            private readonly Mutator _mutator;

            public TrainRuler()
            {
                _mutator = new Mutator(Starter, _random);
            }

            public List<Boss> Train()
            {
                // Generazione popolazione iniziale
                var population = GenerateInitialPopulation();

                for (int gen = 0; gen < Starter.Generations; gen++)
                {
                    // Valutazione fitness
                    var fitnessScores = EvaluatePopulation(population);

                    // Selezione elite
                    var elite = SelectElite(population, fitnessScores);

                    // Generazione nuova popolazione
                    population = GenerateNextGeneration(elite);

                    // Applicazione mutazioni
                    ApplyMutations(population);

                    // Eventuale ruleset breaking
                    ApplyRulesetBreaking(population);
                }

                return population.OrderByDescending(boss =>
                    CalculateFitness(boss)).ToList();
            }

            private List<Boss> GenerateInitialPopulation()
            {
                var population = new List<Boss>();

                for (int i = 0; i < Starter.InitialPopulationSize; i++)
                {
                    population.Add(AssetsGenerator.GenerateBoss(Starter, _random, AvailableStatusEffects));
                }

                return population;
            }

            private List<double> EvaluatePopulation(List<Boss> population)
            {
                return population.Select(boss => CalculateFitness(boss)).ToList();
            }

            private List<Boss> SelectElite(List<Boss> population, List<double> fitnessScores)
            {
                var ranked = population.Zip(fitnessScores, (boss, fitness) =>
                    new { Boss = boss, Fitness = fitness })
                    .OrderByDescending(x => x.Fitness)
                    .ToList();

                return ranked.Take(Starter.EliteSize).Select(x => x.Boss).ToList();
            }

            private List<Boss> GenerateNextGeneration(List<Boss> elite)
            {
                var newGeneration = new List<Boss>();

                // Elitismo: i migliori passano direttamente
                newGeneration.AddRange(elite.Select(boss => new Boss(boss)));

                // Generazione figli
                while (newGeneration.Count < Starter.InitialPopulationSize)
                {
                    var parent1 = SelectParent(elite);
                    var parent2 = SelectParent(elite);

                    var child = Crossover(parent1, parent2);
                    newGeneration.Add(child);
                }

                return newGeneration;
            }

            private Boss SelectParent(List<Boss> elite)
            {
                // Selezione a torneo
                int tournamentSize = Math.Max(2, elite.Count / 4);
                var tournament = new List<Boss>();

                for (int i = 0; i < tournamentSize; i++)
                {
                    tournament.Add(elite[_random.Next(elite.Count)]);
                }

                return tournament.OrderByDescending(boss =>
                    CalculateFitness(boss)).First();
            }

            private Boss Crossover(Boss parent1, Boss parent2)
            {
                var child = new Boss(parent1);

                // Crossover attributi
                if (_random.NextDouble() < 0.5)
                {
                    child.Hp = parent2.Hp;
                    child.Mana = parent2.Mana;
                    child.Stamina = parent2.Stamina;
                }

                // Crossover AiPattern
                foreach (var key in child.AiPattern.Keys)
                {
                    if (_random.NextDouble() < 0.5)
                    {
                        child.AiPattern[key] = parent2.AiPattern[key];
                    }
                }

                // Crossover armi e abilità
                child.CurrentWeapons = _random.NextDouble() < 0.5 ?
                    new List<Weapon?>(parent1.CurrentWeapons) :
                    new List<Weapon?>(parent2.CurrentWeapons);

                child.CurrentSkills = _random.NextDouble() < 0.5 ?
                    new List<Skill?>(parent1.CurrentSkills) :
                    new List<Skill?>(parent2.CurrentSkills);

                return child;
            }

            private void ApplyMutations(List<Boss> population)
            {
                foreach (var boss in population)
                {
                    _mutator.MutateBoss(boss);
                }
            }

            private void ApplyRulesetBreaking(List<Boss> population)
            {
                foreach (var boss in population)
                {
                    if (_random.NextDouble() < Starter.RulesetBreakChance)
                    {
                        // Applica mutazione che viola i limiti
                        var attributes = new[] { "Strength", "Intelligence", "Defence", "Speed" };
                        var selectedAttr = attributes[_random.Next(attributes.Length)];

                        var currentValue = (int)typeof(Boss).GetProperty(selectedAttr).GetValue(boss);
                        var limits = Starter.AttributeLimits[selectedAttr];

                        // Superamento limite del 20-50%
                        var breaker = (int)(limits.Max * (0.2 + _random.NextDouble() * 0.3));
                        typeof(Boss).GetProperty(selectedAttr).SetValue(boss, currentValue + breaker);
                    }
                }
            }

            #region Fitness Calculator

            public double CalculateFitness(Boss boss)
            {
                // Componente 1: Performance di combattimento (50%)
                double combatScore = CalculateCombatPerformance(boss);

                // Componente 2: Adattamento all'Ender (30%)
                double enderScore = Ender.IsEmpty ? 0.5 : CalculateEnderAdaptation(boss);

                // Componente 3: Efficienza delle risorse (10%)
                double resourceScore = CalculateResourceEfficiency(boss);

                // Componente 4: Bilanciamento statistico (10%)
                double balanceScore = CalculateStatBalance(boss);

                return combatScore * 0.5 + enderScore * 0.3 +
                       resourceScore * 0.1 + balanceScore * 0.1;
            }

            private double CalculateCombatPerformance(Boss boss)
            {
                // Valutazione complessiva delle capacità di combattimento
                double offenseScore = (boss.Strength * 0.6 + boss.Intelligence * 0.4) / 100.0;
                double defenseScore = (boss.Defence * 0.7 + boss.TrueDefence * 0.3) / 100.0;
                double speedScore = boss.Speed / 100.0;

                // Valutazione delle armi
                double weaponScore = 0;
                if (boss.CurrentWeapons != null && boss.CurrentWeapons.Count > 0)
                {
                    foreach (var weapon in boss.CurrentWeapons)
                    {
                        if (weapon != null)
                        {
                            double weaponPower = weapon.BaseDamage * weapon.DamageScaling * (1 + weapon.CritChance * weapon.CritMultiplier);
                            weaponScore += Math.Min(1.0, weaponPower / 1000.0);
                        }
                    }
                    weaponScore = Math.Min(1.0, weaponScore / boss.CurrentWeapons.Count);
                }

                // Valutazione delle abilità
                double skillScore = 0;
                if (boss.CurrentSkills != null && boss.CurrentSkills.Count > 0)
                {
                    foreach (var skill in boss.CurrentSkills)
                    {
                        if (skill != null)
                        {
                            double skillPower = skill.BasePower * skill.Scaling * (1 + skill.CritChance * skill.CritMultiplier);
                            skillScore += Math.Min(1.0, skillPower / 500.0);
                        }
                    }
                    skillScore = Math.Min(1.0, skillScore / boss.CurrentSkills.Count);
                }

                // Combinazione dei punteggi
                return (offenseScore * 0.3 + defenseScore * 0.2 + speedScore * 0.2 +
                        weaponScore * 0.15 + skillScore * 0.15);
            }

            private double CalculateEnderAdaptation(Boss boss)
            {
                double attributeScore = 0;
                double behaviorScore = 0;

                // Calcolo distanza attributi
                if (Ender.TargetAttributes.Count > 0)
                {
                    foreach (var target in Ender.TargetAttributes)
                    {
                        var currentValue = (int)typeof(Boss).GetProperty(target.Key).GetValue(boss);
                        var diff = Math.Abs(currentValue - target.Value);
                        var maxDiff = Starter.AttributeLimits[target.Key].Max - Starter.AttributeLimits[target.Key].Min;
                        attributeScore += 1 - (diff / maxDiff);
                    }
                    attributeScore /= Ender.TargetAttributes.Count;
                }
                else
                {
                    attributeScore = 0.5;
                }

                // Calcolo distanza comportamenti
                if (Ender.TargetBehaviors.Count > 0)
                {
                    foreach (var target in Ender.TargetBehaviors)
                    {
                        if (boss.AiPattern.ContainsKey(target.Key))
                        {
                            var diff = Math.Abs(boss.AiPattern[target.Key] - target.Value);
                            var maxDiff = 1.0f; // Assumendo valori normalizzati 0-1
                            behaviorScore += 1 - (diff / maxDiff);
                        }
                    }
                    behaviorScore /= Ender.TargetBehaviors.Count;
                }
                else
                {
                    behaviorScore = 0.5;
                }

                return attributeScore * Ender.TargetWeights["Attributes"] +
                       behaviorScore * Ender.TargetWeights["Behaviors"];
            }

            private double CalculateResourceEfficiency(Boss boss)
            {
                // Calcolo efficienza delle risorse
                double hpEfficiency = Math.Min(1.0, boss.Hp / 1000.0);
                double manaEfficiency = Math.Min(1.0, boss.Mana / 500.0);
                double staminaEfficiency = Math.Min(1.0, boss.Stamina / 500.0);
                double loadEfficiency = 1.0 - (boss.CurrentEquipWeight / (double)boss.MaxEquipLoad);

                // Efficienza rigenerazione
                double regenScore = Math.Min(1.0, (boss.HpRegen + boss.ManaRegen + boss.StaminaRegen) / 30.0);

                // Combinazione dei punteggi
                return (hpEfficiency * 0.2 + manaEfficiency * 0.2 + staminaEfficiency * 0.2 +
                        loadEfficiency * 0.2 + regenScore * 0.2);
            }

            private double CalculateStatBalance(Boss boss)
            {
                // Calcolo bilanciamento statistico
                double[] stats = {
                boss.Strength, boss.Intelligence, boss.Defence, boss.Speed, boss.Wisdom
            };

                double mean = stats.Average();
                double variance = stats.Select(s => Math.Pow(s - mean, 2)).Average();
                double stdDev = Math.Sqrt(variance);

                // Punteggio più alto per bilanciamento ottimale
                double balanceScore = 1.0 - Math.Min(1.0, stdDev / 50.0);

                // Bonus per wisdom (influenza ricarica abilità)
                double wisdomBonus = Math.Min(1.0, boss.Wisdom / 100.0);

                // Bonus per velocità di attacco e precisione
                double combatBonus = Math.Min(1.0, (boss.AtkSpeed + boss.Accuracy) / 2.0);

                return (balanceScore * 0.5 + wisdomBonus * 0.25 + combatBonus * 0.25);
            }

            #endregion Fitness Calculator

            #region Battle Simulator

            public class BattleResult
            {
                public Boss Winner { get; set; }
                public Boss Loser { get; set; }
                public double DamageDealt { get; set; }
                public double DamageTaken { get; set; }
                public double SurvivalTime { get; set; }
                public double ResourceEfficiency { get; set; }

                public BattleResult()
                {
                    // Inizializza con valori di default
                    Winner = null;
                    Loser = null;
                    DamageDealt = 0;
                    DamageTaken = 0;
                    SurvivalTime = 0;
                    ResourceEfficiency = 0;
                }
            }

            public BattleResult SimulateBattle(Boss boss1, Boss boss2)
            {
                // Simulazione semplificata di battaglia
                double boss1Power = CalculateCombatPerformance(boss1);
                double boss2Power = CalculateCombatPerformance(boss2);

                // Fattore casuale per variabilità
                double randomFactor1 = 0.8 + _random.NextDouble() * 0.4;
                double randomFactor2 = 0.8 + _random.NextDouble() * 0.4;

                double boss1Score = boss1Power * randomFactor1;
                double boss2Score = boss2Power * randomFactor2;

                var result = new BattleResult();

                if (boss1Score > boss2Score)
                {
                    result.Winner = boss1;
                    result.Loser = boss2;
                    result.DamageDealt = boss2Score * 100;
                    result.DamageTaken = boss1Score * 50;
                }
                else
                {
                    result.Winner = boss2;
                    result.Loser = boss1;
                    result.DamageDealt = boss1Score * 100;
                    result.DamageTaken = boss2Score * 50;
                }

                // Tempo di sopravvivenza proporzionale alla difesa e HP
                result.SurvivalTime = (boss1.Defence + boss1.Hp / 10.0) * 0.1;

                // Efficienza risorse
                result.ResourceEfficiency = CalculateResourceEfficiency(result.Winner);

                return result;
            }

            #endregion Battle Simulator
        }
    }
}