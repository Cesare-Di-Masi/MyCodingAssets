using System;
using System.Collections.Generic;
using System.Linq;
using static BossGeneratorLib.BossGeneratorLib.TrainRuler;

namespace BossGeneratorLib
{
    public class BattleSystem
    {
        private readonly Random _random = new Random();
        private readonly int _maxTurns = 100; // Limite massimo di turni per evitare loop infiniti

        public BattleResult SimulateBattle(Boss boss1, Boss boss2)
        {
            var result = new BattleResult();
            if (!ValidateBattle(boss1, boss2))
            {
                // Se la battaglia non è valida, assegna un vincitore di default
                result.Winner = boss1 ?? boss2;
                result.Loser = boss1 != null ? boss2 : boss1;
                return result;
            }
            var currentBoss = boss1;
            var opponent = boss2;
            int turn = 0;

            // Inizializzazione delle risorse
            var boss1Resources = new BattleResources(boss1);
            var boss2Resources = new BattleResources(boss2);
            var currentResources = boss1Resources;
            var opponentResources = boss2Resources;

            while (turn < _maxTurns && boss1.IsAlive && boss2.IsAlive)
            {
                turn++;

                // Inizio turno: rigenerazione risorse
                StartTurn(currentBoss, currentResources);

                // Decisione IA
                var action = DecideAction(currentBoss, opponent, currentResources);

                // Esecuzione azione
                ExecuteAction(currentBoss, opponent, action, currentResources, opponentResources);

                // Applicazione effetti di stato
                ApplyStatusEffects(currentBoss, currentResources);
                ApplyStatusEffects(opponent, opponentResources);

                // Verifica sconfitta
                if (!boss1.IsAlive)
                {
                    result.Winner = boss2;
                    result.Loser = boss1;
                    result.DamageDealt = boss2Resources.DamageDealt;
                    result.DamageTaken = boss2Resources.DamageTaken;
                    result.SurvivalTime = turn;
                    result.ResourceEfficiency = CalculateResourceEfficiency(boss2, boss2Resources);
                    break;
                }

                if (!boss2.IsAlive)
                {
                    result.Winner = boss1;
                    result.Loser = boss2;
                    result.DamageDealt = boss1Resources.DamageDealt;
                    result.DamageTaken = boss1Resources.DamageTaken;
                    result.SurvivalTime = turn;
                    result.ResourceEfficiency = CalculateResourceEfficiency(boss1, boss1Resources);
                    break;
                }

                // Cambio turno
                (currentBoss, opponent) = (opponent, currentBoss);
                (currentResources, opponentResources) = (opponentResources, currentResources);
            }

            // Se nessuno è stato sconfitto, determina il vincitore in base agli HP rimanenti
            if (result.Winner == null)
            {
                if (boss1.IsAlive && boss2.IsAlive)
                {
                    // Entrambi ancora vivi, vince chi ha più HP
                    result.Winner = boss1.Hp > boss2.Hp ? boss1 : boss2;
                    result.Loser = boss1.Hp > boss2.Hp ? boss2 : boss1;
                }
                else if (boss1.IsAlive)
                {
                    // Solo boss1 è vivo
                    result.Winner = boss1;
                    result.Loser = boss2;
                }
                else if (boss2.IsAlive)
                {
                    // Solo boss2 è vivo
                    result.Winner = boss2;
                    result.Loser = boss1;
                }
                else
                {
                    // Entrambi morti (caso raro), vince chi ha fatto più danni
                    result.Winner = boss1Resources.DamageDealt > boss2Resources.DamageDealt ? boss1 : boss2;
                    result.Loser = boss1Resources.DamageDealt > boss2Resources.DamageDealt ? boss2 : boss1;
                }

                // Imposta i valori del risultato
                if (result.Winner == boss1)
                {
                    result.DamageDealt = boss1Resources.DamageDealt;
                    result.DamageTaken = boss1Resources.DamageTaken;
                    result.ResourceEfficiency = CalculateResourceEfficiency(boss1, boss1Resources);
                }
                else
                {
                    result.DamageDealt = boss2Resources.DamageDealt;
                    result.DamageTaken = boss2Resources.DamageTaken;
                    result.ResourceEfficiency = CalculateResourceEfficiency(boss2, boss2Resources);
                }

                result.SurvivalTime = turn;
            }

            return result;
        }

