## Concept
Survival game 2D minimalista ispirato a "Snap!". L'obiettivo è resistere il più a lungo possibile evitando il contatto con il mostro. Game Over immediato al contatto.

## Movimento Player
* **8 Direzioni**: Supporto WASD e Frecce.
* **Shift (Dash)**: Velocità x2.5.
* **Ctrl (Crouch)**: Velocità x0.5 (per movimenti di precisione).
* **Correzione Diagonale**: Moltiplicatore 0.707 applicato ai movimenti obliqui per mantenere la velocità uniforme.

## Intelligenza Artificiale (AI)
* **Random**: per gran parte del tempo il mostro si muove in maniera randomica ma intelligente (prende una posizione del player ogni tot secondi e gli applica un errore di 10/25%) per tutto lo schermo con una velocità fissa ma abbastanza elevata
solo le fasi definite possono inseguire il player
* **Latenza**: Il mostro non punta alla posizione attuale, ma a una passata.
* **Buffer di Memoria**: Le coordinate del player vengono salvate in un array.
* **Ritardo**: Tempo di reazione variabile tra 0.5 e 3 secondi (non costante).

## Elenco Fasi (Cambio costante - minimo definito da ogni fase)
Normale
Ghosted
Twin
Bouncer
Pac Man
Glitched
Teleporter
Mirage (1 vero e 3 falsi)
Tron


