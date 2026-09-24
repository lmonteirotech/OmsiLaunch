[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $InstallationRoot,
    [string] $PackageDirectory = '',
    [ValidateRange(5, 60)]
    [int] $ObserveSeconds = 8,
    [switch] $InstallPackage,
    [switch] $RunOmsi
)

# This validator deliberately exercises the packaged Release executable. It
# never runs from artifacts/bin and it leaves OMSI GUI files under the normal
# transaction/restore ownership of OmsiLaunch.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path $InstallationRoot).Path
$repository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($PackageDirectory)) { $PackageDirectory = Join-Path $repository 'artifacts\release\OmsiLaunch-current' }
$package = (Resolve-Path $PackageDirectory).Path
$packageCli = Join-Path $package 'OmsiLaunch.exe'
if (-not (Test-Path -LiteralPath $packageCli)) { throw "Release executable missing: $packageCli" }

$state = Join-Path $root '.omsilaunch'
$diagnostics = Join-Path $state 'diagnostics'
New-Item -ItemType Directory -Force -Path $diagnostics | Out-Null
$assets = Join-Path $package '.omsilaunch\assets\splash'
$languages = @('PTB', 'ENG', 'DEU', 'FRA')
foreach ($language in $languages) {
    $asset = Join-Path $assets ($language + '.bmp')
    if (-not (Test-Path -LiteralPath $asset)) { throw "Release splash asset missing: $asset" }
}

[xml] $identity = Get-Content -LiteralPath (Join-Path $repository 'OmsiLaunch.Version.props') -Raw -Encoding utf8
$identityProperties = $identity.Project.PropertyGroup | Select-Object -First 1
$productVersion = $identityProperties.OmsiLaunchProductVersion
if ([string]::IsNullOrWhiteSpace($productVersion)) { throw 'OmsiLaunch.Version.props does not define OmsiLaunchProductVersion.' }

$manifest = Get-Content -LiteralPath (Join-Path $package 'release-manifest.json') -Raw | ConvertFrom-Json
if ($manifest.configuration -ne 'Release') { throw 'Package manifest is not Release.' }
if ($manifest.product_version -ne $productVersion -or $manifest.package_alias -ne 'current') { throw "Package manifest does not distinguish product version $productVersion from the current package alias." }
if (($manifest.files.path | Where-Object { $_ -match '(^|/)Debug(/|$)' }).Count -ne 0) { throw 'Release manifest contains a Debug path.' }
if (($manifest.files.path | Where-Object { $_ -match '^runtime/plugin/' }).Count -ne 0) { throw 'Release manifest contains obsolete runtime/plugin staging files.' }
if (($manifest.files.path | Where-Object { $_ -match '^plugins/OmsiLaunch\.' }).Count -eq 0) { throw 'Release manifest does not install the permanent plugin closure under plugins.' }

function Get-FileSnapshot {
    param([string] $Directory, [string] $Filter = '*')
    if (-not (Test-Path -LiteralPath $Directory)) { return [ordered]@{} }
    $snapshot = [ordered]@{}
    Get-ChildItem -LiteralPath $Directory -File -Filter $Filter | ForEach-Object {
        $snapshot[$_.Name] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
    return $snapshot
}

function Get-ThirdPartyPluginSnapshot {
    param([string] $Directory)
    if (-not (Test-Path -LiteralPath $Directory)) { return [ordered]@{} }
    $snapshot = [ordered]@{}
    Get-ChildItem -LiteralPath $Directory -File | Where-Object { $_.Name -notlike 'OmsiLaunch.*' } | ForEach-Object {
        $snapshot[$_.Name] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
    return $snapshot
}

function Install-ReleasePackage {
    param([string] $PackageRoot, [string] $Installation)
    # Only product-owned filenames are copied. Existing third-party plugins
    # are neither captured nor touched by this installation operation.
    foreach ($file in Get-ChildItem -LiteralPath $PackageRoot -File) {
        # release-manifest.json is part of the closure: skipping it left an
        # older manifest beside newer binaries (Round A RA-007).
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $Installation $file.Name) -Force
    }
    $destinationPlugins = Join-Path $Installation 'plugins'
    New-Item -ItemType Directory -Force -Path $destinationPlugins | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $PackageRoot 'plugins') -File -Filter 'OmsiLaunch.*' | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $destinationPlugins $_.Name) -Force
    }
    $destinationState = Join-Path $Installation '.omsilaunch'
    New-Item -ItemType Directory -Force -Path $destinationState | Out-Null
    Copy-Item -Path (Join-Path $PackageRoot '.omsilaunch\*') -Destination $destinationState -Recurse -Force
}

