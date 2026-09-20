@echo off
REM Wrapper per l'Utilita' di pianificazione di Windows (Task Scheduler).
REM Attiva il venv, gira il bot, appende l'output a bot.log.
REM NON aprire questo file con doppio click se non per un test: usalo tramite lo scheduler.

cd /d "%~dp0"

call venv\Scripts\activate.bat

python circolari_bot.py >> bot.log 2>&1

REM Se vuoi vedere subito eventuali errori quando lo lanci a mano, decommenta la riga sotto:
REM pause