        private void StartTurn(Boss boss, BattleResources resources)
        {
            // Se il boss non è vivo, non fare nulla
            if (!boss.IsAlive)
                return;

            // Rigenerazione HP
            boss.Hp = Math.Min(boss.MaxHp, boss.Hp + (int)boss.HpRegen);

            // Rigenerazione Mana
            resources.CurrentMana = Math.Min(resources.MaxMana, resources.CurrentMana + (int)boss.ManaRegen);

            // Rigenerazione Stamina
            resources.CurrentStamina = Math.Min(resources.MaxStamina, resources.CurrentStamina + (int)boss.StaminaRegen);

            // Riduzione cooldown abilità
            foreach (var cooldown in resources.Cooldowns.ToList())
            {
                resources.Cooldowns[cooldown.Key] = Math.Max(0, cooldown.Value - 1);
            }
        }

        private bool ValidateBattle(Boss boss1, Boss boss2)
        {
            // Verifica che i boss non siano null
            if (boss1 == null || boss2 == null)
                return false;

            // Verifica che i boss siano vivi all'inizio
            if (!boss1.IsAlive || !boss2.IsAlive)
                return false;

            return true;
        }

        private BattleAction DecideAction(Boss boss, Boss opponent, BattleResources resources)
        {
            var actions = new List<BattleAction>();

            // Aggiungi tutte le azioni possibili
            actions.Add(new BattleAction { Type = ActionType.BasicAttack });

            // Aggiungi abilità disponibili
            foreach (var skill in boss.CurrentSkills)
            {
                if (skill != null && !resources.Cooldowns.ContainsKey(skill.Id) &&
                    resources.CurrentMana >= skill.ManaCost &&
                    resources.CurrentStamina >= skill.StaminaCost)
                {
                    actions.Add(new BattleAction { Type = ActionType.Skill, Skill = skill });
                }
            }

            // Aggiungi azioni difensive
            actions.Add(new BattleAction { Type = ActionType.Block });
            actions.Add(new BattleAction { Type = ActionType.Dodge });

            // Ponderazione delle azioni basata sull'IA
            var weightedActions = new List<(BattleAction Action, double Weight)>();

            foreach (var action in actions)
            {
                double weight = 1.0;

                // Modifica peso in base all'IA - usa il metodo sicuro
                switch (action.Type)
                {
                    case ActionType.BasicAttack:
                        weight *= boss.GetAiBehavior("Aggressiveness");
                        weight *= (1 + boss.GetAiBehavior("RiskTaking"));
                        break;

                    case ActionType.Skill:
                        weight *= boss.GetAiBehavior("StatusEffectUsage");
                        weight *= boss.GetAiBehavior("BurstUsage");
                        break;

                    case ActionType.Block:
                        weight *= boss.GetAiBehavior("BlockingTendency");
                        weight *= (1 - boss.GetAiBehavior("Aggressiveness"));
                        break;

                    case ActionType.Dodge:
                        weight *= boss.GetAiBehavior("DodgingTendency");
                        weight *= (1 - boss.GetAiBehavior("Aggressiveness"));
                        break;
                }

                // Modifica peso in base alla situazione
                if (boss.Hp < GetMaxHp(boss) * 0.3) // HP bassi
                {
                    weight *= action.Type == ActionType.Block || action.Type == ActionType.Dodge ? 2.0 : 0.5;
                }

                if (opponent.Hp < GetMaxHp(opponent) * 0.2) // Avversario quasi sconfitto
                {
                    weight *= boss.GetAiBehavior("FinisherInstinct");
                }

                weightedActions.Add((action, weight));
            }

            // Selezione casuale ponderata
            var totalWeight = weightedActions.Sum(wa => wa.Weight);
            var randomValue = _random.NextDouble() * totalWeight;

            double cumulativeWeight = 0;
            foreach (var (action, weight) in weightedActions)
            {
                cumulativeWeight += weight;
                if (randomValue <= cumulativeWeight)
                {
                    return action;
                }
            }

            // Fallback: attacco base
            return new BattleAction { Type = ActionType.BasicAttack };
        }

