namespace ProtocolSystem;

/// <summary>
/// Che cos'e' un messaggio. Il valore numerico e' parte del protocollo:
/// una volta usato, un numero non cambia mai significato.
/// </summary>
public enum MessageType
{
    /// <summary>Testo della chat.</summary>
    Message = 100,

    /// <summary>Comando dell'utente destinato al server.</summary>
    Command = 200,

    /// <summary>Segnalazione di un problema, con il motivo nel body.</summary>
    Error = 300,

    /// <summary>Uscita volontaria dalla chat.</summary>
    Disconnect = 400,

    /// <summary>Primo messaggio del client dopo la connessione: porta il suo nome.</summary>
    Handshake = 500
}