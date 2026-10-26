using System;
using System.Collections.Generic;
using System.Linq;

namespace BossGeneratorLib
{
    public class StatusEffect
    {
        #region Campi privati

        // Nome del tipo di effetto (es: Burning, Frostbite, Poison, Regen, DefenseUp)
        private string _effectName;

        // Categoria dell'effetto (Buff / Debuff)
        private bool _isPositive;

        private bool _isSelf; //(true isSelf / false player))

        // Parametri di impatto
        private float _value;        // Intensità dell'effetto (es: danno al secondo, quanto buff o debuff)

        private float _scaling;          // Moltiplicatore basato sulle stats del boss o dell'arma (int e wis)

        // Durata e applicazione nel tempo
        private float _duration;         // Tempo totale dell'effetto in secondi

        private float _tickInterval;     // Frequenza con cui l'effetto viene applicato (0 se istantaneo)

        // Probabilità di applicazione
        private float _applicationChance; // Probabilità che l'effetto venga applicato quando triggerato

        // Target
        private int _target;  //target stat (hp, defense, dex)

        // Condizioni o trigger speciali
        private Dictionary<string, string> _conditions; // Es: "sotto 50% HP", "solo se il bersaglio è in acqua", "all'inizio del turno"

        // Eventuali effetti aggiuntivi combinabili (opzionale)
        private List<StatusEffect> _linkedEffects; // Per creare catene, es: Burning → Ignite → Explosion

        #endregion Campi privati

        #region Proprietà

        public string EffectName
        {
            get { return _effectName; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value not acceptable");
                _effectName = value;
            }
        }

        public bool IsPositive
        {
            get { return _isPositive; }
            set { _isPositive = value; }
        }

        public bool IsSelf
        {
            get { return _isSelf; }
            set { _isSelf = value; }
        }

        public int Target
        {
            get { return _target; }
            set { _target = value; }
        }

