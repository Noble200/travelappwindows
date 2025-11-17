# SCRIPT DE DIAGNÓSTICO ALLVA SYSTEM
Write-Host "=======================================" -ForegroundColor Yellow
Write-Host "  DIAGNÓSTICO ALLVA SYSTEM" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "1. Limpiando proyecto..." -ForegroundColor Green
dotnet clean

Write-Host ""
Write-Host "2. Compilando..." -ForegroundColor Green
dotnet build

Write-Host ""
Write-Host "3. Ejecutando con logs detallados..." -ForegroundColor Green
Write-Host "   (Presiona Ctrl+C para salir si no abre)" -ForegroundColor Yellow
Write-Host ""

$env:AVALONIA_LOG_LEVEL = "Verbose"
dotnet run