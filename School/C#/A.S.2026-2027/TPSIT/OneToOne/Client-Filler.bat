
@echo off
title SocketChat - Client Launcher
setlocal

set "TARGETS=%~dp0src\server\SocketChat.GUI"

set "TARGET=%~dp0src\client\SocketChat.GUI"
set "PIDFILE=%~dp0clients_pids.txt"

if not exist "%TARGET%" (
    echo [ERRORE] Cartella non trovata: %TARGET%
    pause
    exit /b 1
)

set /p N="Quante istanze vuoi avviare? "

if "%N%"=="" exit /b 1

if exist "%PIDFILE%" del "%PIDFILE%"

for /L %%i in (1,1,1) do (
    echo Avvio client %%i...

    for /f %%p in ('powershell -NoProfile -Command "$p = Start-Process cmd.exe -ArgumentList '/k dotnet run localhost 11000' -WorkingDirectory '%TARGETS%' -PassThru; $p.Id"') do (
        echo %%p>>"%PIDFILE%"
    )
)

for /L %%i in (1,1,%N%) do (
    echo Avvio client %%i...

    for /f %%p in ('powershell -NoProfile -Command "$p = Start-Process cmd.exe -ArgumentList '/k dotnet run localhost 11000' -WorkingDirectory '%TARGET%' -PassThru; $p.Id"') do (
        echo %%p>>"%PIDFILE%"
    )
)

echo.
echo [OK] Client avviati.
echo Per chiuderli tutti esegui Stop-Clients.bat
pause
