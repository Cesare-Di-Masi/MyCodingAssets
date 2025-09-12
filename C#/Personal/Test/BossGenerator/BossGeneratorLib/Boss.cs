using System;
using System.Collections.Generic;
using System.Linq;

namespace BossGeneratorLib
{
    public class Boss
    {
        private bool _isAlive;
        private string _iD;

        public static readonly List<string> DefaultAiPatternKeys = new List<string>
    {
        "AggroLevel", "FocusTargetLock", "DecisionLatency", "RageMeter", "Confidence", "Frustration",
        "Aggressiveness", "RiskTaking", "Opportunism", "PunishWindowDetection", "FinisherInstinct", "FeintTendency",
        "ResourceManagementIQ", "HealingPriority", "BuffPriority",
        "MeleePreference", "RangedPreference", "MagicPreference", "DebuffPreference", "StatusEffectUsage",
        "DodgingTendency", "BlockingTendency", "CounterAttack", "RetreatInstinct", "ChaseInstinct",
        "Pacing", "BurstUsage", "ComboUsage", "AttackPatternEntropy", "CooldownVariance", "TempoAdaptiveness", "AntiRepetitionTrigger",
        "Adaptiveness", "LearningFactor", "PhasePatternShift", "PhaseMemoryRetention",
        "MoveWeightBias", "ChainTendency", "RecoveryPunishBias", "ErrorTolerance"
    };

        public int MaxHp { get; private set; }

        private int
            _hp, _mana, _stamina,
            _strength, _intelligence, _defence, _speed, _wisdom, //wisdom definisce un moltiplicatore che definisce la ricarica delle abilità e magie
            _maxEquipLoad, _currentEquipWeight,
            _trueDefence, // una difesa base per qualsiasi condizione tipo per attacchi che bypassano armatura va ad aggiungersi a defence
            _arsenalSize,//ATTENZIONE: arsenalSize non definisce per forza il numero di armi che il boss ha sul momento, ovviamente deve essere sempre =>
            _currentWeaponsNumber,
            _currentToken; //token mantenuti dalla fase di mutazione precedente (per risparmiare)

        private float
             _hpRegen, _manaRegen, _staminaRegen,
            _atkspeed,//atkSpeed definito da strength, weapon weight e speed, e currentWeight
            _accuracy; //accuracy definito da speed, intelligence e wisdom

        private Dictionary<string, float?> _currentStatusEffect; //se il float è null o 0 viene rimosso dal dictionary, se arriva ad 1 lo stato è innescato e parte l'effetto
        private List<string?> _EffectResistance, _EffectWeakness;

