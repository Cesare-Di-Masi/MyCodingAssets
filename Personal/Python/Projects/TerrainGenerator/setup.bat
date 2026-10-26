@echo off
setlocal enabledelayedexpansion

set VENV_DIR=.venv
set PYTHON=py -3.12

if not exist "%VENV_DIR%" (
    echo ^>^> Creazione virtual environment in %VENV_DIR%...
    %PYTHON% -m venv "%VENV_DIR%"
    if errorlevel 1 goto :error
) else (
    echo ^>^> Virtual environment gia' esistente in %VENV_DIR%.
)

call "%VENV_DIR%\Scripts\activate.bat"
if errorlevel 1 goto :error

echo ^>^> Aggiornamento pip...
python -m pip install --upgrade pip
if errorlevel 1 goto :error

echo ^>^> Installazione pacchetti...
pip install -r requirements.txt
if errorlevel 1 goto :error

echo.
echo ==========================================
echo  Ambiente pronto.
echo  Attiva:  %VENV_DIR%\Scripts\activate.bat
echo  Esegui:  python TerrainGenerator.py
echo ==========================================
exit /b 0

:error
echo.
echo ##########################################
echo # ERRORE durante il setup.               #
echo # Controlla i messaggi sopra.            #
echo ##########################################
exit /b 1