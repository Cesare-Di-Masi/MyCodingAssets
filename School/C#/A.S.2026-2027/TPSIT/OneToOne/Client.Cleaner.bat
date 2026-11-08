
@echo off
title SocketChat - Stop Clients
setlocal

set "PIDFILE=%~dp0clients_pids.txt"

if not exist "%PIDFILE%" (
    echo Nessun elenco di client trovato.
    pause
    exit /b 1
)

echo Chiusura dei client...

for /f %%p in (%PIDFILE%) do (
    taskkill /PID %%p /T /F >nul 2>&1
    echo Chiuso processo %%p
)

del "%PIDFILE%"

echo.
echo [OK] Tutti i client avviati dallo script sono stati terminati.
pause
