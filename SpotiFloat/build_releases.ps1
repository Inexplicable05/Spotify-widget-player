[CmdletBinding()]
param (
    [switch]$BuildInstaller
)

$ErrorActionPreference = 'Stop'
$projectFile = Join-Path $PSScriptRoot 'SpotiFloat.csproj'

Write-Host 'Publishing the Windows x64 application payload...'
& dotnet publish $projectFile -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
Write-Host "Payload: $(Join-Path $PSScriptRoot 'bin/Release/net8.0-windows10.0.19041.0/win-x64/publish')"

if ($BuildInstaller) {
    $compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    $compilerPath = if ($compiler) { $compiler.Source } else { $null }
    if (!$compilerPath) {
        foreach ($candidate in @(
            (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'),
            (Join-Path $env:LOCALAPPDATA 'Programs/Inno Setup 6/ISCC.exe')
        )) {
            if (Test-Path -LiteralPath $candidate) { $compilerPath = $candidate; break }
        }
    }
    if (!$compilerPath) { throw 'Install Inno Setup 6 and make ISCC.exe available to build the installer.' }
    & $compilerPath (Join-Path $PSScriptRoot 'Setup/installer.iss')
    if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed with exit code $LASTEXITCODE." }
    Write-Host "Installer output: $(Join-Path $PSScriptRoot 'Setup/Output')"
}
