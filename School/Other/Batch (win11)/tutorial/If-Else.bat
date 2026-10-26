@echo off
rem script controllare se il numero è pari o dispari
set /p numero=inserisci il numero:
set /a resto = %numero% %% 2
IF %resto% EQU 0 (
echo il numero e' pari
) else (
echo il numero e' dispari)

cls

curl parrot.live

pause