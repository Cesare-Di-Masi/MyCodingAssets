@echo off

setlocal enabledelayedexpansion
set x=0
for /L %%i in (1,1,5) do (
set /a x=!x!+1
echo Iterazione %%i, valore di x: !x!
)
pause