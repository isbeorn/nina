param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$artifactRoot = Join-Path $repositoryRoot '.artifacts/plugin-compatibility'
$fixtureProject = Join-Path $repositoryRoot '.github/compatibility/PluginFixture/PluginFixture.csproj'
$hostProject = Join-Path $repositoryRoot '.github/compatibility/PluginHost/PluginHost.csproj'
New-Item -ItemType Directory -Force $artifactRoot | Out-Null

# Build the plugin once against immutable published packages, before building the current host.
dotnet build $fixtureProject -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Published 3.2 plugin fixture build failed.' }
$fixtureOutput = Join-Path (Split-Path $fixtureProject) "bin/$Configuration/net8.0-windows"
$pluginDirectory = Join-Path $artifactRoot 'plugin'
New-Item -ItemType Directory -Force $pluginDirectory | Out-Null
$plugin = Join-Path $pluginDirectory 'NINA.CompatibilityFixture.dll'
Copy-Item -LiteralPath (Join-Path $fixtureOutput 'NINA.CompatibilityFixture.dll') -Destination $plugin -Force
$fixtureHash = (Get-FileHash -LiteralPath $plugin).Hash

dotnet build $hostProject -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Current plugin host build failed.' }
$hostOutput = Join-Path (Split-Path $hostProject) "bin/$Configuration/net8.0-windows"
dotnet (Join-Path $hostOutput 'PluginHost.dll') $plugin
if ($LASTEXITCODE -ne 0) { throw 'Unmodified 3.2 plugin failed in the current host.' }
if ((Get-FileHash -LiteralPath $plugin).Hash -ne $fixtureHash) { throw 'The compatibility fixture changed during testing.' }

$toolDirectory = Join-Path $artifactRoot 'tools'
if (-not (Test-Path (Join-Path $toolDirectory 'apicompat.exe'))) {
    dotnet tool install Microsoft.DotNet.ApiCompat.Tool --version 8.0.425 --tool-path $toolDirectory
    if ($LASTEXITCODE -ne 0) { throw 'ApiCompat installation failed.' }
}
$apiCompat = Get-ChildItem -LiteralPath $toolDirectory -Filter 'Microsoft.DotNet.ApiCompat.Tool.dll' -Recurse | Select-Object -First 1
if (-not $apiCompat) { throw 'ApiCompat executable assembly was not found.' }
$dotnetRoot = Split-Path (Get-Command dotnet).Source
$references = @()
foreach ($pack in @('Microsoft.WindowsDesktop.App.Ref', 'Microsoft.NETCore.App.Ref')) {
    $version = Get-ChildItem -Directory (Join-Path $dotnetRoot "packs/$pack") |
        Where-Object { $_.Name -match '^8\.0\.\d+$' } | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
    if (-not $version) { throw "No .NET 8 reference pack found for $pack." }
    $references += Join-Path $version.FullName 'ref/net8.0'
}
$leftReferences = (@($fixtureOutput) + $references) -join ','
$rightReferences = (@($hostOutput) + $references) -join ','
$assemblies = Get-ChildItem -LiteralPath $fixtureOutput -Filter '*.dll' | Where-Object { ($_.Name -like 'NINA.*.dll' -and $_.Name -ne 'NINA.CompatibilityFixture.dll') -or $_.Name -eq 'nikoncswrapper.dll' }
if (@($assemblies).Count -ne 12) { throw "Expected all 12 published NINA contract assemblies, found $(@($assemblies).Count)." }
foreach ($assembly in $assemblies) {
    $current = Join-Path $hostOutput $assembly.Name
    if (-not (Test-Path -LiteralPath $current)) { throw "Missing plugin contract assembly: $($assembly.Name)" }
    $apiOutput = dotnet $apiCompat.FullName -l $assembly.FullName -r $current --lref $leftReferences --rref $rightReferences --enable-rule-cannot-change-parameter-name 2>&1
    $apiExit = $LASTEXITCODE
    $apiOutput | Write-Output
    if ($apiExit -ne 0 -or ($apiOutput -join "`n") -notmatch 'APICompat ran successfully without finding any breaking changes') { throw "3.2 API incompatibility in $($assembly.Name)" }
}
Write-Host "PASS: $($assemblies.Count) published 3.2 assemblies preserve their public API."
