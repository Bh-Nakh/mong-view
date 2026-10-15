@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
rem App name (Korean) lives in app.name so this file stays ASCII-only.
set /p APPNAME=<app.name

rem Prefer the 64-bit dotnet (the 32-bit one may have no SDK)
set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
if not exist "%DOTNET%" set "DOTNET=dotnet"

if not exist res\app.ico (
  python tools\make_icon.py res\app.ico || goto fail
)

rem Framework-dependent single exe. On a PC without .NET, the launcher shows
rem an install prompt that opens the .NET Desktop Runtime download page.
"%DOTNET%" publish src\NoAdView\NoAdView.csproj -c Release -r win-x64 -p:SelfContained=false -p:PublishSingleFile=true -p:DebugType=none -o dist --nologo
if errorlevel 1 goto fail
echo BUILD OK: dist\%APPNAME%.exe
exit /b 0

:fail
echo BUILD FAILED
exit /b 1
