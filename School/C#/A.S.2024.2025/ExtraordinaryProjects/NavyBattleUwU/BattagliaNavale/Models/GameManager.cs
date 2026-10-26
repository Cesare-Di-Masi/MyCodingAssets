namespace BattagliaNavale.Models
{
    public class GameManager
    {
        public Player Giocatore { get; private set; }
        public Bot Bot { get; private set; }
        public int[] UltimaMossaBot { get; private set; } = new int[] { 0, 0 };

        public GameManager(Player giocatore, Bot bot)
        {
            Giocatore = giocatore;
            Bot = bot;
        }

        public Tuple<Risultato, int[], int> VerificaVincitore(int playerX, int playerY)
        {
            if (playerX < 0 || playerX >= Bot.Campo.GetLength(0) ||
                playerY < 0 || playerY >= Bot.Campo.GetLength(1))
            {
                return Tuple.Create(Risultato.SOSPESO, UltimaMossaBot, -1);
            }

            if (Bot.Campo[playerX, playerY] == StatoCampo.NAVE_COLPITA ||
                Bot.Campo[playerX, playerY] == StatoCampo.ACQUA_COLPITA)
            {
                return Tuple.Create(Risultato.SOSPESO, UltimaMossaBot, -1);
            }

            int[] mossaPlayer = new int[] { playerX, playerY };
            int barcaAffondata = -1;

            if (Bot.Campo[mossaPlayer[0], mossaPlayer[1]] == StatoCampo.NAVE)
            {
                Bot.Campo[mossaPlayer[0], mossaPlayer[1]] = StatoCampo.NAVE_COLPITA;
                Bot.Contatore--;

                // Verifica se una barca è completamente affondata
                barcaAffondata = Bot.VerificaBarcaAffondata(mossaPlayer[0], mossaPlayer[1]);

                if (Bot.Contatore == 0)
                    return Tuple.Create(Risultato.PLAYER, mossaPlayer, barcaAffondata);
            }
            else if (Bot.Campo[mossaPlayer[0], mossaPlayer[1]] == StatoCampo.ACQUA)
            {
                Bot.Campo[mossaPlayer[0], mossaPlayer[1]] = StatoCampo.ACQUA_COLPITA;
            }

            int[] mossaBot = ProcessaBotMossa();

            UltimaMossaBot = mossaBot;

            // Controllo game over
            if (Giocatore.Contatore == 0)
                return Tuple.Create(Risultato.BOT, mossaBot, barcaAffondata);

            return Tuple.Create(Risultato.SOSPESO, mossaBot, barcaAffondata);
        }

        private int[] ProcessaBotMossa()
        {
            bool mossaValida = false;
            int[] mossaBot = new int[2];
            int tentativi = 0;
            const int maxTentativi = 100;

            while (!mossaValida && tentativi < maxTentativi)
            {
                tentativi++;
                mossaBot = Bot.FaiMossa();

                // va bene l'hit?
                if (mossaBot[0] < 0 || mossaBot[0] >= Giocatore.Campo.GetLength(0) ||
                    mossaBot[1] < 0 || mossaBot[1] >= Giocatore.Campo.GetLength(1))
                {
                    continue;
                }

                // gia colpito?
                if (Giocatore.Campo[mossaBot[0], mossaBot[1]] == StatoCampo.NAVE_COLPITA ||
                    Giocatore.Campo[mossaBot[0], mossaBot[1]] == StatoCampo.ACQUA_COLPITA)
                {
                    continue;
                }

                mossaValida = true;

                if (Giocatore.Campo[mossaBot[0], mossaBot[1]] == StatoCampo.NAVE)
                {
                    Giocatore.Campo[mossaBot[0], mossaBot[1]] = StatoCampo.NAVE_COLPITA;
                    Giocatore.Contatore--;

                    Bot.ultimaMossa = new int[] { mossaBot[0], mossaBot[1], 0 };
                }
                else if (Giocatore.Campo[mossaBot[0], mossaBot[1]] == StatoCampo.ACQUA)
                {
                    Giocatore.Campo[mossaBot[0], mossaBot[1]] = StatoCampo.ACQUA_COLPITA;

                    if (Bot.ultimaMossa != null)
                    {
                        Bot.ultimaMossa[2] = (Bot.ultimaMossa[2] + 1) % 4;
                    }
                }
            }

            // Se non va si va avanti 
            if (!mossaValida)
            {
                for (int i = 0; i < Giocatore.Campo.GetLength(0); i++)
                {
                    for (int j = 0; j < Giocatore.Campo.GetLength(1); j++)
                    {
                        if (Giocatore.Campo[i, j] != StatoCampo.NAVE_COLPITA &&
                            Giocatore.Campo[i, j] != StatoCampo.ACQUA_COLPITA)
                        {
                            mossaBot = new int[] { i, j };

                            if (Giocatore.Campo[i, j] == StatoCampo.NAVE)
                            {
                                Giocatore.Campo[i, j] = StatoCampo.NAVE_COLPITA;
                                Giocatore.Contatore--;
                                Bot.ultimaMossa = new int[] { i, j, 0 };
                            }
                            else
                            {
                                Giocatore.Campo[i, j] = StatoCampo.ACQUA_COLPITA;
                                Bot.ultimaMossa = null;
                            }

                            return mossaBot;
                        }
                    }
                }
            }

            return mossaBot;
        }
    }
}