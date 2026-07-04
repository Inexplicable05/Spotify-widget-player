param (
    [switch]$BuildInstaller
)

Write-Host "Building SpotiFloat (Portable Executable)..." -ForegroundColor Cyan

# 1. Build the standalone portable executable
$dotnet = "C:\Users\ashut\AppData\Local\Microsoft\dotnet\dotnet.exe"
& $dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishReadyToRun=true

Write-Host "Portable Executable built successfully!" -ForegroundColor Green
Write-Host "Location: bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\SpotiFloat.exe" -ForegroundColor Yellow

if ($BuildInstaller) {
    Write-Host "Checking for Inno Setup compiler..." -ForegroundColor Cyan
    
    $isccSystem = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    $isccUser = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    
    $iscc = ""
    if (Test-Path $isccSystem) { $iscc = $isccSystem }
    elseif (Test-Path $isccUser) { $iscc = $isccUser }

    if ($iscc -ne "") {
        Write-Host "Building Installer Release..." -ForegroundColor Cyan
        & $iscc "Setup\installer.iss"
        Write-Host "Installer built successfully!" -ForegroundColor Green
        Write-Host "Location: Setup\Output\SpotiFloat_Setup.exe" -ForegroundColor Yellow
    } else {
        Write-Host "Inno Setup compiler (ISCC.exe) not found at default location." -ForegroundColor Red
        Write-Host "Please install Inno Setup 6 to compile the Setup executable." -ForegroundColor Red
    }
} else {
    Write-Host "Run this script with -BuildInstaller switch to compile the Inno Setup installer." -ForegroundColor Gray
}
