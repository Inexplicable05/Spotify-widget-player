$ErrorActionPreference = 'Stop'
$buildScript = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../SpotiFloat/build_releases.ps1'))
$fixture = Join-Path ([System.IO.Path]::GetTempPath()) ('SpotiFloat-build-check-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
$fakeCompiler = Join-Path $fixture 'compiler.ps1'
$global:SpotiFloatBuildCheck = @{ PublishExit = 0; CompileExit = 0; Compiler = $fakeCompiler; CompileCalls = 0 }
Set-Content -LiteralPath $fakeCompiler -Value 'param([string]$InstallerPath); $global:SpotiFloatBuildCheck.Installer = $InstallerPath; $global:SpotiFloatBuildCheck.CompileCalls++; $global:LASTEXITCODE = $global:SpotiFloatBuildCheck.CompileExit'

# Shadow external tools only in this test process; no publish or installer runs.
function dotnet {
    $global:SpotiFloatBuildCheck.PublishArguments = $args
    $global:LASTEXITCODE = $global:SpotiFloatBuildCheck.PublishExit
}
function Get-Command {
    param([string]$Name, [object]$ErrorAction)
    if ($Name -ne 'ISCC.exe') { throw "Unexpected compiler lookup: $Name" }
    [pscustomobject]@{ Source = $global:SpotiFloatBuildCheck.Compiler }
}

Push-Location ([System.IO.Path]::GetTempPath())
try {
    & $buildScript
    if ($SpotiFloatBuildCheck.PublishArguments[1] -ne (Join-Path (Split-Path $buildScript -Parent) 'SpotiFloat.csproj')) {
        throw 'Publish input depends on the working directory.'
    }
    if ($SpotiFloatBuildCheck.CompileCalls -ne 0) { throw 'Default publish invoked an installer.' }
    & $buildScript -BuildInstaller
    if ($SpotiFloatBuildCheck.Installer -ne (Join-Path (Split-Path $buildScript -Parent) 'Setup/installer.iss')) {
        throw 'Installer input depends on the working directory.'
    }
    $SpotiFloatBuildCheck.PublishExit = 9
    $failed = $false
    try { & $buildScript -BuildInstaller } catch { $failed = $_.Exception.Message -like 'dotnet publish failed*' }
    if (!$failed -or $SpotiFloatBuildCheck.CompileCalls -ne 1) { throw 'Publish failure did not stop installer execution.' }
    $SpotiFloatBuildCheck.PublishExit = 0
    $SpotiFloatBuildCheck.CompileExit = 7
    $failed = $false
    try { & $buildScript -BuildInstaller } catch { $failed = $_.Exception.Message -like 'Installer compilation failed*' }
    if (!$failed) { throw 'Installer failure was reported as success.' }
    # Do not expose the simulated compiler failure as the test process exit code.
    $global:LASTEXITCODE = 0
    Write-Output 'PASS: build helper paths, optional installer dispatch and external-tool failures (tools mocked).'
}
finally {
    Pop-Location
    [System.IO.File]::Delete($fakeCompiler)
    [System.IO.Directory]::Delete($fixture)
    Remove-Variable SpotiFloatBuildCheck -Scope Global
}
