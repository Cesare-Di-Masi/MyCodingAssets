# Gestione Flotta Veicoli - Noleggio Auto

## Problema
Un'azienda di noleggio auto ha bisogno di un modulo per la gestione dei veicoli della propria flotta. 
Ogni veicolo è caratterizzato da:

- Un identificativo unico (Id)

- Una targa (LicensePlate)

- Il chilometraggio attuale (OdometerKm)

- La tariffa giornaliera di noleggio (DailyRate)

- Il livello percentuale di carburante nel serbatoio (FuelLevelPercentage)

L'azienda desidera un modello solido che:

- Impedisca la creazione di veicoli o valori incoerenti (es. targa non valida, chilometri o tariffe negative).

- Permetta di registrare i viaggi effettuati, incrementando il chilometraggio e riducendo il carburante residuo.

- Consenta di effettuare il rifornimento fino a un massimo del 100% della capienza del serbatoio.

- Fornisca metodi di utilità sintetici ed espressivi per formattare le informazioni ed evidenziare situazioni d'allarme (es. riserva carburante).

## Cosa faremo

Svilupperemo il modulo attraverso 3 stadi di maturità del codice:

Stadio 1: Modello Anemico (classi come meri contenitori di dati).

Stadio 2: Rich Domain Model (l'entità gestisce le proprie regole, ma usa ancora tipi primitivi).

Stadio 3: Rich Domain Model + Value Objects (scomparsa della Primitive Obsession).

## Obiettivi 

- Comprendere l'Incapsulamento: Passare da setter pubblici liberi a metodi di business (RecordTrip, Refuel).

- Identificare la Primitive Obsession: Rendersi conto che validare stringhe o numeri dentro l'entità principale appesantisce la classe.

- Creare Value Objects: Isolare la logica e la validazione dei concetti del dominio (LicensePlate, Money) in tipi dedicati immutabili (record).

- Sintassi Fluida: Applicare metodi di estensione semplici per formattare o leggere i dati.