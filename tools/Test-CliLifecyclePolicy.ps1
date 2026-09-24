[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ControllerAssembly
)

$ErrorActionPreference = 'Stop'
$assembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $ControllerAssembly))
$inputType = $assembly.GetType('CliInput', $true)
$parse = $inputType.GetMethod('Parse', [System.Reflection.BindingFlags]'Static, Public, NonPublic')

function Parse-Cli([string[]] $Arguments) {
    return $parse.Invoke($null, @(,[string[]] $Arguments))
}

$normal = Parse-Cli @('/new')
if ($null -ne $normal.ObserveSeconds -or $normal.HasAutomaticStopPolicy) { throw 'Normal /new unexpectedly has an automatic stop policy.' }
if ($normal.Map -ne $null -or $normal.EntrypointIndex -ne $null) { throw 'Normal /new unexpectedly injects the Grundorf validation fixture.' }

$saved = Parse-Cli @('/saved:situations\fixture.osn')
if ($saved.HasAutomaticStopPolicy) { throw 'Normal /saved unexpectedly has an automatic stop policy.' }

$last = Parse-Cli @('/last')
if ($last.HasAutomaticStopPolicy) { throw 'Normal /last unexpectedly has an automatic stop policy.' }

$observed = Parse-Cli @('/new', '/observe-seconds:8')
if ($observed.ObserveSeconds -ne 8 -or -not $observed.HasAutomaticStopPolicy) { throw 'Explicit /observe-seconds:8 did not enable the sole automatic stop policy.' }

$batch = Parse-Cli @('/new')
if ($batch.RuntimeBatch -or $batch.RuntimeWriteBatch -or $batch.D3DBatch) { throw 'Validation batches must be opt-in.' }

Write-Output 'PASS cli.lifecycle-policy'