if ($RunOmsi -and -not $InstallPackage) {
    throw '-RunOmsi requires -InstallPackage so the package-installed plugin closure is used by OMSI.'
}

$thirdPartyBefore = Get-ThirdPartyPluginSnapshot (Join-Path $root 'plugins')
if ($InstallPackage) { Install-ReleasePackage $package $root }
$cli = if ($InstallPackage) { Join-Path $root 'OmsiLaunch.exe' } else { $packageCli }
if (-not (Test-Path -LiteralPath $cli)) { throw "Installed release executable missing: $cli" }
$installedPluginHashes = Get-FileSnapshot (Join-Path $root 'plugins') 'OmsiLaunch.*'
if ($RunOmsi -and $installedPluginHashes.Count -eq 0) { throw 'No permanent OmsiLaunch plugin files are installed under plugins.' }

function Get-SplashSnapshot {
    param([string] $GuiDirectory)
    $snapshot = [ordered]@{}
    foreach ($name in @('NewSplashscreen_ENG.bmp', 'NewSplashscreen_PTB.bmp')) {
        $path = Join-Path $GuiDirectory $name
        $snapshot[$name] = if (Test-Path -LiteralPath $path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash } else { 'ABSENT' }
    }
    return $snapshot
}

function Test-SameSnapshot {
    param($Left, $Right)
    return (($Left | ConvertTo-Json -Compress) -eq ($Right | ConvertTo-Json -Compress))
}

$gui = Join-Path $root 'GUI'
$defaultExpected = @{
    'NewSplashscreen_ENG.bmp' = (Get-FileHash -LiteralPath (Join-Path $assets 'ENG.bmp') -Algorithm SHA256).Hash
    'NewSplashscreen_PTB.bmp' = (Get-FileHash -LiteralPath (Join-Path $assets 'PTB.bmp') -Algorithm SHA256).Hash
}
$custom = Join-Path $state 'assets\splash-release-validation-custom'
$customRelative = '.omsilaunch\assets\splash-release-validation-custom'
New-Item -ItemType Directory -Force -Path $custom | Out-Null
# Use a valid but intentionally distinguishable PTB fixture. It proves custom
# resolution without modifying the persistent default asset set.
Copy-Item -LiteralPath (Join-Path $assets 'ENG.bmp') -Destination (Join-Path $custom 'ENG.bmp') -Force
Copy-Item -LiteralPath (Join-Path $assets 'ENG.bmp') -Destination (Join-Path $custom 'PTB.bmp') -Force
$customExpected = @{
    'NewSplashscreen_ENG.bmp' = $defaultExpected['NewSplashscreen_ENG.bmp']
    'NewSplashscreen_PTB.bmp' = (Get-FileHash -LiteralPath (Join-Path $custom 'PTB.bmp') -Algorithm SHA256).Hash
}

