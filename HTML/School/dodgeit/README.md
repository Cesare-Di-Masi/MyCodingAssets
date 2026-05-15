# DodgeIt

## Panoramica del gioco
DodgeIt è un gioco arcade in cui il giocatore deve evitare mostri con comportamenti differenti. Il giocatore si muove liberamente sullo schermo mentre i mostri usano l'intelligenza artificiale per inseguire, predire o rimbalzare contro i bordi della schermata.

## Meccaniche di gioco
- Il giocatore viene controllato direttamente dal mouse o dalla tastiera.
- I mostri hanno statistiche diverse: velocità, precisione, aggressività, dimensione e stile visivo.
- Ogni mostro è definito da una modalità di comportamento (AI) specifica.
- I mostri lasciano dietro di sé una scia visiva che rende l'azione più leggibile e dinamica.

## Come funziona l'AI dei mostri
L'AI dei mostri è gestita da `src/game/monster/monsterLogic/MonsterAI.js`. Le modalità registrate definiscono il comportamento di destinazione e la logica di movimento.

### Comportamenti principali
- `static`: il mostro non si muove.
- `hunt`: segue la posizione passata del giocatore, con latenze e errori calcolati dalla precisione.
- `random`: alterna spostamenti verso i bordi e attacchi diretti, con probabilità legata all'aggressività.
- `orbit`: orbita attorno al giocatore mantenendo una distanza fissa.
- `prediction`: predice la traiettoria del giocatore in base al movimento passato e cerca di intercettarlo.
- `bounce`: muove il mostro in linea retta e rimbalza sui bordi dello schermo.
- `grid`: movimento simile a Tron, con cambi di direzione basati su bordo e previsione.

## Dettaglio delle modalità corrette
### BouncerMonster
- Classe: `src/game/monster/Phases/BouncerMonster.js`
- Modalità attiva: `bounce`
- Comportamento: muove il mostro in una direzione costante e rimbalza quando tocca i bordi.
- Parametri: alta velocità, moderata aggressività, precisione bassa.

### PredictorMonster
- Classe: `src/game/monster/Phases/PredictorMonster.js`
- Modalità attiva: `prediction`
- Comportamento: calcola la velocità e la direzione del giocatore sul movimento passato e punta verso la posizione prevista.
- Parametri: alta precisione, alta aggressività, velocità moderata.

## Miglioramenti apportati
- Corretto l'uso delle modalità di comportamento per `BouncerMonster` e `PredictorMonster`.
- Ora `BouncerMonster` usa il comportamento `bounce` registrato nell'AI.
- Ora `PredictorMonster` usa il comportamento `prediction` invece di un nome non valido.

## Idee per futuri miglioramenti
1. Aggiungere nuovi tipi di mostri con AI ibrida, ad esempio un mostro che alterna `hunt` e `orbit`.
2. Gestire ostacoli e pareti interne per aumentare la varietà dei percorsi.
3. Introdurre power-up per il giocatore: aumento di velocità, scudo temporaneo, rallentamento dei mostri.
4. Migliorare l'interfaccia grafica con indicatori di pericolo e punteggi per tipo di mostro evitato.
5. Aggiungere livelli e progressione: più mostri, velocità crescente e difficoltà dinamica.

## Struttura del progetto
- `index.html`: pagina principale del gioco.
- `style.css`: stili visivi.
- `src/main.js`: entry point dell'app.
- `src/game/VisualEntity.js`: base visuale per entità con effetti, traiettorie e rendering.
- `src/game/monster/BaseMonster.js`: base per ogni mostro, estende l'AI con rendering e aggiornamenti.
- `src/game/monster/monsterLogic/MonsterAI.js`: logica principale dei comportamenti.
- `src/manager/GameManager.js`: crea e gestisce i mostri.

## Note tecniche
- Le configurazioni dei mostri vengono unite con `_mergeConfig` per mantenere valori predefiniti e sovrascrivere solo quelli specificati.
- La velocità complessiva del mostro è influenzata dall'aggressività come bonus di movimento.
- Il comportamento `bounce` usa uno stato interno per conservare la direzione di rimbalzo.

---

Buon lavoro con DodgeIt! Ora le modalità `bounce` e `prediction` sono attive e funzionanti secondo l'AI registrata.