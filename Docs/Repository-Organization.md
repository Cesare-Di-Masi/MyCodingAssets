# Riorganizzazione e inventario di MyCodingAssets

Data dell'audit e della riorganizzazione: 2026-10-26.

## Stato iniziale verificato

- Repository Git alla radice `C:\Users\Cesare\Desktop\MyCodingAssets`, branch `main`.
- Stato Git iniziale pulito; 1.981 voci nell'indice.
- Inventario su disco fuori da `.git`: 2.132 file, 659 directory e 126.102.233 byte.
- I file `.gitignore` erano 13. Le estensioni prevalenti erano `.cs` (565), `.txt` (294), `.csproj` (154), `.py` (142), `.js` (110), `.cpp` (87), `.xaml` (77), `.pdf` (70), `.sln` e `.c` (53 ciascuno).
- Sono presenti 154 progetti C#, 56 soluzioni (`.sln` e `.slnx`) e 89 riferimenti `ProjectReference`.
- Il README alla radice conteneva soltanto `MyCodingAsset`; il titolo è conservato qui.

## Struttura attuale

- `School/`: materiale scolastico, raggruppato per linguaggio o disciplina.
- `Personal/`: progetti personali raggruppati per linguaggio.
- `C#/`: asset e strumenti .NET condivisi rimasti fuori dalle raccolte scolastiche e personali.
- `Python/`, `C++/`, `HTML/` e `Kotlin/`: contenitori originali conservati; i gruppi classificati sono stati ricollocati in `School/` o `Personal/`.
- `Archive/Other/`: materiale misto residuo (script, bot, JSON, shell e progetti musicali) mantenuto insieme perché la classificazione più specifica non è supportata da evidenze sufficienti.
- `Docs/`: documentazione, snapshot e questa mappa.
- `REPO/`: applicazione autonoma con frontend JavaScript e backend Node.js/Express.
- `.vscode/`, `manager.bat` e le configurazioni alla radice sono rimasti nelle posizioni originali.

## Gruppi spostati

| Percorso iniziale | Percorso attuale | Criterio |
|---|---|---|
| `C#/School` | `School/C#` | Raccolta esplicitamente scolastica |
| `C++/OIS` | `School/C++/OIS` | Esercizi e gare OIS |
| `HTML/School` | `School/HTML` | Raccolta scolastica, inclusi progetti e configurazioni correlate |
| `Kotlin/School` | `School/Kotlin` | Archivi di esercizi scolastici; gli ZIP non sono stati estratti |
| `Python/Olicyber` | `School/Python/Olicyber` | Materiali di gara scolastici |
| `Other/Cisco Packet Tracer` | `School/Other/Cisco Packet Tracer` | Cartella organizzata per anno scolastico ed esercizi |
| `C#/Personal` | `Personal/C#` | Raccolta esplicitamente personale |
| `Python/Personal` | `Personal/Python` | Raccolta esplicitamente personale |
| `Other/JavaScript/Personal` | `Personal/JavaScript` | Sottocartella esplicitamente personale |

La mappa file per file è in `Repository-File-Moves.tsv`, con colonne `From` e `To` relative alla radice. Dopo il completamento della seconda ricollocazione registra 2.012 file. I contenuti dei file spostati non sono stati modificati.

| Percorso iniziale | Percorso attuale | Criterio |
|---|---|---|
| `HTML/Personal` | `Personal/HTML` | Progetti esplicitamente personali |
| `Other/Assembly (x86)` | `School/Other/Assembly (x86)` | Esercizi e verifiche |
| `Other/Batch (win11)` | `School/Other/Batch (win11)` | Esercizi e tutorial didattici |
| `Other/Docker` | `School/Other/Docker` | Serie di laboratori numerati |
| `Other` residuo | `Archive/Other` | Raccolta mista mantenuta integra |

Il gitlink vuoto `Other/Docker/php-example` è stato ricollocato in `School/Other/Docker/php-example` insieme alla cartella Docker; non contiene un repository annidato inizializzato e resta senza mappatura in `.gitmodules`.

## Regole `.gitignore`

Il `.gitignore` principale già copriva .NET, Visual Studio, Python, Node.js, cache e output comuni; le regole locali dei 12 `.gitignore` annidati sono state mantenute. Sono state aggiunte regole mirate per artefatti JVM/Kotlin (`*.class`, `.kotlin/`), directory di build CMake (`cmake-build-*`) e output di test Python (`.tox/`, `.nox/`, `.coverage`, `.coverage.*`, `htmlcov/`). Le regole `CMakeFiles/` e `CMakeCache.txt` erano già presenti. Nessun file è stato rimosso dall'indice.

Nel manager `REPO`, lo stato persistente e i token ora finiscono nella directory dati privata dell'utente, fuori dal repository indicizzato. `REPO/.gitignore` esclude inoltre `/backend/data/` come protezione per residui o dati locali delle versioni precedenti; le regole `data/...` errate sono state rimosse.

