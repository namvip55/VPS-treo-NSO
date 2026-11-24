@echo off
title JarManager - Admin Dashboard
cd /d "%~dp0"

echo [*] Kiem tra va giai phong tien trinh AdminDashboard cu...
taskkill /f /im AdminDashboard.exe >nul 2>&1

echo [*] Dang bien dich JarManager Admin Dashboard (High-DPI 200%%)...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /r:System.Web.Extensions.dll /out:"%~dp0AdminDashboard.exe" "%~dp0tools\AdminDashboard.cs"
if %errorlevel% neq 0 (
    echo.
    echo [!] LOI BIEN DICH C#! Vui long kiem tra file tools\AdminDashboard.cs
    pause
    exit /b 1
)

echo [*] Khoi chay JarManager Central Admin Dashboard...
start "" "%~dp0AdminDashboard.exe"
exit
