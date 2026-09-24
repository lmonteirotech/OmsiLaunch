[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $InstallationRoot,
    [Parameter(Mandatory = $true)] [string] $PackageDirectory
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path $InstallationRoot).Path
$package = (Resolve-Path $PackageDirectory).Path
if (Get-Process Omsi -ErrorAction SilentlyContinue) { throw 'OMSI is already running.' }

# The fixture is product-owned test content under the public profile directory.
# It is deleted only after the session and all postconditions are verified.
$profile = Join-Path $root '.omsilaunch\session-profiles\beta3-grundorf-fixture'
Remove-Item -LiteralPath $profile -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path (Join-Path $profile 'assets\splash') | Out-Null
Copy-Item -LiteralPath (Join-Path $package '.omsilaunch\assets\splash\ENG.bmp') -Destination (Join-Path $profile 'assets\splash\ENG.bmp')
Copy-Item -LiteralPath (Join-Path $package '.omsilaunch\assets\splash\PTB.bmp') -Destination (Join-Path $profile 'assets\splash\PTB.bmp')
@'
schema: omsilaunch.session-profile/v1
id: beta3-grundorf-fixture
name: Beta3 Grundorf Fixture
author: OmsiLaunch validation
version: '1'
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: controlled
    name: Controlled
    settings:
      graphics.maxFPS: 30
    presentation:
      splash:
        mode: managed
        language: ENG
        assets: assets\splash
    internet-textures:
      mode: disabled
'@ | Set-Content -LiteralPath (Join-Path $profile 'profile.yaml') -Encoding utf8

$optionsPath = Join-Path $root 'options.cfg'
$profilePath = Join-Path $profile 'profile.yaml'
$beforeOptions = (Get-FileHash -LiteralPath $optionsPath -Algorithm SHA256).Hash
$beforeProfile = (Get-FileHash -LiteralPath $profilePath -Algorithm SHA256).Hash
$diagnostics = Join-Path $root '.omsilaunch\diagnostics'
New-Item -ItemType Directory -Force -Path $diagnostics | Out-Null
$output = Join-Path $diagnostics 'beta3-profile-runtime.out'
$resultPath = Join-Path $diagnostics 'beta3-profile-runtime-result.json'

try {
    & (Join-Path $root 'OmsiLaunch.exe') $root /predefined-profile:beta3-grundorf-fixture /predefined-profile-index:1 /new /observe-seconds:8 /json | Set-Content -LiteralPath $output -Encoding utf8
    $exitCode = $LASTEXITCODE
    $result = [ordered]@{
        exit_code = $exitCode
        options_restored = $beforeOptions -eq (Get-FileHash -LiteralPath $optionsPath -Algorithm SHA256).Hash
        profile_intact = $beforeProfile -eq (Get-FileHash -LiteralPath $profilePath -Algorithm SHA256).Hash
        journal_present = Test-Path -LiteralPath (Join-Path $root '.omsilaunch\journal.json')
        lease_present = Test-Path -LiteralPath (Join-Path $root '.omsilaunch\lease')
        omsi_running = [bool](Get-Process Omsi -ErrorAction SilentlyContinue)
        runtime_output = $output
    }
    $result | ConvertTo-Json | Set-Content -LiteralPath $resultPath -Encoding utf8
    $result | ConvertTo-Json
    if ($exitCode -ne 0 -or -not $result.options_restored -or -not $result.profile_intact -or $result.journal_present -or $result.lease_present -or $result.omsi_running) { exit 1 }
}
finally {
    Remove-Item -LiteralPath $profile -Recurse -Force -ErrorAction SilentlyContinue
}
