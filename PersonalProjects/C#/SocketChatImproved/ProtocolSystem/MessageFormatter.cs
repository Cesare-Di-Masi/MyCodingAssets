using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace ProtocolSystem;

/// <summary>
/// Converte Message da/verso testo. Non conosce socket, buffer o byte e non decide
/// dove finisce un messaggio nello stream (framing): lavora su un messaggio completo.
///
/// Formato:
///   Chiave: valore        (una riga per campo, solo "\n" come fine riga)
///   ...
///   Length: n             (sempre presente, numero di caratteri del body)
///   (riga vuota)
///   body                  (esattamente Length caratteri, puo' contenere qualunque cosa)
///
/// Campi: Type e Length obbligatori; Sender, Recipient e Date opzionali.
///
/// Regole del parser:
///   - le chiavi non distinguono maiuscole/minuscole, gli spazi attorno ai valori sono ignorati
///   - chiave sconosciuta: ignorata; chiave duplicata: errore
///   - mai eccezioni sull'input: TryParse restituisce false con il motivo
/// </summary>
public static class MessageFormatter
{
    private const int MaxHeaderLength = 1000;
    private const string HeaderEnd = "\n\n";
    private const string DateFormat = "O"; // ISO 8601, sempre in UTC in scrittura

    private const string TypeKey = "Type";
    private const string SenderKey = "Sender";
    private const string RecipientKey = "Recipient";
    private const string DateKey = "Date";
    private const string LengthKey = "Length";

    /// <summary>
    /// Da Message a testo. I messaggi in uscita li costruisce il nostro codice,
    /// quindi un messaggio non valido e' un errore di programmazione: lancia ArgumentException.
    /// </summary>
    public static string Format(Message message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (!message.TryValidate(out string error))
        {
            throw new ArgumentException($"Messaggio non valido: {error}", nameof(message));
        }

        var builder = new StringBuilder();

        AppendField(builder, TypeKey, ((int)message.Type).ToString(CultureInfo.InvariantCulture));

        if (!string.IsNullOrEmpty(message.Sender))
        {
            AppendField(builder, SenderKey, message.Sender);
        }

        if (!string.IsNullOrEmpty(message.Recipient))
        {
            AppendField(builder, RecipientKey, message.Recipient);
        }

        if (message.Date.HasValue)
        {
            AppendField(builder, DateKey, message.Date.Value.ToUniversalTime().ToString(DateFormat, CultureInfo.InvariantCulture));
        }

        AppendField(builder, LengthKey, message.Body.Length.ToString(CultureInfo.InvariantCulture));

        builder.Append('\n'); // riga vuota: fine header
        builder.Append(message.Body);

        return builder.ToString();
    }

    /// <summary>Da testo a Message. Restituisce false (con il motivo) se il testo non rispetta il protocollo.</summary>
    public static bool TryParse(string text, [NotNullWhen(true)] out Message? message, out string error)
    {
        message = null;

        if (string.IsNullOrEmpty(text))
        {
            return Fail("Messaggio vuoto", out error);
        }

        if (text.Length > MaxHeaderLength + Message.MaxBodyLength + HeaderEnd.Length)
        {
            return Fail("Messaggio troppo lungo", out error);
        }

        // 1. Header e body si separano alla prima riga vuota
        int separatorIndex = text.IndexOf(HeaderEnd, StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return Fail("Separatore tra header e body mancante", out error);
        }

        if (separatorIndex == 0)
        {
            return Fail("Header vuoto", out error);
        }

        if (separatorIndex > MaxHeaderLength)
        {
            return Fail("Header troppo lungo", out error);
        }

        string header = text.Substring(0, separatorIndex);
        string body = text.Substring(separatorIndex + HeaderEnd.Length);

        // 2. Righe dell'header -> coppie chiave/valore
        if (!TryReadFields(header, out Dictionary<string, string> fields, out error))
        {
            return false;
        }

        // 3. Campi obbligatori
        if (!fields.TryGetValue(TypeKey, out string? typeText)
            || !int.TryParse(typeText, NumberStyles.None, CultureInfo.InvariantCulture, out int typeValue)
            || !Enum.IsDefined(typeof(MessageType), typeValue))
        {
            return Fail("Type mancante o sconosciuto", out error);
        }

        if (!fields.TryGetValue(LengthKey, out string? lengthText)
            || !int.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out int length))
        {
            return Fail("Length mancante o non valido", out error);
        }

        if (length != body.Length)
        {
            return Fail($"Length dichiara {length} caratteri ma il body ne ha {body.Length}", out error);
        }

        // 4. Campi opzionali
        fields.TryGetValue(SenderKey, out string? sender);
        fields.TryGetValue(RecipientKey, out string? recipient);

        DateTimeOffset? date = null;
        if (fields.TryGetValue(DateKey, out string? dateText))
        {
            if (!DateTimeOffset.TryParseExact(dateText, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dateValue))
            {
                return Fail("Date non valida (atteso ISO 8601)", out error);
            }
            date = dateValue;
        }

        // 5. Costruzione e validazione delle regole del protocollo
        message = new Message
        {
            Type = (MessageType)typeValue,
            Sender = NullIfEmpty(sender),
            Recipient = NullIfEmpty(recipient),
            Date = date,
            Body = body
        };

        if (!message.TryValidate(out error))
        {
            message = null;
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryReadFields(string header, out Dictionary<string, string> fields, out string error)
    {
        fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string line in header.Split('\n'))
        {
            int colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                return Fail("Riga di header non valida: chiave o due punti mancanti", out error);
            }

            string key = line.Substring(0, colonIndex).Trim();
            string value = line.Substring(colonIndex + 1).Trim();

            if (key.Length == 0)
            {
                return Fail("Riga di header non valida: chiave vuota", out error);
            }

            if (!fields.TryAdd(key, value))
            {
                return Fail($"Campo duplicato: {key}", out error);
            }
        }

        error = string.Empty;
        return true;
    }

    // Un valore con un a capo romperebbe la struttura dell'header
    private static void AppendField(StringBuilder builder, string key, string value)
    {
        if (value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0)
        {
            throw new ArgumentException($"Il campo {key} contiene un a capo");
        }

        builder.Append(key).Append(": ").Append(value).Append('\n');
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;

    private static bool Fail(string reason, out string error)
    {
        error = reason;
        return false;
    }
}