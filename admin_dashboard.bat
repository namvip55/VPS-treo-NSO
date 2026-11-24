@echo off
title JarManager - Admin Dashboard
cd /d "%~dp0"

if not exist "%~dp0AdminDashboard.exe" (
    echo [*] Dang bien dich AdminDashboard.exe lan dau...
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /r:System.Web.Extensions.dll /out:"%~dp0AdminDashboard.exe" "%~dp0tools\AdminDashboard.cs"
)

start "" "%~dp0AdminDashboard.exe"
exit