## Correzioni al Repo Command Center

- La vista file ora considera il repository stesso un percorso interno; prima il percorso relativo vuoto della radice era rifiutato.
- Le scansioni di file, dipendenze, duplicati, indice e watcher ignorano i link simbolici, evitando cicli e attraversamenti involontari.
- Il bypass di autenticazione locale usa l'indirizzo socket effettivo e non l'header `Host`, che il client può falsificare.
- Il server si lega a `127.0.0.1` per default. Per un host non locale richiede `auth.enabled`; il token generato è conservato nello stato privato dell'utente e non viene scritto in `config.json` né stampato.
- Le impostazioni di Ollama salvate dal pannello si applicano anche al processo già avviato.
- Le aggiunte al `.gitignore` del repository gestito validano il formato e non scambiano corrispondenze parziali per regole già presenti.
- Il README del manager usa la directory reale `REPO` e documenta l'accesso locale/LAN.

## Elementi conservati e anomalie

- Nessun file o contenuto è stato eliminato. Le copie identiche sono state conservate.
- Un confronto SHA-256 iniziale, limitato ai file non vuoti e senza le directory `bin`, `obj` e `cache`, ha trovato 89 gruppi di contenuti identici (212 copie aggiuntive). L'identità degli hash non implica che gli esercizi o le versioni siano intercambiabili.
- Gli output di compilazione già presenti sono stati mantenuti: inizialmente 31 file sotto `bin`, 121 sotto `obj` e 5 sotto `cache`.
- `Other/Docker/php-example` è una directory vuota registrata nell'indice come gitlink, ma non esiste una mappatura per essa in `.gitmodules`. È stata lasciata intatta; non sono state usate fonti remote.
- `Docs/RepoStructure/repoTree.json` è stato conservato come snapshot preesistente. Contiene nomi e percorsi che già non corrispondevano alla struttura auditata, quindi non è stato sovrascritto.
- `Personal/Python/Utilities/Utils/ESP32 TRNG tester/master_key.sec` è un possibile file contenente materiale di chiave; il contenuto non è stato letto. Il file è già tracciato da Git, quindi una regola ignore non lo toglierebbe dall'indice.
- Non sono stati esaminati backup esterni né recuperati file.

## Ripristino dei percorsi precedenti

Usare `Repository-File-Moves.tsv` come mappa inversa: per ogni riga, spostare `To` di nuovo in `From`, creando prima la directory genitore originale. Verificare che ogni destinazione `From` sia libera prima dello spostamento; non sovrascrivere eventuali file creati successivamente. Per ripristinare anche il gruppo di cartelle miste, riportare prima `Archive/Other` a `Other`; quindi ripristinare i gruppi indicati nelle due tabelle. Il gitlink va riportato da `School/Other/Docker/php-example` a `Other/Docker/php-example`. I contenitori originali vuoti sono stati conservati.

Per le modifiche a `README.md` e `.gitignore`, confrontare il diff Git prima di ripristinarle; non usare un ripristino globale del repository.

## Verifiche finali

- Inventario su disco fuori da `.git`: 2.135 file, 665 directory e 126.415.045 byte.
- Tutte le 2.012 destinazioni elencate nella mappa esistono e nessun file mappato è rimasto al percorso iniziale.
- I 1.860 file spostati già tracciati sono stati confrontati con gli oggetti Git tenendo conto delle regole di normalizzazione: zero differenze. Gli altri 152 file spostati erano ignorati/non tracciati.
- `node --check` ha analizzato sintatticamente gli 11 file JavaScript modificati; i tre JSON di configurazione/lockfile sono validi; `git diff --check` non ha rilevato errori di spaziatura.
- Il branch è rimasto `main`; lo stato segnala 1.861 percorsi sorgente tracciati come rimossi, 1.863 percorsi/directory nuovi, 14 file modificati e zero conflitti. I percorsi sorgente sono stati spostati e sono recuperabili dalla mappa; nulla è stato messo in staging.
- Non sono stati eseguiti test, build, script dell'applicazione, installazioni, operazioni remote o commit.

## Riordino Python e contenitori vuoti (2026-10-26)

- I progetti personali Python sono stati raggruppati in Personal/Python/Projects (Auto-OIS, TerrainGenerator, AI, Cyber e Other).
- Gli esperimenti sono in Personal/Python/Experiments (Fun, Improving e RandomScripts).
- Gli strumenti sono in Personal/Python/Utilities (Utils, FilesToOneFile.py e FileToOneFolder.py).
- La raccolta storica Scarted è stata conservata sotto Archive/Python/Scarted; nessun file è stato eliminato.
- Sono stati rimossi soltanto i contenitori vuoti; il gitlink vuoto School/Other/Docker/php-example è stato conservato.
- Le nuove righe di percorso sono state aggiunte a Repository-File-Moves.tsv per consentire il ripristino dei percorsi precedenti.