        private Dictionary<string, float> _aiPattern = new Dictionary<string, float>
        {
            // === STATI DINAMICI ===
            { "AggroLevel", 0f },             // cresce se il player continua ad attaccare / amplifica Aggressiveness
            { "FocusTargetLock", 0f },        // quanto resta bloccato su un target/decisione prima di ricalcolare
            { "DecisionLatency", 0f },        // tempo simulato di riflessione: alto = lento a reagire, basso = istantaneo
            { "RageMeter", 0f },              // aumenta aggressività se subisce danni senza reagire
            { "Confidence", 0f },             // influenza risk taking/difesa in base ai successi/fallimenti
            { "Frustration", 0f },            // cresce se sbaglia spesso, aumenta probabilità di burst o mosse disperate
            // === AGGRESSIVITÀ E RISCHIO ===
            { "Aggressiveness", 0f },         // tendenza ad attaccare vs difendere
            { "RiskTaking", 0f },             // propensione a esporsi pur di infliggere danno
            { "Opportunism", 0f },            // sfrutta i momenti di vulnerabilità del player (tipo quando è stunlockato oppure si sta curando)
            { "PunishWindowDetection", 0f },  // capacità di colpire dopo errori/lentezze del player
            { "FinisherInstinct", 0f },       // quanto tenta la kill quando il player è low hp, più è alta più la tenterà, a 0 tenterà comunque ma meno spesso
            { "FeintTendency", 0f },          // probabilità di fingere un attacco per baitare
            // === GESTIONE RISORSE ===
            { "ResourceManagementIQ", 0f },   // gestione unificata di mana/stamina/abilità
            { "HealingPriority", 0f },        // quanto considera vitale curarsi
            { "BuffPriority", 0f },           // priorità ai buff rispetto agli attacchi
            // === STILE DI COMBATTIMENTO ===
            { "MeleePreference", 0f },        // preferenza per attacchi corpo a corpo
            { "RangedPreference", 0f },       // preferenza per attacchi a distanza
            { "MagicPreference", 0f },        // preferenza per magie
            { "DebuffPreference", 0f },       // preferenza per infliggere debuff
            { "StatusEffectUsage", 0f },      // uso attivo di status effect (poison, burn, ecc.)
            // === COMPORTAMENTI DIFENSIVI ===
            { "DodgingTendency", 0f },        // probabilità di schivare
            { "BlockingTendency", 0f },       // probabilità di bloccare
            { "CounterAttack", 0f },          // propensione a counter immediati
            { "RetreatInstinct", 0f },        // propensione a indietreggiare
            { "ChaseInstinct", 0f },          // propensione a inseguire il player
            // === RITMO E VARIABILITÀ ===
            { "Pacing", 0f },                 // gestione del ritmo (attacco → pausa → attacco)
            { "BurstUsage", 0f },             // frequenza di combo esplosive
            { "ComboUsage", 0f },             // frequenza nell'uso delle combo normali
            { "AttackPatternEntropy", 0f },   // variabilità nelle scelte di attacco
            { "CooldownVariance", 0f },       // randomizzazione tempi di ricarica
            { "TempoAdaptiveness", 0f },      // rompe la ripetitività del player cambiando timing
            { "AntiRepetitionTrigger", 0f },  // counter contro spam del player
            // === ADATTIVITÀ ===
            { "Adaptiveness", 0f },           // capacità di adattarsi al player
            { "LearningFactor", 0f },         // quanto velocemente impara a rispondere a pattern ripetuti
            { "PhasePatternShift", 0f },      // quanto cambia il pattern a ogni fase
            { "PhaseMemoryRetention", 0f },   // quanto "ricorda" le strategie del player tra le fasi
            // === CONTROLLO MOSSE ===
            { "MoveWeightBias", 0f },         // preferenza verso mosse specifiche
            { "ChainTendency", 0f },          // tendenza a concatenare più azioni di fila
            { "RecoveryPunishBias", 0f },     // propensione a colpire durante la recovery del player
            { "ErrorTolerance", 0f }          // livello di imperfezione simulata: più alto = IA meno perfetta
        };

        private BossClass _bossClass;
        private List<Weapon?> _currentWeapons;
        private List<Skill?> _currentSkills;

        #region Proprietà

        public string ID
        {
            get { return _iD; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value not acceptable");
                _iD = value;
            }
        }

        public bool IsAlive
        {
            get { return _isAlive; }
            set { _isAlive = value; }
        }

        public int Hp
        {
            get { return _hp; }
            set
            {
                if (value < 0)
                    _hp = 0;
                else if (value > MaxHp)
                    _hp = MaxHp;
                else
                    _hp = value;

                if (_hp == 0) _isAlive = false;
            }
        }

        public int Mana
        {
            get { return _mana; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _mana = value;
            }
        }

        public int Stamina
        {
            get { return _stamina; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _stamina = value;
            }
        }

        public int Strength
        {
            get { return _strength; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _strength = value;
                UpdateCombatStats();
            }
        }

        public int Intelligence
        {
            get { return _intelligence; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _intelligence = value;
                UpdateCombatStats();
            }
        }

        public int Defence
        {
            get { return _defence; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _defence = value;
            }
        }

        public int Speed
        {
            get { return _speed; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _speed = value;
                UpdateCombatStats();
            }
        }

        public int Wisdom
        {
            get { return _wisdom; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _wisdom = value;
                UpdateCombatStats();
            }
        }

        public int TrueDefence
        {
            get { return _trueDefence; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _trueDefence = value;
            }
        }

        public int MaxEquipLoad
        {
            get { return _maxEquipLoad; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _maxEquipLoad = value;
                // Se il nuovo max equip load è inferiore al peso attuale, aggiusta il peso attuale
                if (_currentEquipWeight > _maxEquipLoad)
                {
                    _currentEquipWeight = _maxEquipLoad;
                }
            }
        }

        public int CurrentEquipWeight
        {
            get { return _currentEquipWeight; }
            set
            {
                if (value < 0 || value > _maxEquipLoad)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _currentEquipWeight = value;
                UpdateCombatStats();
            }
        }

