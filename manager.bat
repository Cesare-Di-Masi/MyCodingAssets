@echo off
setlocal EnableExtensions EnableDelayedExpansion

:: ---------------------------------------------------------------------------
::  REPO COMMAND CENTER - launcher
::  Place this file anywhere. It will locate the project next to itself
::  or inside a "repo-manager" subfolder.
:: ---------------------------------------------------------------------------

title Repo Command Center

:: ---- Resolve script directory (works from any CWD, handles spaces) --------
set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

:: ---- Locate project root --------------------------------------------------
set "PROJECT_DIR="
if exist "%SCRIPT_DIR%\backend\server.js" (
    set "PROJECT_DIR=%SCRIPT_DIR%"
) else if exist "%SCRIPT_DIR%\repo-manager\backend\server.js" (
    set "PROJECT_DIR=%SCRIPT_DIR%\repo-manager"
)

if not defined PROJECT_DIR (
    echo.
    echo   [X] Could not find the Repo Command Center project.
    echo       Expected "backend\server.js" next to this file
    echo       or in a "repo-manager" subfolder.
    echo.
    pause
    exit /b 1
)

pushd "%PROJECT_DIR%" || (echo   [X] Failed to enter "%PROJECT_DIR%" & pause & exit /b 1)

cls
echo.
echo   ============================================================
echo     R E P O   C O M M A N D   C E N T E R
echo   ============================================================
echo     Project: %PROJECT_DIR%
echo   ============================================================
echo.

:: ---- Check Node.js --------------------------------------------------------
where node >nul 2>&1
if errorlevel 1 (
    echo   [X] Node.js is not installed or not on PATH.
    echo       Download it from https://nodejs.org/  ^(v18+ recommended^)
    echo.
    pause
    popd
    exit /b 1
)
for /f "delims=" %%v in ('node -v') do set "NODE_VER=%%v"
echo   [ok] Node.js %NODE_VER%

:: ---- Check npm ------------------------------------------------------------
where npm >nul 2>&1
if errorlevel 1 (
    echo   [X] npm not found on PATH.
    echo.
    pause
    popd
    exit /b 1
)
for /f "delims=" %%v in ('npm -v') do set "NPM_VER=%%v"
echo   [ok] npm %NPM_VER%

:: ---- Install dependencies only when needed --------------------------------
if not exist "node_modules\express" (
    echo.
    echo   [..] Installing dependencies ^(first run, may take a minute^)...
    call npm install --no-audit --no-fund
    if errorlevel 1 (
        echo.
        echo   [X] npm install failed. Check the output above.
        pause
        popd
        exit /b 1
    )
    echo   [ok] Dependencies installed.
) else (
    echo   [ok] Dependencies already present ^(skipping install^).
)

:: ---- Sanity-check config.json ---------------------------------------------
if not exist "config.json" (
    echo.
    echo   [!] config.json is missing. Creating a default one...
    > config.json echo {
    >>config.json echo   "repoPath": "..",
    >>config.json echo   "port": 4747,
    >>config.json echo   "ignoreDirs": ["node_modules",".git","dist","build","bin","obj",".vs","__pycache__"],
    >>config.json echo   "maxSearchResults": 200,
    >>config.json echo   "largeFileThresholdMB": 10
    >>config.json echo }
    echo   [ok] config.json created. Edit "repoPath" to point at your repo.
)

:: Warn if repoPath still looks unconfigured
findstr /R /C:"\"repoPath\"[ ]*:[ ]*\"\.\.\"" config.json >nul 2>&1
if not errorlevel 1 (
    echo.
    echo   [!] "repoPath" in config.json is still set to ".." ^(default^).
    echo       If that is not your target repository, edit config.json first.
    echo.
    choice /C YN /N /M "       Continue anyway? [Y/N] "
    if errorlevel 2 (
        echo.
        echo   Aborted. Edit config.json and re-run.
        timeout /t 2 >nul
        popd
        exit /b 0
    )
)

:: ---- Read port from config.json -------------------------------------------
set "PORT=4747"
for /f "tokens=2 delims=:," %%p in ('findstr /R /C:"\"port\"" config.json') do (
    set "RAW=%%p"
    set "RAW=!RAW: =!"
    set "PORT=!RAW!"
)

:: ---- Launch ----------------------------------------------------------------
echo.
echo   [..] Starting server on port %PORT% ...
echo        Press Ctrl+C to stop.
echo.
echo   ------------------------------------------------------------
echo.

:: Open browser shortly after the server boots
start "" /b cmd /c "timeout /t 2 >nul & start "" http://localhost:%PORT%"

:: Run the server in the foreground so Ctrl+C shuts it down cleanly
call node backend\server.js
set "EXITCODE=%errorlevel%"

echo.
if not "%EXITCODE%"=="0" (
    echo   [X] Server exited with code %EXITCODE%.
) else (
    echo   [ok] Server stopped.
)
popd
pause
endlocal
exit /b %EXITCODE%