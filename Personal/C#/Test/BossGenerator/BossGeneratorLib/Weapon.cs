using System;
using System.Collections.Generic;
using System.Linq;

namespace BossGeneratorLib
{
    public class Weapon
    {
        #region Campi privati

        // Identificativo interno
        private string _name;

        // Tipologia dell'arma (es: spada, martello, arco)
        private string _weaponType;

        // Requisiti per l'equipaggiamento
        private int _reqStrength;

        private int _reqIntelligence;
        private int _reqDefense;
        private int _reqSpeed;

        // Statistiche principali dell'arma
        private int _baseDamage;           // Danno base dell'arma

        private float _damageScaling;      // Moltiplicatore in base alla stat rilevante del personaggio
        private float _attackSpeed;        // Velocità d'attacco (colpi al secondo o tempo tra attacchi)
        private int _range;                // Portata dell'arma
        private int _weight;               // Influenza mobilità e velocità attacco

        // Critico
        private float _critChance;         // Probabilità di critico (0.0 - 1.0)

        private float _critMultiplier;     // Moltiplicatore danno critico

        // Abilità legata all'arma
        private Skill _weaponSkill;

        // Effetti aggiuntivi
        private List<StatusEffect> _statusEffectsDmg;  // Bonus/penalità su stats o effetti speciali

        // Slot per futuri upgrade o modificatori
        private int _upgradeSlots;

        // Upgrade attualmente installati
        private List<WeaponUpgrade> _installedUpgrades;

        #endregion Campi privati

        #region Proprietà

        public string Name
        {
            get { return _name; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value not acceptable");
                _name = value;
            }
        }

        public string WeaponType
        {
            get { return _weaponType; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value not acceptable");
                _weaponType = value;
            }
        }

        public int ReqStrength
        {
            get { return _reqStrength; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _reqStrength = value;
            }
        }

        public int ReqIntelligence
        {
            get { return _reqIntelligence; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _reqIntelligence = value;
            }
        }

        public int ReqDefense
        {
            get { return _reqDefense; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _reqDefense = value;
            }
        }

        public int ReqSpeed
        {
            get { return _reqSpeed; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _reqSpeed = value;
            }
        }

        public int BaseDamage
        {
            get { return _baseDamage; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _baseDamage = value;
            }
        }

        public float DamageScaling
        {
            get { return _damageScaling; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _damageScaling = value;
            }
        }

        public float AttackSpeed
        {
            get { return _attackSpeed; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _attackSpeed = value;
            }
        }

        public int Range
        {
            get { return _range; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _range = value;
            }
        }