        public int ArsenalSize
        {
            get { return _arsenalSize; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _arsenalSize = value;
                // Se il nuovo arsenale size è inferiore al numero di armi attuali, rimuovi le armi in eccesso
                while (_currentWeaponsNumber > _arsenalSize && _currentWeapons.Count > 0)
                {
                    RemoveWeapon(_currentWeapons.Last());
                }
            }
        }

        public int CurrentWeaponsNumber
        {
            get { return _currentWeaponsNumber; }
            set
            {
                if (value < 0 || value > _arsenalSize)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _currentWeaponsNumber = value;
            }
        }

        public int CurrentToken
        {
            get { return _currentToken; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _currentToken = value;
            }
        }

        public Dictionary<string, float?> CurrentStatusEffect
        {
            get { return _currentStatusEffect; }
            set { _currentStatusEffect = value ?? new Dictionary<string, float?>(); }
        }

        public List<string?> EffectResistance
        {
            get { return _EffectResistance; }
            set { _EffectResistance = value ?? new List<string?>(); }
        }

        public List<string?> EffectWeakness
        {
            get { return _EffectWeakness; }
            set { _EffectWeakness = value ?? new List<string?>(); }
        }

        public Dictionary<string, float> AiPattern
        {
            get { return _aiPattern; }
            set { _aiPattern = value ?? new Dictionary<string, float>(); }
        }

        public BossClass BossClass
        {
            get { return _bossClass; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value not acceptable");
                _bossClass = value;
            }
        }

        public List<Weapon?> CurrentWeapons
        {
            get { return _currentWeapons; }
            set
            {
                _currentWeapons = value ?? new List<Weapon?>();
                _currentWeaponsNumber = _currentWeapons.Count(w => w != null);
                _currentEquipWeight = _currentWeapons.Where(w => w != null).Sum(w => w.Weight);
                UpdateCombatStats();
            }
        }

        public List<Skill?> CurrentSkills
        {
            get { return _currentSkills; }
            set { _currentSkills = value ?? new List<Skill?>(); }
        }

        public float HpRegen
        {
            get { return _hpRegen; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _hpRegen = value;
            }
        }

        public float ManaRegen
        {
            get { return _manaRegen; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _manaRegen = value;
            }
        }

        public float StaminaRegen
        {
            get { return _staminaRegen; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _staminaRegen = value;
            }
        }

        public float AtkSpeed
        {
            get { return _atkspeed; }
            private set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _atkspeed = value;
            }
        }

