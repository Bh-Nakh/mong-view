@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
set /p APPNAME=<app.name
rem Build final exe into release\ and make the installer (setup\<name>-Setup-*.exe).
set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
if not exist "%DOTNET%" set "DOTNET=dotnet"
"%DOTNET%" publish src\NoAdView\NoAdView.csproj -c Release -r win-x64 -p:SelfContained=false -p:PublishSingleFile=true -p:DebugType=none -o release --nologo || (echo BUILD FAILED & exit /b 1)
set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
"%ISCC%" /DSrcDir="%~dp0release" installer\%APPNAME%.iss || (echo SETUP FAILED & exit /b 1)
echo RELEASE OK: setup\
exit /b 0
