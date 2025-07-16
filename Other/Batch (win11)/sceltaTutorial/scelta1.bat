@echo off
:INIZIO
cls

echo 1 - Mostra elenco file nella cartella
echo 2 - Crea una cartella 
echo 3 - Mostra informazioni di sistema
echo 4 - Esci dal programma

choice /c 1234 /M "Scelgi un'opzione:"

if errorlevel 4 goto USCITA
if errorlevel 3 goto INFO
if errorlevel 2 goto MAKEREPO
if errorlevel 1 goto SHOWFILE


:SHOWFILE
cls
dir
pause
goto INIZIO

:MAKEREPO
cls
set /p nomeCartella=Inserisci nome della cartella:
mkdir %nomeCartella% 
pause
goto INIZIO

:INFO
cls
echo informazioni di sistema:
systeminfo
pause
goto INIZIO

:USCITA
cls 
echo Uscita dal programma...
pause
exit