namespace ProtocolSystem
{
    public enum MessageTypes
    {
        Handshake, //Collegamento -->Il server comunica al client il nome assegnato, e magari la versione del protocollo

        //Log, Messaggi interni (post comandi/) --> Salvati in locale tramite metodo interno server
        Message,

        Command,
        Error, //errori per i messaggi (messaggi malformarmato)
        Disconnect, //uscita volontaria del client
        Notice, //Annunci (user1 si è connesso al server)
        Acknowledge, //esito richiesta
        Ping,
        Kick,
    }
}