function ConvertTo-CommandLineArgument {
    # Quotes one argument the way CommandLineToArgvW expects it. Windows
    # PowerShell joins an -ArgumentList array with spaces and adds no quoting,
    # so every value is escaped here: embedded quotes and the backslashes that
    # precede them (or the closing quote) are escaped, which keeps a root such
    # as 'D:\' or 'C:\Program Files\OMSI 2\' intact.
    param([string] $Value)
    if ($Value.Length -ne 0 -and $Value -notmatch '[\s"]') { return $Value }
    $builder = New-Object System.Text.StringBuilder
    [void] $builder.Append('"')
    $backslashes = 0
    foreach ($character in $Value.ToCharArray()) {
        if ($character -eq '\') { $backslashes++; continue }
        if ($character -eq '"') {
            [void] $builder.Append('\' * ($backslashes * 2 + 1)).Append('"')
            $backslashes = 0
            continue
        }
        if ($backslashes -ne 0) { [void] $builder.Append('\' * $backslashes); $backslashes = 0 }
        [void] $builder.Append($character)
    }
    [void] $builder.Append('\' * ($backslashes * 2)).Append('"')
    return $builder.ToString()
}

function Invoke-PresentationCase {
    param([string] $Name, [string[]] $Arguments, $ExpectedOverlay, [bool] $PreserveDuringSession)
    $before = Get-SplashSnapshot $gui
    $plan = & $cli $root @Arguments /plan /json | Out-String
    if ($Name -eq 'unset' -and $plan -match 'NewSplashscreen_') { throw 'Unset plan unexpectedly stages a splash overlay.' }
    if ($Name -ne 'unset' -and $plan -notmatch 'NewSplashscreen_ENG.bmp') { throw "$Name plan did not stage the English splash fallback." }
    if (-not $RunOmsi) { return [ordered]@{ name = $Name; plan_passed = $true; runtime_executed = $false; before = $before } }

    $output = Join-Path $diagnostics ('release-presentation-' + $Name + '-cli.out')
    $stderrPath = $output + '.err'
    Remove-Item -LiteralPath $output, $stderrPath -Force -ErrorAction SilentlyContinue
    $processArguments = @($root) + $Arguments + @(('/observe-seconds:' + $ObserveSeconds), '/json')
    $argumentLine = ($processArguments | ForEach-Object { ConvertTo-CommandLineArgument $_ }) -join ' '
    $process = Start-Process -FilePath $cli -ArgumentList $argumentLine -WorkingDirectory $root -WindowStyle Hidden -RedirectStandardOutput $output -RedirectStandardError $stderrPath -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(210)
    $observed = $false
    while ([DateTime]::UtcNow -lt $deadline) {
        if (-not (Get-Process -Id $process.Id -ErrorAction SilentlyContinue)) { break }
        $current = Get-SplashSnapshot $gui
        if ($PreserveDuringSession) { if (Test-SameSnapshot $before $current) { $observed = $true } else { throw 'Unset session changed an OMSI splash file.' } }
        elseif ($current['NewSplashscreen_ENG.bmp'] -eq $ExpectedOverlay['NewSplashscreen_ENG.bmp'] -and $current['NewSplashscreen_PTB.bmp'] -eq $ExpectedOverlay['NewSplashscreen_PTB.bmp']) { $observed = $true }
        Start-Sleep -Milliseconds 250
    }
    if (Get-Process -Id $process.Id -ErrorAction SilentlyContinue) { throw "Release CLI did not exit before the validation timeout: $Name" }
    $after = Get-SplashSnapshot $gui
    $clean = -not (Get-Process Omsi -ErrorAction SilentlyContinue) -and -not (Test-Path -LiteralPath (Join-Path $state 'journal.json'))
    return [ordered]@{ name = $Name; plan_passed = $true; runtime_executed = $true; cli_exit_code = $process.ExitCode; overlay_observed = $observed; restore_exact = (Test-SameSnapshot $before $after); cleanup_clean = $clean; before = $before; after = $after; stdout = (Get-Content -LiteralPath $output -Raw); stderr = (Get-Content -LiteralPath $stderrPath -Raw) }
}

$baseArgs = @('/new', '/map:maps\Grundorf\global.cfg', '/entrypoint-index:1')
$result = [ordered]@{
    package = $package
    installation = $root
    configuration = $manifest.configuration
    package_assets = $languages
    default_managed = Invoke-PresentationCase 'default' $baseArgs $defaultExpected $false
    custom_managed = Invoke-PresentationCase 'custom' ($baseArgs + ('/splash-assets:' + $customRelative)) $customExpected $false
    unset_preserve = Invoke-PresentationCase 'unset' ($baseArgs + '/splash:Unset') $null $true
    permanent_plugin_hashes = Get-FileSnapshot (Join-Path $root 'plugins') 'OmsiLaunch.*'
    third_party_plugins_unchanged = (Test-SameSnapshot $thirdPartyBefore (Get-ThirdPartyPluginSnapshot (Join-Path $root 'plugins')))
    generated_utc = [DateTimeOffset]::UtcNow.ToString('O')
}
$resultPath = Join-Path $diagnostics 'release-presentation-validation.json'
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $resultPath -Encoding utf8
$result | ConvertTo-Json -Depth 8

if ($RunOmsi) {
    $failed = @($result.default_managed, $result.custom_managed, $result.unset_preserve) | Where-Object { -not $_.overlay_observed -or -not $_.restore_exact -or -not $_.cleanup_clean -or $_.cli_exit_code -ne 0 }
    if ($failed.Count -ne 0 -or -not $result.third_party_plugins_unchanged -or ($result.permanent_plugin_hashes.Count -ne $installedPluginHashes.Count)) { exit 1 }
}
