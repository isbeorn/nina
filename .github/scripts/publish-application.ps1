param(
    [Parameter(Mandatory)]
    [string]$OutputDirectory,
    [ValidateSet('Baseline', 'ReadyToRun', 'ReadyToRunComposite')]
    [string]$Mode = 'ReadyToRun',
    [ValidateSet('Release', 'SignedRelease')]
    [string]$Configuration = 'Release',
    [switch]$Sign
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $outputPath) -and (Get-ChildItem -LiteralPath $outputPath -Force | Select-Object -First 1)) {
    throw 'Use an empty output directory so stale files cannot enter the published application.'
}

$arguments = @(
    'publish', (Join-Path $repositoryRoot 'NINA/NINA.csproj'),
    '-c', $Configuration, '-r', 'win-x64', '--self-contained', 'true',
    '-o', $outputPath, '-m:1', '-p:UseSharedCompilation=false',
    '-p:GeneratePackageOnBuild=false'
)
if ($Mode -eq 'Baseline') {
    $arguments += @('-p:PublishReadyToRun=false', '-p:PublishReadyToRunComposite=false')
} else {
    $arguments += "-p:PublishProfile=$Mode"
}

$timer = [Diagnostics.Stopwatch]::StartNew()
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Application publishing failed with exit code $LASTEXITCODE." }

# These private SDK assets are copied by the existing post-build event, not by publish items.
foreach ($vendor in @('Canon', 'Nikon')) {
    $source = Join-Path $repositoryRoot "NINA/External/x64/$vendor"
    if (Test-Path -LiteralPath $source) {
        $destination = Join-Path $outputPath "External/x64/$vendor"
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Get-ChildItem -LiteralPath $source | Copy-Item -Destination $destination -Recurse -Force
    }
}

if ($Sign) {
    # R2R rewrites assemblies. Attempt final signing with the existing optional-signing policy.
    $signTool = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if (!$signTool) {
        Write-Warning 'signtool.exe is unavailable. Skipping optional signing of the published application.'
    } else {
        $PSNativeCommandUseErrorActionPreference = $false
        $files = Get-ChildItem -LiteralPath $outputPath -File | Where-Object {
            $_.Name -like 'NINA*.dll' -or $_.Name -in @('NINA.exe', 'Accord.Imaging.dll', 'nikoncswrapper.dll')
        }
        foreach ($file in $files) {
            try {
                & $signTool.Source sign /t http://timestamp.sectigo.com /i Sectigo /a /fd SHA256 $file.FullName
                if ($LASTEXITCODE -ne 0) {
                    Write-Warning "Optional signing failed for $($file.Name) (exit code $LASTEXITCODE)."
                    continue
                }
                & $signTool.Source verify /pa $file.FullName
                if ($LASTEXITCODE -ne 0) {
                    Write-Warning "Signature verification failed for $($file.Name) (exit code $LASTEXITCODE)."
                }
            } catch {
                Write-Warning "Optional signing could not complete for $($file.Name): $($_.Exception.Message)"
            }
        }
    }
}

$timer.Stop()
$files = Get-ChildItem -LiteralPath $outputPath -File -Recurse
[pscustomobject]@{
    Mode = $Mode
    Configuration = $Configuration
    OutputDirectory = $outputPath
    PublishSeconds = $timer.Elapsed.TotalSeconds
    FileCount = $files.Count
    TotalBytes = ($files | Measure-Object Length -Sum).Sum
    PayloadBytesWithoutSymbols = ($files | Where-Object Extension -ne '.pdb' | Measure-Object Length -Sum).Sum
} | ConvertTo-Json | Set-Content -LiteralPath ($outputPath + '.publish.json')

# GitHub's pwsh wrapper must not propagate an optional SignTool failure as the publish result.
exit 0
