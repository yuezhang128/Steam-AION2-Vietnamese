@echo off
rem Build AION2_VIE.exe with the .NET Framework compiler that ships with Windows.
rem Embed the language data; the optional font is loaded before building the pak.
setlocal
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /out:AION2_VIE.exe /resource:langpack\L10NString.dat,vie.dat /reference:System.Windows.Forms.dll /reference:System.Drawing.dll AION2_VIE.cs
if errorlevel 1 exit /b 1
echo Built AION2_VIE.exe