        private void ExecuteAction(Boss attacker, Boss defender, BattleAction action,
                                 BattleResources attackerResources, BattleResources defenderResources)
        {
            try
            {
                switch (action.Type)
                {
                    case ActionType.BasicAttack:
                        ExecuteBasicAttack(attacker, defender, attackerResources, defenderResources);
                        break;

                    case ActionType.Skill:
                        if (action.Skill != null)
                        {
                            ExecuteSkill(attacker, defender, action.Skill, attackerResources, defenderResources);
                        }
                        break;

                    case ActionType.Block:
                        attackerResources.IsBlocking = true;
                        break;

                    case ActionType.Dodge:
                        attackerResources.IsDodging = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                // In caso di errore, assegna comunque un risultato per evitare null
                Console.WriteLine($"Errore durante l'esecuzione dell'azione: {ex.Message}");
            }
        }

        private void ExecuteBasicAttack(Boss attacker, Boss defender,
                                      BattleResources attackerResources, BattleResources defenderResources)
        {
            // Calcolo danno base
            double baseDamage = attacker.Strength * 1.5;

            // Modifica danno in base all'arma equipaggiata
            if (attacker.CurrentWeapons.Count > 0)
            {
                var weapon = attacker.CurrentWeapons[_random.Next(attacker.CurrentWeapons.Count)];
                if (weapon != null)
                {
                    baseDamage = weapon.BaseDamage * weapon.DamageScaling;
                    baseDamage *= (1 + attacker.Strength / 100.0);

                    // Considera il peso dell'arma
                    float speedModifier = 1.0f - (weapon.Weight / 100.0f);
                    attackerResources.CurrentStamina -= (int)(10 * (2 - speedModifier));
                }
            }

            // Calcolo precisione
            double accuracy = attacker.Accuracy + attacker.Speed * 0.1;
            double dodgeChance = defenderResources.IsDodging ? defender.AiPattern["DodgingTendency"] * 0.5 : 0.1;

            // Verifica colpo
            if (_random.NextDouble() > accuracy - dodgeChance)
            {
                // Schivata
                return;
            }

            // Calcolo critico
            double critChance = attacker.CurrentWeapons.Count > 0 ?
                attacker.CurrentWeapons[0].CritChance : 0.1;
            double critMultiplier = attacker.CurrentWeapons.Count > 0 ?
                attacker.CurrentWeapons[0].CritMultiplier : 1.5;

            bool isCrit = _random.NextDouble() < critChance;
            if (isCrit)
            {
                baseDamage *= critMultiplier;
            }

            // Applicazione difesa
            double defense = defender.Defence;
            if (defenderResources.IsBlocking)
            {
                defense *= 1.5;
                defenderResources.CurrentStamina -= 15;
            }

            int damage = (int)Math.Max(1, baseDamage - defense * 0.5);
            damage = (int)Math.Max(1, damage - defender.TrueDefence);

            // Applica danno in modo sicuro
            defender.Hp = Math.Max(0, defender.Hp - damage);
            attackerResources.DamageDealt += damage;
            defenderResources.DamageTaken += damage;

            // Applica effetti di stato dalle armi
            if (attacker.CurrentWeapons.Count > 0)
            {
                var weapon = attacker.CurrentWeapons[_random.Next(attacker.CurrentWeapons.Count)];
                if (weapon != null)
                {
                    foreach (var effect in weapon.StatusEffectsDmg)
                    {
                        if (_random.NextDouble() < effect.ApplicationChance)
                        {
                            ApplyStatusEffect(defender, effect, defenderResources);
                        }
                    }
                }
            }
        }

        private void ExecuteSkill(Boss attacker, Boss defender, Skill skill,
                                 BattleResources attackerResources, BattleResources defenderResources)
        {
            // Consumo risorse
            attackerResources.CurrentMana = Math.Max(0, attackerResources.CurrentMana - skill.ManaCost);
            attackerResources.CurrentStamina = Math.Max(0, attackerResources.CurrentStamina - skill.StaminaCost);

            // Imposta cooldown
            attackerResources.Cooldowns[skill.Id] = skill.Cooldown;

            // Calcolo danno base
            double baseDamage = skill.BasePower * skill.Scaling;

            // Modifica in base alla statistica rilevante
            if (skill.Id.Contains("Fire") || skill.Id.Contains("Ice") || skill.Id.Contains("Lightning"))
            {
                baseDamage *= (1 + attacker.Intelligence / 100.0);
            }
            else
            {
                baseDamage *= (1 + attacker.Strength / 100.0);
            }

            // Calcolo critico
            bool isCrit = _random.NextDouble() < skill.CritChance;
            if (isCrit)
            {
                baseDamage *= skill.CritMultiplier;
            }

            // Applicazione area d'effetto
            if (skill.AreaOfEffect.HasValue)
            {
                baseDamage *= 0.8; // Riduzione danno per AoE
            }

            // Applicazione difesa
            double defense = defender.Defence;
            if (defenderResources.IsBlocking)
            {
                defense *= 1.5;
            }

            int damage = (int)Math.Max(1, baseDamage - defense * 0.5);
            damage = (int)Math.Max(1, damage - defender.TrueDefence);

            defender.Hp = Math.Max(0, defender.Hp - damage);
            attackerResources.DamageDealt += damage;
            defenderResources.DamageTaken += damage;

            // Applica effetti di stato
            foreach (var effect in skill.StatusEffects)
            {
                if (_random.NextDouble() < effect.ApplicationChance)
                {
                    ApplyStatusEffect(defender, effect, defenderResources);
                }
            }
        }

        private void ApplyStatusEffect(Boss target, StatusEffect effect, BattleResources resources)
        {
            // Crea una copia dell'effetto per tracciare la durata
            var activeEffect = new ActiveStatusEffect
            {
                Effect = effect,
                RemainingDuration = effect.Duration,
                NextTick = effect.TickInterval
            };

            resources.ActiveEffects.Add(activeEffect);
        }

        private void ApplyStatusEffects(Boss boss, BattleResources resources)
        {
            foreach (var activeEffect in resources.ActiveEffects.ToList())
            {
                activeEffect.NextTick--;

                if (activeEffect.NextTick <= 0)
                {
                    // Applica l'effetto
                    var effect = activeEffect.Effect;

                    switch (effect.Target)
                    {
                        case 0: // HP
                            int hpChange = (int)(effect.Value * effect.Scaling);
                            if (effect.IsPositive)
                            {
                                boss.Hp = Math.Min(boss.Hp + hpChange, GetMaxHp(boss));
                            }
                            else
                            {
                                boss.Hp = Math.Max(1, boss.Hp - hpChange);
                            }
                            break;

                        case 1: // Difesa
                            if (effect.IsPositive)
                            {
                                resources.DefenseModifier += effect.Value;
                            }
                            else
                            {
                                resources.DefenseModifier -= effect.Value;
                            }
                            break;

                        case 2: // Velocità
                            if (effect.IsPositive)
                            {
                                resources.SpeedModifier += effect.Value;
                            }
                            else
                            {
                                resources.SpeedModifier -= effect.Value;
                            }
                            break;
                    }

                    // Resetta il tick
                    activeEffect.NextTick = effect.TickInterval;
                }

                activeEffect.RemainingDuration--;
                if (activeEffect.RemainingDuration <= 0)
                {
                    resources.ActiveEffects.Remove(activeEffect);
                }
            }
        }

        private double CalculateResourceEfficiency(Boss boss, BattleResources resources)
        {
            // Calcola l'efficienza nell'uso delle risorse
            double manaEfficiency = resources.CurrentMana / (double)boss.Mana;
            double staminaEfficiency = resources.CurrentStamina / (double)boss.Stamina;

            // Bonus per aver usato le abilità in modo efficace
            double skillEfficiency = resources.SkillsUsed > 0 ?
                Math.Min(1.0, resources.DamageDealt / (resources.SkillsUsed * 100.0)) : 0.5;

            return (manaEfficiency + staminaEfficiency + skillEfficiency) / 3.0;
        }

        private int GetMaxHp(Boss boss)
        {
            // Calcola l'HP massimo considerando i buff
            return boss.Hp;
        }

        #region Classi di supporto

        public class BattleResources
        {
            public int CurrentMana { get; set; }
            public int CurrentStamina { get; set; }
            public int MaxMana { get; set; }
            public int MaxStamina { get; set; }
            public Dictionary<string, int> Cooldowns { get; set; } = new Dictionary<string, int>();
            public List<ActiveStatusEffect> ActiveEffects { get; set; } = new List<ActiveStatusEffect>();
            public bool IsBlocking { get; set; }
            public bool IsDodging { get; set; }
            public double DamageDealt { get; set; }
            public double DamageTaken { get; set; }
            public double DefenseModifier { get; set; }
            public double SpeedModifier { get; set; }
            public int SkillsUsed { get; set; }

            public BattleResources(Boss boss)
            {
                CurrentMana = boss.Mana;
                CurrentStamina = boss.Stamina;
                MaxMana = boss.Mana;
                MaxStamina = boss.Stamina;
            }
        }

        public class ActiveStatusEffect
        {
            public StatusEffect Effect { get; set; }
            public float RemainingDuration { get; set; }
            public float NextTick { get; set; }
        }

        public class BattleAction
        {
            public ActionType Type { get; set; }
            public Skill Skill { get; set; }
        }

        public enum ActionType
        {
            BasicAttack,
            Skill,
            Block,
            Dodge
        }

        #endregion Classi di supporto
    }
}