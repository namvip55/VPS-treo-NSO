@echo off
title JarManager - Admin Dashboard
cd /d "%~dp0"

echo [*] Dang khoi dong JarManager Admin Dashboard (High-DPI 200%%)...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /r:System.Web.Extensions.dll /out:"%~dp0AdminDashboard.exe" "%~dp0tools\AdminDashboard.cs" >nul 2>&1

start "" "%~dp0AdminDashboard.exe"
exit
