using BossGeneratorLib.BossGeneratorLib;

namespace BossGeneratorLib
{
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

            // Usa tutte le chiavi predefinite invece di solo quelle nei limiti
            foreach (var key in Boss.DefaultAiPatternKeys)
            {
                if (starterRules.AiPatternLimits.ContainsKey(key))
                {
                    var limits = starterRules.AiPatternLimits[key];
                    aiPattern[key] = (float)(random.NextDouble() * (limits.Max - limits.Min) + limits.Min);
                }
                else
                {
                    // Se non ci sono limiti definiti, imposta a 0
                    aiPattern[key] = 0f;
                }
            }

            return aiPattern;
        }

        #endregion Helper Methods
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
}