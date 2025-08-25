using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CatteryManagerLib
{
    public static class Serializer
    {
        private static readonly string fileName = "Cattery.json";

        // Salvataggio su file
        public static void SaveToFile(Cattery cattery)
        {
            string json = JsonSerializer.Serialize(cattery);
            File.WriteAllText(fileName, json);
        }

        // Recupero del gioco
        public static Cattery? GetFile()
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            string json = File.ReadAllText(fileName);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<Cattery>(json);
        }
    }
}
