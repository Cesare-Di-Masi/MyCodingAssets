namespace ProtocolSystem
{
    public class MessageFormatter
    {
        /*
         * HEADER (metadati: stessi campi per tutti i messaggi)
         * Type          (obbligatorio)  MessageType, guida il parsing
         * Sender        (server)        nome della sessione, scritto dal server, mai fidarsi del client
         * Recipient     (client/server)  vuoto = tutti (broadcast), un nome = messaggio privato, server= localhost/hostname a cui si è collegati se altri
         * Date          (server)        UTC, formato ISO 8601 //generato sul momento dell'invio
         * Length        (obbligatorio se c'è il body)  lunghezza del body
         *
         * SETTINGS (righe opzionali dell'header, se assenti vale il default)
         * Keep          default: dipende dal tipo   salva il messaggio in cronologia
         * Anonymous     default: false quando c'è il forwarding si può decidere se essere anonimi o meno
         *
         * BODY
         * Message       testo della chat //SE MESSAGGIO
         * Command       nome del comando e argomenti //SE COMANDO
         * Error         motivo dell'errore //SE ERRORE
         * Disconnect    nessun body //SE DISCONNESSIONE
         */

        // REGOLE
        // - Sender e Date li scrive sempre il server: in ingresso vengono ignorati e sovrascritti
        // - Chiave sconosciuta: ignorata
        // - Parsing: mai eccezioni, TryParse restituisce false
    }
}