        public int Weight
        {
            get { return _weight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _weight = value;
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

        public Skill WeaponSkill
        {
            get { return _weaponSkill; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value not acceptable");
                _weaponSkill = value;
            }
        }

        public List<StatusEffect> StatusEffectsDmg
        {
            get { return _statusEffectsDmg; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value not acceptable");
                _statusEffectsDmg = value;
            }
        }

        public int UpgradeSlots
        {
            get { return _upgradeSlots; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _upgradeSlots = value;
                // Se il numero di slot viene ridotto, rimuovi gli upgrade in eccesso
                while (_installedUpgrades.Count > _upgradeSlots && _installedUpgrades.Count > 0)
                {
                    _installedUpgrades.RemoveAt(_installedUpgrades.Count - 1);
                }
            }
        }

        public List<WeaponUpgrade> InstalledUpgrades
        {
            get { return _installedUpgrades; }
            private set { _installedUpgrades = value ?? new List<WeaponUpgrade>(); }
        }

        // Proprietà calcolate
        public int AvailableUpgradeSlots
        {
            get { return _upgradeSlots - _installedUpgrades.Count; }
        }

        public float EffectiveDamageScaling
        {
            get
            {
                float scaling = _damageScaling;
                // Aggiungi bonus dagli upgrade
                scaling += _installedUpgrades.Where(u => u.UpgradeType == UpgradeType.DamageScaling)
                                           .Sum(u => u.Value);
                return scaling;
            }
        }

        public float EffectiveAttackSpeed
        {
            get
            {
                float speed = _attackSpeed;
                // Aggiungi bonus dagli upgrade
                speed += _installedUpgrades.Where(u => u.UpgradeType == UpgradeType.AttackSpeed)
                                        .Sum(u => u.Value);
                return speed;
            }
        }

        public float EffectiveCritChance
        {
            get
            {
                float chance = _critChance;
                // Aggiungi bonus dagli upgrade
                chance += _installedUpgrades.Where(u => u.UpgradeType == UpgradeType.CritChance)
                                         .Sum(u => u.Value);
                return Math.Min(1.0f, chance); // Non può superare 100%
            }
        }

        public float EffectiveCritMultiplier
        {
            get
            {
                float multiplier = _critMultiplier;
                // Aggiungi bonus dagli upgrade
                multiplier += _installedUpgrades.Where(u => u.UpgradeType == UpgradeType.CritMultiplier)
                                             .Sum(u => u.Value);
                return Math.Max(1.0f, multiplier); // Non può scendere sotto 1.0
            }
        }

        #endregion Proprietà

        #region Costruttori

        public Weapon(string name, string weaponType, int reqStr, int reqInt, int reqDef, int redSpd, int baseDmg, float dmgScaling, float atkSpeed,
            int range, int weight, float critChance, float critMul, Skill skill, List<StatusEffect> effectsDmg, int? upgSlots = null)
        {
            _name = name;
            _weaponType = weaponType;
            _reqStrength = reqStr;
            _reqIntelligence = reqInt;
            _reqDefense = reqDef;
            _reqSpeed = redSpd;
            _baseDamage = baseDmg;
            _damageScaling = dmgScaling;
            _attackSpeed = atkSpeed;
            _range = range;
            _weight = weight;
            _critChance = critChance;
            _critMultiplier = critMul;
            _weaponSkill = skill;
            _statusEffectsDmg = effectsDmg;
            _upgradeSlots = upgSlots ?? 0;
            _installedUpgrades = new List<WeaponUpgrade>();
        }

        public Weapon(Weapon predecessor)
        {
            _name = predecessor.Name;
            _weaponType = predecessor.WeaponType;
            _reqStrength = predecessor.ReqStrength;
            _reqIntelligence = predecessor.ReqIntelligence;
            _reqDefense = predecessor.ReqDefense;
            _reqSpeed = predecessor.ReqSpeed;
            _baseDamage = predecessor.BaseDamage;
            _damageScaling = predecessor.DamageScaling;
            _attackSpeed = predecessor.AttackSpeed;
            _range = predecessor.Range;
            _weight = predecessor.Weight;
            _critChance = predecessor.CritChance;
            _critMultiplier = predecessor.CritMultiplier;
            _weaponSkill = predecessor.WeaponSkill;
            _statusEffectsDmg = new List<StatusEffect>(predecessor.StatusEffectsDmg);
            _upgradeSlots = predecessor.UpgradeSlots;
            _installedUpgrades = new List<WeaponUpgrade>(predecessor.InstalledUpgrades);
        }

        #endregion Costruttori

        #region Metodi pubblici

        // Metodo per verificare se un boss può usare questa arma
        public bool CanBeUsedBy(Boss boss)
        {
            return boss.Strength >= _reqStrength &&
                   boss.Intelligence >= _reqIntelligence &&
                   boss.Defence >= _reqDefense &&
                   boss.Speed >= _reqSpeed;
        }

        // Metodo per calcolare il danno effettivo basato sulle statistiche del boss
        public int CalculateEffectiveDamage(Boss boss)
        {
            if (!CanBeUsedBy(boss))
                return 0; // Non può usare l'arma

            // Determina la statistica rilevante per il danno
            float relevantStat = 0;
            if (_weaponType == "Staff" || _weaponType == "Wand")
            {
                relevantStat = boss.Intelligence;
            }
            else
            {
                relevantStat = boss.Strength;
            }

            // Calcola il danno base
            float damage = _baseDamage * (1 + relevantStat / 100f) * EffectiveDamageScaling;

            // Applica penalità per peso eccessivo rispetto al max equip load
            float weightRatio = boss.CurrentEquipWeight / (float)boss.MaxEquipLoad;
            if (weightRatio > 0.8f)
            {
                damage *= (1.0f - (weightRatio - 0.8f) * 0.5f); // Riduzione fino al 10% se sovraccarico
            }

            return (int)damage;
        }

        // Metodo per calcolare il DPS (Danno Per Secondo)
        public float CalculateDPS(Boss boss)
        {
            int damage = CalculateEffectiveDamage(boss);
            float attacksPerSecond = EffectiveAttackSpeed;
            float critChance = EffectiveCritChance;
            float critMultiplier = EffectiveCritMultiplier;

            // DPS = danno * attacchi al secondo * (1 + chance critico * (moltiplicatore critico - 1))
            return damage * attacksPerSecond * (1 + critChance * (critMultiplier - 1));
        }

        // Metodo per installare un upgrade
        public bool InstallUpgrade(WeaponUpgrade upgrade)
        {
            if (upgrade == null)
                throw new ArgumentNullException("upgrade cannot be null");

            if (AvailableUpgradeSlots <= 0)
                return false; // Nessuno slot disponibile

            _installedUpgrades.Add(upgrade);
            return true;
        }

        // Metodo per rimuovere un upgrade
        public bool RemoveUpgrade(WeaponUpgrade upgrade)
        {
            if (upgrade == null)
                throw new ArgumentNullException("upgrade cannot be null");

            return _installedUpgrades.Remove(upgrade);
        }

        // Metodo per rimuovere tutti gli upgrade
        public void RemoveAllUpgrades()
        {
            _installedUpgrades.Clear();
        }

        // Metodo per ottenere gli effetti di stato attivi
        public List<StatusEffect> GetActiveStatusEffects()
        {
            var effects = new List<StatusEffect>(_statusEffectsDmg);

            // Aggiungi effetti dagli upgrade
            effects.AddRange(_installedUpgrades
                .Where(u => u.StatusEffect != null)
                .Select(u => u.StatusEffect));

            return effects;
        }

        // Metodo per clonare l'arma
        public Weapon Clone()
        {
            return new Weapon(this);
        }

        // Metodo per ottenere una descrizione testuale
        public override string ToString()
        {
            return $"{_name} ({_weaponType}) - DMG: {_baseDamage} - Crit: {(_critChance * 100).ToString("F1")}%x{(_critMultiplier).ToString("F1")} - Slots: {AvailableUpgradeSlots}/{_upgradeSlots}";
        }

        #endregion Metodi pubblici
    }

    #region Classi di supporto per Weapon

    public enum UpgradeType
    {
        DamageScaling,
        AttackSpeed,
        CritChance,
        CritMultiplier,
        StatusEffect
    }

    public class WeaponUpgrade
    {
        public string Name { get; set; }
        public UpgradeType UpgradeType { get; set; }
        public float Value { get; set; }
        public StatusEffect StatusEffect { get; set; }

        public WeaponUpgrade(string name, UpgradeType upgradeType, float value, StatusEffect statusEffect = null)
        {
            Name = name;
            UpgradeType = upgradeType;
            Value = value;
            StatusEffect = statusEffect;
        }
    }

    #endregion Classi di supporto per Weapon
}