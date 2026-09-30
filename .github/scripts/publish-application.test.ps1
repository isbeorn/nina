$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('nina-publish-tests-' + [Guid]::NewGuid().ToString('N'))
$scriptsPath = Join-Path $testRoot '.github/scripts'
New-Item -ItemType Directory -Path $scriptsPath -Force | Out-Null
$publisher = Join-Path $scriptsPath 'publish-application.ps1'
Copy-Item (Join-Path $PSScriptRoot 'publish-application.ps1') $publisher
$fakeSignTool = Join-Path $testRoot 'signtool.ps1'
@'
$signCalls.Add(@($args))
$global:LASTEXITCODE = if ($args[0] -eq 'sign') { $scenario.SignExitCode } else { $scenario.VerifyExitCode }
'@ | Set-Content -LiteralPath $fakeSignTool

$scenarios = @(
    @{ Name = 'Signing disabled'; Sign = $false; ToolAvailable = $false; PublishExitCode = 0; SignExitCode = 0; VerifyExitCode = 0; ExpectedSigns = 0; ExpectedVerifications = 0; ExpectedWarning = $false },
    @{ Name = 'SignTool missing'; Sign = $true; ToolAvailable = $false; PublishExitCode = 0; SignExitCode = 0; VerifyExitCode = 0; ExpectedSigns = 0; ExpectedVerifications = 0; ExpectedWarning = $true },
    @{ Name = 'Signing succeeds'; Sign = $true; ToolAvailable = $true; PublishExitCode = 0; SignExitCode = 0; VerifyExitCode = 0; ExpectedSigns = 6; ExpectedVerifications = 6; ExpectedWarning = $false },
    @{ Name = 'Signing fails'; Sign = $true; ToolAvailable = $true; PublishExitCode = 0; SignExitCode = 1; VerifyExitCode = 0; ExpectedSigns = 6; ExpectedVerifications = 0; ExpectedWarning = $true },
    @{ Name = 'Verification fails'; Sign = $true; ToolAvailable = $true; PublishExitCode = 0; SignExitCode = 0; VerifyExitCode = 1; ExpectedSigns = 6; ExpectedVerifications = 6; ExpectedWarning = $true },
    @{ Name = 'Publishing fails'; Sign = $true; ToolAvailable = $true; PublishExitCode = 9; SignExitCode = 0; VerifyExitCode = 0; ExpectedSigns = 0; ExpectedVerifications = 0; ExpectedWarning = $false }
)

$failures = [Collections.Generic.List[string]]::new()
foreach ($scenario in $scenarios) {
    & {
        $signCalls = [Collections.Generic.List[object]]::new()
        $toolLookups = [Collections.Generic.List[string]]::new()
        $output = Join-Path $testRoot $scenario.Name

        function dotnet {
            $global:LASTEXITCODE = $scenario.PublishExitCode
            if ($global:LASTEXITCODE -ne 0) { return }
            $destination = $args[[Array]::IndexOf($args, '-o') + 1]
            New-Item -ItemType Directory -Path $destination | Out-Null
            foreach ($name in 'NINA.exe', 'NINA.dll', 'NINA.Core.dll', 'NINA.r2r.dll', 'Accord.Imaging.dll', 'nikoncswrapper.dll', 'Vendor.dll') {
                Set-Content -LiteralPath (Join-Path $destination $name) -Value 'Published fixture'
            }
        }

        function Get-Command {
            param([string]$Name, [string]$ErrorAction)
            if ($Name -ne 'signtool.exe') { throw "Unexpected command lookup: $Name" }
            $toolLookups.Add($Name)
            if ($scenario.ToolAvailable) { return [pscustomobject]@{ Source = $fakeSignTool } }
            if ($ErrorAction -eq 'Stop') {
                throw [Management.Automation.CommandNotFoundException]::new("The term 'signtool.exe' is not recognized as a name of a cmdlet, function, script file, or executable program.")
            }
        }

        $caught = $null
        $messages = @()
        try {
            $messages = @(& $publisher -OutputDirectory $output -Sign:$scenario.Sign 3>&1)
        } catch {
            $caught = $_
        }
        try {
            if ($scenario.PublishExitCode -ne 0) {
                if (!$caught -or $caught.Exception.Message -notlike '*Application publishing failed with exit code 9*') {
                    throw 'Publishing failures must remain fatal.'
                }
            } else {
                if ($caught) { throw $caught }
                # GitHub's pwsh wrapper propagates LASTEXITCODE after the script returns.
                if ($LASTEXITCODE -ne 0) { throw "Successful publish left exit code $LASTEXITCODE." }
                if (!(Test-Path -LiteralPath ($output + '.publish.json'))) { throw 'Publish metadata was not written.' }
                if ((Get-Content -LiteralPath ($output + '.publish.json') -Raw | ConvertFrom-Json).FileCount -ne 7) {
                    throw 'Unexpected published payload.'
                }
            }
            $signs = @($signCalls | Where-Object { $_[0] -eq 'sign' })
            $verifications = @($signCalls | Where-Object { $_[0] -eq 'verify' })
            if ($signs.Count -ne $scenario.ExpectedSigns -or $verifications.Count -ne $scenario.ExpectedVerifications) {
                throw "Unexpected signing/verification calls: $($signs.Count)/$($verifications.Count)."
            }
            if (@($signCalls | Where-Object { [IO.Path]::GetFileName($_[-1]) -eq 'Vendor.dll' }).Count) {
                throw 'The publisher must not sign unrelated vendor assemblies.'
            }
            if (!$scenario.Sign -and $toolLookups.Count) { throw 'Signing disabled must not look up SignTool.' }
            $warnings = @($messages | Where-Object { $_ -is [Management.Automation.WarningRecord] })
            if (($warnings.Count -gt 0) -ne $scenario.ExpectedWarning) { throw 'Unexpected warning behavior.' }
            Write-Host "PASS: $($scenario.Name)"
        } catch {
            $failures.Add("$($scenario.Name): $($_.Exception.Message)")
            Write-Host "FAIL: $($failures[-1])"
        }
    }
}

# Delete only the unique temporary fixture root created by this test run.
$resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
if (!$resolvedTestRoot.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase) -or
    [IO.Path]::GetFileName($resolvedTestRoot) -notlike 'nina-publish-tests-*') {
    throw 'Refusing to remove a fixture outside the temporary directory.'
}
Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
if ($failures.Count) { throw ($failures -join [Environment]::NewLine) }
exit 0
