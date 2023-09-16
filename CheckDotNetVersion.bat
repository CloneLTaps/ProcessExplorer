@echo off
setlocal enabledelayedexpansion

rem Define the required .NET version
set requiredVersion=5

rem Get the installed .NET version
for /f "delims=" %%v in ('dotnet --version') do (
    set version=%%v
)

for /f "tokens=1 delims=." %%a in ("!version!") do (
    set version=%%a
)

rem Define the full path to ProcessExplorer.runtimeconfig.json
set "filePath=ProcessExplorer.runtimeconfig.json"

rem Check if the installed version is greater than or equal to the required version
if "%version%" GEQ "%requiredVersion%" (
    echo Found .NET version %version% required: %requiredVersion% or greater

    rem Update ProcessExplorer.runtimeconfig.json to use .NET %version%
    (
        echo {
        echo   "runtimeOptions": {
        echo     "tfm": "net%version%.0",
        echo     "framework": {
        echo       "name": "Microsoft.WindowsDesktop.App",
        echo       "version": "%version%.0.0"
        echo     }
        echo   }
        echo }
    ) > "%filePath%"

    echo Updated ProcessExplorer.runtimeconfig.json to use .NET %version%

    exit /b 0
) else (
    echo Required .NET version %requiredVersion% or greater is not installed.
    exit /b 1
)
