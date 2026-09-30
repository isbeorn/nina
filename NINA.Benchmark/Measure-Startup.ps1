param(
    [Parameter(Mandatory)][string]$BaselineDirectory,
    [Parameter(Mandatory)][string]$ReadyToRunDirectory,
    [Parameter(Mandatory)][string]$CompositeDirectory,
    [Parameter(Mandatory)][string]$SeedDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(1, 100)][int]$Iterations = 12,
    [ValidateRange(10, 120)][int]$TimeoutSeconds = 45
)

$ErrorActionPreference = 'Stop'
$variants = @(
    [pscustomobject]@{ Name = 'Baseline'; Path = (Resolve-Path $BaselineDirectory).Path },
    [pscustomobject]@{ Name = 'ReadyToRun'; Path = (Resolve-Path $ReadyToRunDirectory).Path },
    [pscustomobject]@{ Name = 'Composite'; Path = (Resolve-Path $CompositeDirectory).Path }
)
$seedPath = (Resolve-Path $SeedDirectory).Path
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) { throw 'Use a new output directory to preserve previous measurements.' }
if (!(Test-Path (Join-Path $seedPath 'NINA.sqlite')) -or !(Test-Path (Join-Path $seedPath 'Profiles'))) {
    throw 'The seed directory must contain NINA.sqlite and a Profiles directory with one benchmark profile.'
}
foreach ($variant in $variants) {
    if (!(Test-Path (Join-Path $variant.Path 'NINA.exe'))) { throw "Missing NINA.exe: $($variant.Path)" }
}

$probeProject = Join-Path $PSScriptRoot 'StartupProbe/StartupProbe.csproj'
& dotnet build $probeProject -c Release -m:1 -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Startup probe build failed.' }
$probePath = Join-Path $PSScriptRoot 'StartupProbe/bin/Release/net10.0-windows/StartupProbe.dll'
New-Item -ItemType Directory -Path $outputPath | Out-Null
$rows = [Collections.Generic.List[object]]::new()

function Invoke-Startup($variant, [int]$iteration) {
    $label = '{0:D2}-{1}' -f $iteration, $variant.Name
    $statePath = Join-Path $outputPath "$label-state"
    New-Item -ItemType Directory -Path $statePath | Out-Null
    Get-ChildItem -LiteralPath $seedPath | Copy-Item -Destination $statePath -Recurse
    $resultPath = Join-Path $outputPath "$label.json"
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = Join-Path $variant.Path 'NINA.exe'
    $startInfo.WorkingDirectory = $variant.Path
    $startInfo.UseShellExecute = $false
    $startInfo.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Environment['DOTNET_STARTUP_HOOKS'] = $probePath
    $startInfo.Environment['NINA_STARTUP_STATE'] = $statePath
    $startInfo.Environment['NINA_STARTUP_RESULT'] = $resultPath
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $started = $false
    try {
        $startInfo.Environment['NINA_STARTUP_TIMESTAMP'] = [Diagnostics.Stopwatch]::GetTimestamp().ToString()
        if (!$process.Start()) { throw "Could not start $label." }
        $started = $true
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        while (!$process.WaitForExit(100) -and !(Test-Path -LiteralPath $resultPath)) {
            if ([DateTime]::UtcNow -ge $deadline) {
                throw "Startup timed out: $label. Inspect $statePath/Logs."
            }
        }
        # Native camera SDK shutdown can take much longer than startup. It is outside
        # the measured interval; stop only this retained test process after its atomic result.
        if (!$process.HasExited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        $stdout.GetAwaiter().GetResult() | Set-Content (Join-Path $outputPath "$label.stdout.log")
        $stderr.GetAwaiter().GetResult() | Set-Content (Join-Path $outputPath "$label.stderr.log")
        if (!(Test-Path -LiteralPath $resultPath)) {
            throw "Startup failed: $label, exit code $($process.ExitCode). Inspect the output directory."
        }
        $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
        if ($result.Error) { throw $result.Error }
        $result | Add-Member NoteProperty Variant $variant.Name
        $result | Add-Member NoteProperty Iteration $iteration
        $rows.Add($result)
        $rows | Export-Csv (Join-Path $outputPath 'startup.csv') -NoTypeInformation
        Write-Host ('{0}: {1:N1} ms, JIT {2:N1} ms' -f $label, $result.StartupMilliseconds, $result.JitMilliseconds)
    } finally {
        if ($started -and !$process.HasExited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        $process.Dispose()
    }
}

# A separate warm-up per variant brings filesystem caches into a comparable state.
foreach ($variant in $variants) { Invoke-Startup $variant 0 }
# Rotate all six orders to balance order effects. Only iteration > 0 is measured.
$orders = @(@(0, 1, 2), @(2, 1, 0), @(1, 2, 0), @(0, 2, 1), @(2, 0, 1), @(1, 0, 2))
for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
    foreach ($index in $orders[($iteration - 1) % $orders.Count]) {
        Invoke-Startup $variants[$index] $iteration
    }
}

function Get-Median($values) {
    $sorted = @($values | Sort-Object)
    $middle = [int][Math]::Floor($sorted.Count / 2)
    if ($sorted.Count % 2) { return $sorted[$middle] }
    return ($sorted[$middle - 1] + $sorted[$middle]) / 2
}

$summary = foreach ($variant in $variants) {
    $measured = @($rows | Where-Object { $_.Iteration -gt 0 -and $_.Variant -eq $variant.Name })
    [pscustomobject]@{
        Variant = $variant.Name
        Runs = $measured.Count
        MedianStartupMs = Get-Median $measured.StartupMilliseconds
        MinStartupMs = ($measured.StartupMilliseconds | Measure-Object -Minimum).Minimum
        MaxStartupMs = ($measured.StartupMilliseconds | Measure-Object -Maximum).Maximum
        MedianCpuMs = Get-Median $measured.CpuMilliseconds
        MedianJitMs = Get-Median $measured.JitMilliseconds
        MedianJitMethods = Get-Median $measured.JitMethods
        MedianWorkingSetBytes = Get-Median $measured.WorkingSetBytes
        MedianPrivateBytes = Get-Median $measured.PrivateBytes
    }
}
$summary | ConvertTo-Json | Set-Content (Join-Path $outputPath 'summary.json')
$summary | Format-Table Variant, Runs, MedianStartupMs, MedianCpuMs, MedianJitMs -AutoSize