        public float Value
        {
            get { return _value; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _value = value;
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

        public float Duration
        {
            get { return _duration; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _duration = value;
            }
        }

        public float TickInterval
        {
            get { return _tickInterval; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _tickInterval = value;
            }
        }

        public float ApplicationChance
        {
            get { return _applicationChance; }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("value not acceptable");
                _applicationChance = value;
            }
        }

        public Dictionary<string, string> Conditions
        {
            get { return _conditions; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value not acceptable");
                _conditions = value;
            }
        }

        public List<StatusEffect> LinkedEffects
        {
            get { return _linkedEffects; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value not acceptable");
                _linkedEffects = value;
            }
        }

        // Proprietà calcolate
        public bool IsInstantaneous
        {
            get { return _tickInterval == 0; }
        }

        public bool IsPermanent
        {
            get { return _duration == 0; }
        }

        public int TotalTicks
        {
            get
            {
                if (IsInstantaneous || IsPermanent)
                    return 1;
                return (int)(_duration / _tickInterval);
            }
        }

        #endregion Proprietà

        #region Costruttori

        public StatusEffect(string effectName, bool isPositive, bool isSelf, float value, float scaling, float duration, float tickInterval, float applicationChance, int target, Dictionary<string, string> conditions, List<StatusEffect> linkedEffects)
        {
            _effectName = effectName;
            _isPositive = isPositive;
            _isSelf = isSelf;
            _value = value;
            _scaling = scaling;
            _duration = duration;
            _tickInterval = tickInterval;
            _applicationChance = applicationChance;
            _target = target;
            _conditions = conditions;
            _linkedEffects = linkedEffects;
        }

        public StatusEffect(StatusEffect effect)
        {
            _effectName = effect._effectName;
            _isPositive = effect._isPositive;
            _isSelf = effect._isSelf;
            _value = effect._value;
            _scaling = effect._scaling;
            _duration = effect._duration;
            _tickInterval = effect._tickInterval;
            _applicationChance = effect._applicationChance;
            _target = effect._target;
            _conditions = new Dictionary<string, string>(effect._conditions);
            _linkedEffects = new List<StatusEffect>(effect._linkedEffects);
        }

        public StatusEffect()
        {
            _effectName = "Unknown";
            _isPositive = true;
            _isSelf = false;
            _value = 0;
            _scaling = 0;
            _duration = 0;
            _tickInterval = 0;
            _applicationChance = 0;
            _target = 0;
            _conditions = new Dictionary<string, string>();
            _linkedEffects = new List<StatusEffect>();
        }

        #endregion Costruttori

        #region Metodi pubblici

        // Metodo per applicare l'effetto a un boss
        public bool ApplyTo(Boss boss, BattleSystem.BattleResources resources = null)
        {
            // Verifica se l'effetto può essere applicato basandosi sulle condizioni
            if (!CanApply(boss, resources))
                return false;

            // Verifica la probabilità di applicazione
            if (_random.NextDouble() > _applicationChance)
                return false;

            // Applica l'effetto in base al target
            switch (_target)
            {
                case 0: // HP
                    int hpChange = (int)(_value * _scaling);
                    if (_isPositive)
                    {
                        boss.Hp = Math.Min(boss.Hp + hpChange, GetMaxHp(boss));
                    }
                    else
                    {
                        boss.Hp = Math.Max(1, boss.Hp - hpChange);
                    }
                    break;

                case 1: // Difesa
                    if (resources != null)
                    {
                        if (_isPositive)
                        {
                            resources.DefenseModifier += _value;
                        }
                        else
                        {
                            resources.DefenseModifier -= _value;
                        }
                    }
                    break;

                case 2: // Velocità
                    if (resources != null)
                    {
                        if (_isPositive)
                        {
                            resources.SpeedModifier += _value;
                        }
                        else
                        {
                            resources.SpeedModifier -= _value;
                        }
                    }
                    break;
            }

            // Applica gli effetti collegati
            foreach (var linkedEffect in _linkedEffects)
            {
                linkedEffect.ApplyTo(boss, resources);
            }

            return true;
        }

        // Metodo per verificare se l'effetto può essere applicato
        public bool CanApply(Boss boss, BattleSystem.BattleResources resources = null)
        {
            // Verifica le condizioni
            foreach (var condition in _conditions)
            {
                if (!EvaluateCondition(condition.Key, condition.Value, boss, resources))
                    return false;
            }

            // Verifica se il boss ha resistenze a questo effetto
            if (boss.HasResistanceTo(_effectName))
                return false;

            return true;
        }

        // Metodo per clonare l'effetto
        public StatusEffect Clone()
        {
            return new StatusEffect(this);
        }

        // Metodo per ottenere una descrizione testuale
        public override string ToString()
        {
            string targetName = "";
            switch (_target)
            {
                case 0: targetName = "HP"; break;
                case 1: targetName = "DEF"; break;
                case 2: targetName = "SPD"; break;
                default: targetName = "UNKNOWN"; break;
            }

            string type = _isPositive ? "Buff" : "Debuff";
            string durationText = IsPermanent ? "Permanent" : $"{_duration}s";
            string intervalText = IsInstantaneous ? "Instant" : $"Every {_tickInterval}s";

            return $"{_effectName} ({type}) - Target: {targetName} - Value: {_value}x{_scaling} - {durationText} - {intervalText} - Chance: {(_applicationChance * 100).ToString("F1")}%";
        }

        #endregion Metodi pubblici

        #region Metodi privati

        private static Random _random = new Random();

        private bool EvaluateCondition(string conditionType, string conditionValue, Boss boss, BattleSystem.BattleResources resources)
        {
            switch (conditionType.ToLower())
            {
                case "hp_below":
                    if (float.TryParse(conditionValue, out float hpThreshold))
                    {
                        float hpPercentage = boss.Hp / (float)GetMaxHp(boss);
                        return hpPercentage < hpThreshold;
                    }
                    break;

                case "hp_above":
                    if (float.TryParse(conditionValue, out hpThreshold))
                    {
                        float hpPercentage = boss.Hp / (float)GetMaxHp(boss);
                        return hpPercentage > hpThreshold;
                    }
                    break;

                case "has_status":
                    return boss.CurrentStatusEffect.ContainsKey(conditionValue);

                case "not_has_status":
                    return !boss.CurrentStatusEffect.ContainsKey(conditionValue);

                case "random":
                    if (float.TryParse(conditionValue, out float chance))
                    {
                        return _random.NextDouble() < chance;
                    }
                    break;
            }

            // Se la condizione non è riconosciuta, restituisce true
            return true;
        }

        private int GetMaxHp(Boss boss)
        {
            // Metodo helper per ottenere l'HP massimo di un boss
            // In una implementazione reale, questo potrebbe considerare i buff attivi
            return boss.Hp; // Semplificato per questo esempio
        }

        #endregion Metodi privati
    }
}