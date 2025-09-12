using System;
using System.Collections.Generic;
using System.Linq;

namespace BossGeneratorLib
{
    public class Skill
    {
        #region Campi privati

        private string _id;

        // Effetto principale della skill
        private int _basePower;            // Potenza base dell'effetto (danno, cura, scudo, ecc.)

        private float _scaling;            // Moltiplicatore basato sulla stat rilevante del boss/personaggio

        // Risorse richieste
        private int _manaCost;             // Punti mana/energia

        private int _staminaCost;          // Punti stamina (se applicabile)
        private int _cooldown;             // Tempo di riuso in secondi o tick

        // Durata e area di effetto
        private float? _duration;           // Durata della skill (0 se istantanea) (null se permanente (talismani))

        private float? _areaOfEffect;       // Raggio dell'effetto (0 se mirata/target single) (null se self)

        // Probabilità di effetto critico
        private float _critChance;         // Probabilità di critico della skill

        private float _critMultiplier;     // Moltiplicatore danno/effetto critico

        // Statistiche aggiuntive o modificatori
        private List<StatusEffect> _statusEffects; // Buff/Debuff applicati dal boss o al nemico

        #endregion Campi privati

        #region Proprietà

        public string Id
        {
            get { return _id; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value not acceptable");
                _id = value;
            }
        }

        public int BasePower
        {
            get { return _basePower; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _basePower = value;
            }
        }

        public float Scaling
        {
            get { return _scaling; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _scaling = value;
            }
        }

        public int ManaCost
        {
            get { return _manaCost; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _manaCost = value;
            }
        }

        public int StaminaCost
        {
            get { return _staminaCost; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _staminaCost = value;
            }
        }

        public int Cooldown
        {
            get { return _cooldown; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _cooldown = value;
            }
        }

        public float? Duration
        {
            get { return _duration; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _duration = value;
            }
        }

        public float? AreaOfEffect
        {
            get { return _areaOfEffect; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _areaOfEffect = value;
            }
        }

        public float CritChance
        {
            get { return _critChance; }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _critChance = value;
            }
        }

        public float CritMultiplier
        {
            get { return _critMultiplier; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _critMultiplier = value;
            }
        }

        public List<StatusEffect> StatusEffects
        {
            get { return _statusEffects; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value not acceptable");
                _statusEffects = value;
            }
        }

        // Proprietà calcolate
        public bool IsHealingSkill
        {
            get { return _id.Contains("Heal") || _id.Contains("Regen") || _statusEffects.Any(e => e.IsPositive && e.Target == 0); }
        }

        public bool IsBuffSkill
        {
            get { return _statusEffects.Any(e => e.IsPositive && e.Target != 0); }
        }

        public bool IsDebuffSkill
        {
            get { return _statusEffects.Any(e => !e.IsPositive); }
        }

        public bool IsAreaOfEffect
        {
            get { return _areaOfEffect.HasValue && _areaOfEffect.Value > 0; }
        }

        public bool IsInstantaneous
        {
            get { return !_duration.HasValue || _duration.Value == 0; }
        }

        public bool IsPermanent
        {
            get { return _duration.HasValue && _duration.Value == 0; }
        }

        public int TotalResourceCost
        {
            get { return _manaCost + _staminaCost; }
        }

        #endregion Proprietà

        #region Costruttori

        public Skill(string id, int basePower, float scaling, int manaCost, int staminaCost, int cooldown, float? duration, float? areaOfEffect, float critChance, float critMultiplier, List<StatusEffect> statusEffects)
        {
            _id = id;
            _basePower = basePower;
            _scaling = scaling;
            _manaCost = manaCost;
            _staminaCost = staminaCost;
            _cooldown = cooldown;
            _duration = duration;
            _areaOfEffect = areaOfEffect;
            _critChance = critChance;
            _critMultiplier = critMultiplier;
            _statusEffects = statusEffects;
        }

        public Skill(Skill skill)
        {
            _id = skill.Id;
            _basePower = skill.BasePower;
            _scaling = skill.Scaling;
            _manaCost = skill.ManaCost;
            _staminaCost = skill.StaminaCost;
            _cooldown = skill.Cooldown;
            _duration = skill.Duration;
            _areaOfEffect = skill.AreaOfEffect;
            _critChance = skill.CritChance;
            _critMultiplier = skill.CritMultiplier;
            _statusEffects = new List<StatusEffect>(skill.StatusEffects);
        }

        public Skill()
        {
            _id = "default_skill";
            _basePower = 0;
            _scaling = 0;
            _manaCost = 0;
            _staminaCost = 0;
            _cooldown = 0;
            _duration = 0;
            _areaOfEffect = 0;
            _critChance = 0;
            _critMultiplier = 1;
            _statusEffects = new List<StatusEffect>();
        }

        #endregion Costruttori

        #region Metodi pubblici

        // Metodo per verificare se un boss può usare questa abilità
        public bool CanBeUsedBy(Boss boss, BattleSystem.BattleResources resources = null)
        {
            // Verifica le risorse
            if (resources != null)
            {
                if (resources.CurrentMana < _manaCost || resources.CurrentStamina < _staminaCost)
                    return false;
            }
            else
            {
                if (boss.Mana < _manaCost || boss.Stamina < _staminaCost)
                    return false;
            }

            // Verifica il cooldown
            if (resources != null && resources.Cooldowns.ContainsKey(_id))
                return false;

            return true;
        }

