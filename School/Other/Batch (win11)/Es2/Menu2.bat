@echo off
:INIZIO
rem cls ripulisce tutto
cls 
echo 1 - Mostra elenco dei file nella cartella
echo 2 - Crea una cartella
echo 3 - Mostra informazioni di sistema
echo 4 - Ipconfig
echo 5 - Test di connessione verso il gateway
echo 6 - Test di connessione verso indirizzo della scuola
echo 7 - Test di instradamento pacchetti verso indirizzo della scuola
echo 8 - Esci dal programma
echo 9 _ Test Instradamento con indirizzo dato dall'utente

choice /c 123456789 /M "Scegli un'opzione: "
rem è una sorta di switch
if errorlevel 9 goto INSTRADAMENTO_PACCHETTI
if errorlevel 8 goto USCITA
if errorlevel 7 goto INSTRADAMENTO_PACCHETTI_SCUOLA
if errorlevel 6 goto GATEWAY_SCUOLA
if errorlevel 5 goto GATEWAY
if errorlevel 4 goto IPCONFIG
if errorlevel 3 goto INFO
if errorlevel 2 goto NUOVA_CARTELLA
if errorlevel 1 goto LISTA_FILE

:LISTA_FILE
cls
echo Elenco dei file e delle directory nella cartella
dir
pause
goto INIZIO

:NUOVA_CARTELLA
cls
set /p nome_cartella=Inserisci il nome della cartella:
rem mkdir o md creano una nuova directory, è la stessa cosa
mkdir %nome_cartella%
echo La cartella e' stata creata
pause
goto INIZIO

:INFO
cls 
echo Informazioni di sistema:
rem systeminfo da le informazioni del sistema
systeminfo
pause
goto INIZIO

:USCITA 
cls
echo Uscita dal programma
pause
exit

:IPCONFIG
cls
echo Configurazione della rete
ipconfig
pause
goto INIZIO

:GATEWAY
cls
set /p gateway=Inserisci il gateway:
ping %gateway%
pause
goto INIZIO

:GATEWAY_SCUOLA
cls
echo Test connessione scuola
ping ispascalcomandini.it
pause
goto INIZIO

:INSTRADAMENTO_PACCHETTI_SCUOLA
cls
echo Test instradamento pacchetti scuola
tracert ispascalcomandini.it
pause
goto INIZIO

:INSTRADAMENTO_PACCHETTI
cls
set /p trace=Inserire l'indirizzo di instradamento
echo instradamento %trace%
tracert %trace%
pause
goto INIZIO