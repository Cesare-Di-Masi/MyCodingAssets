using System;
using System.Linq;

namespace ProtocolSystem;

/// <summary>
/// Modello di un messaggio. Contiene solo i dati che interessano al resto del programma:
/// la lunghezza del body e il formato sul filo sono responsabilita' del MessageFormatter.
/// </summary>
public record Message
{
    // ---- Limiti ----
    public const int MaxBodyLength = 2000;
    public const int MaxNameLength = 20;

    // ---- Valori riservati: iniziano per '@', nessun utente puo' sceglierli come nome ----
    public const char ReservedPrefix = '@';
    public const string ServerRecipient = "@server";

    // ---- HEADER ----
    public MessageType Type { get; init; }

    /// <summary>Lo scrive sempre il server (mai fidarsi del client). Null se non impostato.</summary>
    public string? Sender { get; init; }

    /// <summary>Vuoto/null = tutti (broadcast), un nome = privato, ServerRecipient = il server.</summary>
    public string? Recipient { get; init; }

    /// <summary>La scrive sempre il server, in UTC.</summary>
    public DateTimeOffset? Date { get; init; }

    // ---- BODY ----
    public string Body { get; init; } = string.Empty;

    // ---- Proprieta' derivate ----
    public bool IsBroadcast => string.IsNullOrEmpty(Recipient);
    public bool IsForServer => Recipient == ServerRecipient;

    // ---- Costruttori "guidati": l'app non assembla i campi a mano ----

    /// <summary>Messaggio di chat. Senza destinatario va a tutti, con un nome e' privato.</summary>
    public static Message Text(string text, string? recipient = null) =>
        new() { Type = MessageType.Message, Recipient = recipient, Body = text };

    /// <summary>Primo messaggio del client. Senza nome ne sceglie uno il server.</summary>
    public static Message Handshake(string? userName) =>
        new() { Type = MessageType.Handshake, Recipient = ServerRecipient, Body = userName ?? string.Empty };

    /// <summary>Uscita volontaria dalla chat.</summary>
    public static Message Disconnect() =>
        new() { Type = MessageType.Disconnect, Recipient = ServerRecipient };

    /// <summary>Segnalazione di un problema, con il motivo nel body.</summary>
    public static Message Error(string reason, string? recipient = null) =>
        new() { Type = MessageType.Error, Recipient = recipient, Body = reason };

    // ---- Operazioni lato server ----

    /// <summary>
    /// Il server sovrascrive Sender e Date con i valori veri:
    /// quello che il client ha scritto in questi campi viene ignorato.
    /// </summary>
    public Message StampedByServer(string senderName, DateTimeOffset date) =>
        this with { Sender = senderName, Date = date };

    // ---- Validazione ----

    /// <summary>Un nome utente valido: non vuoto, corto, senza spazi, non riservato.</summary>
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Length <= MaxNameLength
        && name[0] != ReservedPrefix
        && !name.Any(char.IsWhiteSpace);

    /// <summary>
    /// Controlla le regole del protocollo. Da chiamare dopo il parsing e prima di usare il messaggio.
    /// Non lancia eccezioni: l'input arriva dalla rete.
    /// </summary>
    public bool TryValidate(out string error)
    {
        if (!Enum.IsDefined(typeof(MessageType), Type))
        {
            return Fail("Tipo di messaggio sconosciuto", out error);
        }

        if (Body is null || Body.Length > MaxBodyLength)
        {
            return Fail($"Il body supera {MaxBodyLength} caratteri", out error);
        }

        if (!IsBroadcast && !IsForServer && !IsValidName(Recipient))
        {
            return Fail("Destinatario non valido", out error);
        }

        switch (Type)
        {
            case MessageType.Message:
                if (Body.Length == 0)
                {
                    return Fail("Un messaggio non puo' essere vuoto", out error);
                }
                break;

            case MessageType.Command:
                if (!IsForServer)
                {
                    return Fail("I comandi sono destinati al server", out error);
                }
                if (Body.Length == 0)
                {
                    return Fail("Un comando non puo' essere vuoto", out error);
                }
                break;

            case MessageType.Error:
                if (Body.Length == 0)
                {
                    return Fail("Un errore deve indicare il motivo", out error);
                }
                break;

            case MessageType.Disconnect:
                if (Body.Length != 0)
                {
                    return Fail("La disconnessione non ha body", out error);
                }
                break;

            case MessageType.Handshake:
                if (!IsForServer)
                {
                    return Fail("L'handshake e' destinato al server", out error);
                }
                // Body vuoto = il server genera un nome; altrimenti il body e' il nome richiesto
                if (Body.Length > 0 && !IsValidName(Body))
                {
                    return Fail("Nome utente non valido", out error);
                }
                break;
        }

        error = string.Empty;
        return true;
    }

    private static bool Fail(string reason, out string error)
    {
        error = reason;
        return false;
    }
}