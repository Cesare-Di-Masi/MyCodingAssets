// ==UserScript==
// @name         Monster Initiative Dashboard
// @namespace    http://tampermonkey.net/
// @version      1.0
// @description  Calcola iniziativa dinamica dei mostri per Obsidian Encounter
// @author       Cesare
// @match        *://*/*
// @grant        none
// ==/UserScript==

(async function() {
    'use strict';

    // Funzione roll d10
    function rollD10(){ return Math.ceil(Math.random()*10); }

    // Chiede percorso del file encounter.md
    const encounterPath = prompt('Inserisci il path completo del file Encounter.md');
    if(!encounterPath){
        alert('Path non inserito, esco.');
        return;
    }

    // Carica il file (Node.js fs o fetch se su server locale)
    // Qui esempio Node.js (da adattare se in browser)
    const fs = require('fs');
    if(!fs.existsSync(encounterPath)){
        alert('File non trovato: ' + encounterPath);
        return;
    }

    const yamlText = fs.readFileSync(encounterPath, 'utf8');
    const jsyaml = require('js-yaml');
    const yaml = jsyaml.load(yamlText);

    // Lista dei partecipanti
    let participants = [];
    yaml.Monsters.forEach(monster => {
        if(!monster.Attacks) return;
        monster.Attacks.forEach(attack => {
            if(monster.StaminaCurrent >= attack.StaminaCost){
                const initiative = monster.BaseSpeed + attack.SpeedModifier + rollD10();
                participants.push({
                    name: monster.Name,
                    attack: attack.AttackName,
                    initiative: initiative,
                    staminaCost: attack.StaminaCost,
                    staminaCurrent: monster.StaminaCurrent,
                    staminaRegen: monster.StaminaRegen
                });
            }
        });
    });

    // Ordina per iniziativa decrescente
    participants.sort((a,b) => b.initiative - a.initiative);

    // Aggiorna stamina
    participants.forEach(p => {
        p.staminaCurrent = Math.min(p.staminaCurrent - p.staminaCost + p.staminaRegen, 100);
    });

    // Mostra tabella in console (puoi adattare per creare file output)
    console.log('| Mostro | Attacco scelto | Iniziativa | Stamina Attuale | Prossima Azione |');
    console.log('|--------|----------------|------------|----------------|----------------|');
    participants.forEach(p => {
        console.log(`| ${p.name} | ${p.attack} | ${p.initiative} | ${p.staminaCurrent} | Rigenera ${p.staminaRegen} |`);
    });
})();
