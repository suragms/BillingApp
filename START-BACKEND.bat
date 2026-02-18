@echo off
title HexaBill API - Backend Server
echo.
echo Starting HexaBill backend at http://localhost:5000
echo Keep this window OPEN while using the app.
echo.
cd /d "%~dp0backend\HexaBill.Api"
dotnet run
echo.
pause