        // Metodo per calcolare l'efficacia dell'abilità basata sulle statistiche di un boss
        public float CalculateEffectiveness(Boss boss)
        {
            // Determina la statistica rilevante per l'abilità
            float relevantStat = 0;

            if (_id.Contains("Fire") || _id.Contains("Ice") || _id.Contains("Lightning") ||
                _id.Contains("Magic") || _manaCost > _staminaCost)
            {
                // Abilità magica, basata su intelligenza
                relevantStat = boss.Intelligence;
            }
            else if (_id.Contains("Heal") || _id.Contains("Regen"))
            {
                // Abilità di cura, basata su wisdom
                relevantStat = boss.Wisdom;
            }
            else
            {
                // Abilità fisica, basata su forza
                relevantStat = boss.Strength;
            }

            // Calcola l'efficacia base
            float effectiveness = _basePower * _scaling * (1 + relevantStat / 100f);

            // Applica modificatori in base alle caratteristiche del boss
            if (IsHealingSkill)
            {
                // Le abilità di cura beneficiano di wisdom
                effectiveness *= (1 + boss.Wisdom / 200f);
            }
            else
            {
                // Le abilità offensive beneficiano di altri fattori
                float aggressiveness = boss.GetAiBehavior("Aggressiveness");
                effectiveness *= (1 + aggressiveness * 0.5f);
            }

            // Considera l'efficienza delle risorse
            float resourceEfficiency = 1 - (TotalResourceCost / (float)(boss.Mana + boss.Stamina) * 0.5f);
            effectiveness *= resourceEfficiency;

            return effectiveness;
        }

        // Metodo per calcolare il danno/effetto effettivo dell'abilità
        public float CalculateEffectivePower(Boss boss, bool isCritical = false)
        {
            float power = CalculateEffectiveness(boss);

            // Applica modificatore per area d'effetto
            if (IsAreaOfEffect)
            {
                power *= 0.8f; // Riduzione del 20% per abilità ad area
            }

            // Applica modificatore critico
            if (isCritical)
            {
                power *= _critMultiplier;
            }

            return power;
        }

        // Metodo per consumare le risorse
        public void ConsumeResources(Boss boss, BattleSystem.BattleResources resources)
        {
            if (resources != null)
            {
                resources.CurrentMana -= _manaCost;
                resources.CurrentStamina -= _staminaCost;
                resources.Cooldowns[_id] = _cooldown;
            }
            else
            {
                // Fallback se non sono disponibili risorse di battaglia
                boss.Mana -= _manaCost;
                boss.Stamina -= _staminaCost;
            }
        }

        // Metodo per applicare gli effetti dell'abilità
        public void ApplyEffects(Boss caster, Boss target, BattleSystem.BattleResources targetResources = null, bool isCritical = false)
        {
            float power = CalculateEffectivePower(caster, isCritical);

            // Applica l'effetto principale
            if (IsHealingSkill)
            {
                // Abilità di cura
                int healAmount = (int)power;
                target.Hp = Math.Min(target.Hp + healAmount, GetMaxHp(target));
            }
            else
            {
                // Abilità offensiva
                int damage = (int)power;

                // Applica difese
                int totalDefense = target.Defence + target.TrueDefence;
                if (targetResources != null)
                {
                    totalDefense += (int)targetResources.DefenseModifier;
                }

                damage = Math.Max(1, damage - totalDefense / 2);

                // Applica danno
                target.Hp = Math.Max(1, target.Hp - damage);
            }

            // Applica gli effetti di stato
            foreach (var effect in _statusEffects)
            {
                // Modifica la probabilità di applicazione in base al critico
                if (isCritical)
                {
                    var modifiedEffect = effect.Clone();
                    modifiedEffect.ApplicationChance = Math.Min(1.0f, effect.ApplicationChance * 1.5f);
                    modifiedEffect.ApplyTo(target, targetResources);
                }
                else
                {
                    effect.ApplyTo(target, targetResources);
                }
            }
        }

        // Metodo per clonare l'abilità
        public Skill Clone()
        {
            return new Skill(this);
        }

        // Metodo per ottenere una descrizione testuale
        public override string ToString()
        {
            string type = IsHealingSkill ? "Healing" : (IsBuffSkill ? "Buff" : (IsDebuffSkill ? "Debuff" : "Offensive"));
            string aoeText = IsAreaOfEffect ? $" (AoE: {_areaOfEffect}m)" : "";
            string durationText = IsPermanent ? "Permanent" : (IsInstantaneous ? "Instant" : $"{_duration}s");
            string costText = $"Cost: {_manaCost}MP/{_staminaCost}ST";
            string critText = $"Crit: {(_critChance * 100).ToString("F1")}%x{_critMultiplier.ToString("F1")}";

            return $"{_id} ({type}){aoeText} - Power: {_basePower}x{_scaling} - {durationText} - CD: {_cooldown}s - {costText} - {critText}";
        }

        #endregion Metodi pubblici

        #region Metodi privati

        private int GetMaxHp(Boss boss)
        {
            // Metodo helper per ottenere l'HP massimo di un boss
            return boss.Hp; // Semplificato per questo esempio
        }

        #endregion Metodi privati
    }
}