        public float Accuracy
        {
            get { return _accuracy; }
            private set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _accuracy = value;
            }
        }

        #endregion Proprietà

        #region Costruttori

        public Boss(string iD,
    bool isAlive,
    int hp, int mana, int stamina,
    int strength, int intelligence, int defence, int speed, int wisdom, int trueDefence,
    int maxEquipLoad, int currentEquipWeight,
    int arsenalSize,
    int currentToken,
    Dictionary<string, float?> currentStatusEffect,
    List<string?> effectResistance,
    List<string?> effectWeakness,
    Dictionary<string, float> aiPattern,
    BossClass bossClass,
    List<Weapon?> currentWeapons,
    List<Skill?> currentSkills)
        {
            _iD = iD;
            _isAlive = isAlive;
            MaxHp = hp;
            _hp = hp;
            _mana = mana;
            _stamina = stamina;
            _strength = strength;
            _intelligence = intelligence;
            _defence = defence;
            _speed = speed;
            _wisdom = wisdom;
            _trueDefence = trueDefence;
            _maxEquipLoad = maxEquipLoad;
            _currentEquipWeight = currentEquipWeight;
            _arsenalSize = arsenalSize;
            _currentToken = currentToken;
            _currentStatusEffect = currentStatusEffect;
            _EffectResistance = effectResistance;
            _EffectWeakness = effectWeakness;
            _bossClass = bossClass;
            _currentWeapons = currentWeapons;
            _currentSkills = currentSkills;

            // Assicurati che il pattern IA abbia tutte le chiavi necessarie
            _aiPattern = EnsureAllAiPatternKeys(aiPattern);

            // Inizializza il numero di armi
            _currentWeaponsNumber = _currentWeapons.Count(w => w != null);

            // Calcola le statistiche di combattimento
            UpdateCombatStats();
        }

        // Metodo per garantire che tutte le chiavi del pattern IA siano presenti
        private Dictionary<string, float> EnsureAllAiPatternKeys(Dictionary<string, float> aiPattern)
        {
            var result = new Dictionary<string, float>(aiPattern);

            foreach (var key in DefaultAiPatternKeys)
            {
                if (!result.ContainsKey(key))
                {
                    result[key] = 0f;
                }
            }

            return result;
        }

        public Boss(Boss parent)
        {
            _iD = parent._iD;
            _isAlive = parent._isAlive;
            MaxHp = parent.MaxHp;
            _hp = parent._hp;
            _mana = parent._mana;
            _stamina = parent._stamina;
            _strength = parent._strength;
            _intelligence = parent._intelligence;
            _defence = parent._defence;
            _speed = parent._speed;
            _wisdom = parent._wisdom;
            _trueDefence = parent._trueDefence;
            _maxEquipLoad = parent._maxEquipLoad;
            _currentEquipWeight = parent._currentEquipWeight;
            _arsenalSize = parent._arsenalSize;
            _currentToken = parent._currentToken;
            _currentStatusEffect = new Dictionary<string, float?>(parent._currentStatusEffect);
            _EffectResistance = new List<string?>(parent._EffectResistance);
            _EffectWeakness = new List<string?>(parent._EffectWeakness);
            _bossClass = parent._bossClass;
            _currentWeapons = new List<Weapon?>(parent._currentWeapons);
            _currentSkills = new List<Skill?>(parent._currentSkills);

            // Assicurati che il pattern IA abbia tutte le chiavi necessarie
            _aiPattern = EnsureAllAiPatternKeys(parent._aiPattern);

            // Inizializza il numero di armi
            _currentWeaponsNumber = _currentWeapons.Count(w => w != null);

            // Calcola le statistiche di combattimento
            UpdateCombatStats();
        }

        #endregion Costruttori

        #region Metodi pubblici

        public void AddWeapon(Weapon weapon)
        {
            if (weapon == null)
                throw new ArgumentNullException("weapon cannot be null");
            if (_currentWeaponsNumber >= _arsenalSize)
                throw new InvalidOperationException("Cannot add more weapons, arsenal is full.");
            if (_currentEquipWeight + weapon.Weight > _maxEquipLoad)
                throw new InvalidOperationException("Cannot equip weapon, exceeds max equip load.");

            // Verifica se il boss può usare questo tipo di arma in base alla sua classe
            if (!_bossClass.IsWeaponAllowed(weapon.WeaponType))
                throw new InvalidOperationException($"Boss class {_bossClass.ClassName} cannot use weapon type {weapon.WeaponType}");

            _currentWeapons.Add(weapon);
            _currentWeaponsNumber++;
            _currentEquipWeight += weapon.Weight;
            UpdateCombatStats();
        }

        public void RemoveWeapon(Weapon weapon)
        {
            if (weapon == null)
                throw new ArgumentNullException("weapon cannot be null");
            if (_currentWeapons.Remove(weapon))
            {
                _currentWeaponsNumber--;
                _currentEquipWeight -= weapon.Weight;
                UpdateCombatStats();
            }
            else
            {
                throw new InvalidOperationException("Weapon not found in current weapons.");
            }
        }

        public void AddSkill(Skill skill)
        {
            if (skill == null)
                throw new ArgumentNullException("skill cannot be null");
            _currentSkills.Add(skill);
        }

        public void RemoveSkill(Skill skill)
        {
            if (skill == null)
                throw new ArgumentNullException("skill cannot be null");
            if (!_currentSkills.Remove(skill))
            {
                throw new InvalidOperationException("Skill not found in current skills.");
            }
        }

        // Metodo per aggiornare gli effetti di stato
        public void UpdateStatusEffects()
        {
            // Rimuovi gli effetti con valore null o 0
            var keysToRemove = (_currentStatusEffect
                .Where(kvp => kvp.Value == null || kvp.Value == 0)
                .Select(kvp => kvp.Key)
                .ToList());

            foreach (var key in keysToRemove)
            {
                _currentStatusEffect.Remove(key);
            }
        }

        // Metodo per applicare un effetto di stato
        public void ApplyStatusEffect(string effectName, float? value)
        {
            if (string.IsNullOrEmpty(effectName))
                throw new ArgumentNullException("effectName cannot be null");

            _currentStatusEffect[effectName] = value;
            UpdateStatusEffects();
        }

        // Metodo per rimuovere un effetto di stato
        public void RemoveStatusEffect(string effectName)
        {
            if (string.IsNullOrEmpty(effectName))
                throw new ArgumentNullException("effectName cannot be null");

            _currentStatusEffect.Remove(effectName);
        }

        // Metodo per verificare se il boss ha una resistenza a un effetto
        public bool HasResistanceTo(string effectName)
        {
            return _EffectResistance.Contains(effectName);
        }

        // Metodo per verificare se il boss ha una debolezza a un effetto
        public bool HasWeaknessTo(string effectName)
        {
            return _EffectWeakness.Contains(effectName);
        }

        // Metodo per ottenere il valore di un comportamento IA
        // Metodo per ottenere un valore del pattern IA in modo sicuro
        public float GetAiBehavior(string behaviorName)
        {
            if (_aiPattern.TryGetValue(behaviorName, out float value))
                return value;
            return 0f; // Valore predefinito se la chiave non esiste
        }

        // Metodo per impostare un valore del pattern IA
        public void SetAiBehavior(string behaviorName, float value)
        {
            _aiPattern[behaviorName] = value;
        }

        // Metodo per calcolare il danno base
        public virtual int CalculateBaseDamage()
        {
            // Danno base basato su forza e intelligenza
            float physicalDamage = _strength * 1.5f;
            float magicalDamage = _intelligence * 1.2f;

            // Combina i danni con un peso basato sulle preferenze del boss
            float meleePref = GetAiBehavior("MeleePreference");
            float magicPref = GetAiBehavior("MagicPreference");
            float totalPref = meleePref + magicPref;

            if (totalPref > 0)
            {
                return (int)((physicalDamage * meleePref + magicalDamage * magicPref) / totalPref);
            }

            return (int)physicalDamage;
        }

        // Metodo per calcolare la difesa totale
        public virtual int CalculateTotalDefense()
        {
            return _defence + _trueDefence;
        }

        // Metodo per calcolare l'efficacia di un'abilità
        public virtual float CalculateSkillEffectiveness(Skill skill)
        {
            if (skill == null) return 0f;

            // L'efficacia dipende dalle statistiche rilevanti per l'abilità
            float statMultiplier = 1f;

            // Se è un'abilità magica, usa l'intelligenza
            if (skill.ManaCost > skill.StaminaCost)
            {
                statMultiplier = 1f + (_intelligence / 100f);
            }
            // Altrimenti usa la forza
            else
            {
                statMultiplier = 1f + (_strength / 100f);
            }

            // La wisdom influenza la ricarica delle abilità
            float cooldownReduction = 1f - (_wisdom / 200f);

            return skill.BasePower * skill.Scaling * statMultiplier * cooldownReduction;
        }

        #endregion Metodi pubblici

        #region Metodi privati

        // Metodo per aggiornare le statistiche di combattimento
        private void UpdateCombatStats()
        {
            // Calcola la velocità di attacco basata su forza, velocità e peso equipaggiato
            float weightFactor = 1f - (_currentEquipWeight / (float)_maxEquipLoad * 0.5f);
            _atkspeed = (_strength * 0.1f + _speed * 0.2f) * weightFactor;

            // Calcola la precisione basata su velocità, intelligenza e wisdom
            _accuracy = (_speed * 0.3f + _intelligence * 0.2f + _wisdom * 0.1f);
        }

        public void SetMana(int value)
        {
            _mana = Math.Max(0, Math.Min(Mana, value));
        }

        public void SetStamina(int value)
        {
            _stamina = Math.Max(0, Math.Min(Stamina, value));
        }

        public void Regenerate()
        {
            Hp = Math.Min(MaxHp, Hp + (int)HpRegen);
            SetMana(Math.Min(Mana, Mana + (int)ManaRegen));
            SetStamina(Math.Min(Stamina, Stamina + (int)StaminaRegen));
        }

        // Metodo per prendere danno in modo sicuro
        public void TakeDamage(int damage)
        {
            Hp = Math.Max(0, Hp - damage);
        }

        // Metodo per usare mana in modo sicuro
        public void UseMana(int amount)
        {
            SetMana(Math.Max(0, Mana - amount));
        }

        // Metodo per usare stamina in modo sicuro
        public void UseStamina(int amount)
        {
            SetStamina(Math.Max(0, Stamina - amount));
        }

        #endregion Metodi privati
    }
}