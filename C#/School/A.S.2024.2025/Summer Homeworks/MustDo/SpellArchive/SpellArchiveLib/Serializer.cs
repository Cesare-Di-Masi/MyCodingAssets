using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpellArchiveLib
{
    public static class Serializer
    {
        private static readonly string fileName = "SpellArchive.json";

        // Salvataggio su file
        public static void SaveToFile(Archive archive)
        {
            string json = JsonSerializer.Serialize(archive);
            File.WriteAllText(fileName, json);
        }

        // Recupero del gioco
        public static Archive? GetFile()
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

            return JsonSerializer.Deserialize<Archive>(json);
        }
    }
}
