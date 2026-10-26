# Albero e conteggio del repository

Rigenera l'albero completo e i conteggi con Node.js, senza dipendenze aggiuntive:

```sh
node Docs/RepoStructure/generate-repo-tree.js
```

Oppure, dalla cartella `REPO`, usa `npm run tree`.

I report vengono scritti in `Repository-Tree.md` e `Repository-Counts.json`.
Lo strumento include file e directory nascosti, non segue collegamenti simbolici
ed esclude `.git`, `node_modules` e `.rcc-index` (indice locale del manager).
I report generati non vengono conteggiati nel
loro stesso inventario. Le directory escluse e il conteggio per estensione sono
riportati nei risultati.
