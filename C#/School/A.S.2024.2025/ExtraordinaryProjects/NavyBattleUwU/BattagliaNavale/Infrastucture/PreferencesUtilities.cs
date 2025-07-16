using BattagliaNavale.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BattagliaNavale.Infrastucture
{
    public static class PreferencesUtilities
    {
        private static readonly JsonSerializerOptions _defaultJsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };


        public static List<PartitaStatistiche> GetFields()
        {
            var serializedSavedOrders = Preferences.Default.Get("fields", "");

            if (string.IsNullOrEmpty(serializedSavedOrders)) return new List<PartitaStatistiche>();

            try
            {
                return JsonSerializer.Deserialize<List<PartitaStatistiche>>(
                    serializedSavedOrders,
                    _defaultJsonSerializerOptions) ?? new List<PartitaStatistiche>();
            }
            catch (JsonException)
            {
                return new List<PartitaStatistiche>();
            }
        }

        public static PartitaStatistiche GetField(int id)
        {
            var savedOrders = GetFields();
            var order = savedOrders.Where(x => x.Id == id).First();

            return order;
        }

        public static void SaveField(Risultato risultatoPartita, TimeSpan tempoPartita,
                                    List<Tuple<int, int, bool>> barcheBot, List<Tuple<int, int, bool>> barchePlayer,
                                    StatoCampo[,] campoPlayer, StatoCampo[,] campoBot)
        {
            var savedFields = GetFields();

            var barcheBotDto = barcheBot.Select(b => new PosizioneBarca(b.Item1, b.Item2, b.Item3)).ToList();
            var barchePlayerDto = barchePlayer.Select(b => new PosizioneBarca(b.Item1, b.Item2, b.Item3)).ToList();

            var campoBotListed = new List<CellaCampo>();
            var campoPlayerListed = new List<CellaCampo>();

            for (int i = 0; i < campoBot.GetLength(0); i++)
            {
                for (int j = 0; j < campoBot.GetLength(1); j++)
                {
                    if (campoBot[i, j] != StatoCampo.ACQUA && campoBot[i, j] != StatoCampo.NAVE)
                        campoBotListed.Add(new CellaCampo(campoBot[i, j], i, j));

                    if (campoPlayer[i, j] != StatoCampo.ACQUA && campoPlayer[i, j] != StatoCampo.NAVE)
                        campoPlayerListed.Add(new CellaCampo(campoPlayer[i, j], i, j));
                }
            }

            savedFields.Add(new PartitaStatistiche
            {
                Id = savedFields.Count + 1,
                RisultatoPartita = risultatoPartita,
                TempoPartita = tempoPartita,
                BarcheBot = barcheBotDto,
                BarchePlayer = barchePlayerDto,
                CampoBot = campoBotListed,
                CampoPlayer = campoPlayerListed
            });

            var serializedOrders = JsonSerializer.Serialize(savedFields, _defaultJsonSerializerOptions);
            Preferences.Default.Set("fields", serializedOrders);
        }

        public static void DeleteOrder(int id)
        {
            var savedOrders = GetFields();
            savedOrders.RemoveAt(id - 1);

            var count = 1;
            foreach (var order in savedOrders)
            {
                order.Id = count;
                count++;
            }

            var serializedOrders = JsonSerializer.Serialize(savedOrders, _defaultJsonSerializerOptions);
            Preferences.Default.Set("fields", serializedOrders);
        }
    }
}
