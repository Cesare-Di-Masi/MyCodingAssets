namespace MastermindLib
{
    public class GameManager
    {
        private int _codeComplexity;
        private int _codeLength;
        private Colours[] _codeSolution;
        private int _nAttempts;
        private int _nColours;
        private bool _isBotOn;
        private bool _isColorBlind;
        private bool _isAllWrong;
        private int _rightPosition;
        private int _wrongPosition;

        private IGenerator _bot;

        public GameManager(bool isColorBlind, int codeLength, int nColours, int nAttempts, int codeComplexity, IGenerator bot = null)
        {
            if (codeLength < 4 || codeLength > 12)
                throw new ArgumentOutOfRangeException("lunghezza del codice deve essere fra 4 e 12");

            if (nColours < 4 || nColours > 20)
                throw new ArgumentOutOfRangeException("il numero di colori deve essere fra 4 e 20");

            if (nAttempts < 1)
                throw new ArgumentOutOfRangeException("numero di tentativi deve essere almeno 1");

            if (codeComplexity < 1 || codeComplexity > 5)
                throw new ArgumentOutOfRangeException("complessità del codice deve essere fra 1 e 5");

            if (bot == null)
                _bot = new CodeGenerator(codeLength, nColours, codeComplexity);
            else
                _bot = bot;
            _codeSolution = _bot.generateCode();
            _nAttempts = nAttempts;
            _codeLength = codeLength;
            _nColours = nColours;
            _isColorBlind = isColorBlind;
        }

        //caso in cui la soluzione venga decisa da un player
        public GameManager(Colours[] codeSolution, bool isColorBlind, int codeLength, int nColours, int nAttempts, int codeComplexity) : this(isColorBlind, codeLength, nColours, nAttempts, codeComplexity)
        {
            _codeSolution = codeSolution;
        }

        public int NAttempts
        {
            get
            {
                return _nAttempts;
            }
        }

        public int CodeLength
        {
            get
            {
                return _codeLength;
            }
        }

        public int NColours
        {
            get
            {
                return _nColours;
            }
        }

        public int CodeComplexity
        {
            get
            {
                return _codeComplexity;
            }
        }

        public bool IsColorBlindness
        {
            get
            {
                return _isColorBlind;
            }
        }

        public int RightPosition
        {
            get
            {
                return _rightPosition;
            }
        }

        public int WrongPosition
        {
            get
            {
                return _wrongPosition;
            }
        }

        public bool IsAllWrong
        {
            get
            {
                return _isAllWrong;
            }
        }

        //stampa la soluzione del codice nel caso si sia indovinato o siano finite le vite
        public Colours[] GiveColourCode()
        {
            return _codeSolution;
        }

        public GameStatus EndOfTheTurn(Colours[] codeToCheck)
        {
            if (codeToCheck == null || codeToCheck.Length != _codeSolution.Length)
                throw new ArgumentException("il codice tentato è illegale");

            for (int i = 0; i < CodeLength; i++)
            {
                if ((int)codeToCheck[i] < 0 || (int)codeToCheck[i] > NColours - 1)
                    throw new ArgumentException("il codice inserito è illegale, colori fuori dal range massimo");
            }

            _nAttempts--;

            if (CheckGuess(codeToCheck))
            {
                return GameStatus.Won;
            }

            if (NAttempts == 0)
            {
                return GameStatus.Lost;
            }

            return GameStatus.Playing;
        }

        private bool CheckGuess(Colours[] codeToCheck)
        {
            bool correct = true;
            _rightPosition = 0;
            _wrongPosition = 0;
            Colours[] dummy = codeToCheck;
            for (int i = 0; i < codeToCheck.Length; i++)
            {
                if (codeToCheck[i] == _codeSolution[i])
                {
                    _rightPosition++;
                    dummy[i] = Colours.Null;
                }
                else
                {
                    correct = false;
                }
            }

            bool found = false;
            int counter = 0;
            Colours ColourToCheck = Colours.Null;

            for (int i = 0; i < dummy.Length; i++)
            {
                ColourToCheck = _codeSolution[i];

                while (found == false && counter < dummy.Length)
                {
                    if (ColourToCheck == dummy[counter] && dummy[counter] != Colours.Null)
                    {
                        _wrongPosition++;
                        dummy[counter] = Colours.Null;
                        found = true;
                    }
                    counter++;
                }

                found = false;
                counter = 0;
            }

            _isAllWrong = _rightPosition == 0 && _wrongPosition == 0;

            return correct;
        }
    }
}