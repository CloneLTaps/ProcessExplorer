@echo off
setlocal enabledelayedexpansion

rem Define the required .NET version
set requiredVersion=5

rem Get the installed .NET version
for /f "delims=" %%v in ('dotnet --version') do (
    set version=%%v
    set version=!version:~0,1!
)

rem Check if the installed version is greater than or equal to the required version
if !version! GEQ %requiredVersion% (
    echo Found .NET version %version% required: %requiredVersion% or greater
    exit /b 0
) else (
    echo Required .NET version %requiredVersion% or greater is not installed.
    exit /b 1
)


