[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$InstallerPath)
$ErrorActionPreference = 'Stop'
$InstallerPath = (Resolve-Path -LiteralPath $InstallerPath).Path
$uninstallKey = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SpotiFloat_is1'
if (Test-Path -LiteralPath $uninstallKey) { throw 'Refusing to replace an existing per-user installation.' }
$machineKeys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SpotiFloat_is1', 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SpotiFloat_is1')
$machineBefore = @($machineKeys | ForEach-Object { Get-ItemProperty -LiteralPath $_ -ErrorAction SilentlyContinue | Select-Object DisplayName, DisplayVersion, InstallLocation, UninstallString }) | ConvertTo-Json
$repo = Split-Path $PSScriptRoot -Parent
$logs = Join-Path $repo 'artifacts'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$fixture = Join-Path ([System.IO.Path]::GetTempPath()) ('SpotiFloat-install-check-' + [guid]::NewGuid().ToString('N'))
$shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'SpotiFloat/SpotiFloat.lnk'
if (Test-Path -LiteralPath $shortcut) { throw 'Refusing to replace an existing per-user shortcut.' }
$runKey = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
$startupBefore = Get-ItemPropertyValue -LiteralPath $runKey -Name SpotiFloat -ErrorAction SilentlyContinue
$settingsPath = Join-Path $env:APPDATA 'SpotiFloat/settings.json'
$settingsBefore = if (Test-Path -LiteralPath $settingsPath) { (Get-FileHash -LiteralPath $settingsPath).Hash } else { $null }
$app = $null
try {
    $install = Start-Process -FilePath $InstallerPath -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/NOCLOSEAPPLICATIONS', '/TASKS=', ('/DIR="' + $fixture + '"'), ('/LOG="' + (Join-Path $logs 'install-check.log') + '"')) -WindowStyle Hidden -PassThru -Wait
    if ($install.ExitCode -ne 0) { throw "Setup failed: $($install.ExitCode)" }
    $entry = Get-ItemProperty -LiteralPath $uninstallKey
    if ($entry.DisplayName -notin @('SpotiFloat', 'SpotiFloat (Current user)') -or $entry.DisplayVersion -ne '1.2.0' -or $entry.InstallLocation.TrimEnd('\') -ne $fixture) { throw 'Incorrect Windows uninstall registration.' }
    foreach ($relative in @('SpotiFloat.exe', 'LICENSE', 'THIRD-PARTY-NOTICES.md', 'docs/licenses/NAudio-MIT.txt', 'docs/licenses/Hardcodet-MIT.txt', 'runtime-notices/Microsoft.NETCore.App.Runtime.win-x64/THIRD-PARTY-NOTICES.TXT', 'runtime-notices/Microsoft.WindowsDesktop.App.Runtime.win-x64/LICENSE')) {
        if (!(Test-Path -LiteralPath (Join-Path $fixture $relative))) { throw "Installed payload is missing $relative" }
    }
    if (!(Test-Path -LiteralPath $shortcut)) { throw 'Start menu shortcut is missing.' }
    $app = Start-Process -FilePath (Join-Path $fixture 'SpotiFloat.exe') -WindowStyle Hidden -PassThru
    if ($app.WaitForExit(6000)) { throw "Installed app exited early: $($app.ExitCode)" }
    Write-Output 'PASS: setup registration, application payload, licences, Start menu shortcut and installed app startup.'
}
finally {
    if ($app -and !$app.HasExited) { Stop-Process -Id $app.Id; $app.WaitForExit() }
    $uninstaller = Join-Path $fixture 'unins000.exe'
    $removeExit = 0
    if (Test-Path -LiteralPath $uninstaller) {
        $remove = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', ('/LOG="' + (Join-Path $logs 'uninstall-check.log') + '"')) -WindowStyle Hidden -PassThru -Wait
        $removeExit = $remove.ExitCode
    }
    # The disposable installation shares the real product's startup value.
    if ($null -ne $startupBefore) { Set-ItemProperty -LiteralPath $runKey -Name SpotiFloat -Value $startupBefore }
    if ($removeExit -ne 0) { throw "Uninstall failed: $removeExit" }
}
if (Test-Path -LiteralPath $uninstallKey) { throw 'Uninstall registration was not removed.' }
if (Test-Path -LiteralPath (Join-Path $fixture 'SpotiFloat.exe')) { throw 'Installed executable was not removed.' }
$settingsAfter = if (Test-Path -LiteralPath $settingsPath) { (Get-FileHash -LiteralPath $settingsPath).Hash } else { $null }
if ($settingsBefore -ne $settingsAfter) { throw 'Preference file changed during the installer check.' }
if (Test-Path -LiteralPath $shortcut) { throw 'Start menu shortcut was not removed.' }
$machineAfter = @($machineKeys | ForEach-Object { Get-ItemProperty -LiteralPath $_ -ErrorAction SilentlyContinue | Select-Object DisplayName, DisplayVersion, InstallLocation, UninstallString }) | ConvertTo-Json
if ($machineBefore -ne $machineAfter) { throw 'Existing machine-wide registration changed.' }
Write-Output 'PASS: uninstall removed registration, executable and shortcut; existing preferences and startup opt-in preserved.'
