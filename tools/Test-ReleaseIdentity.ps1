[CmdletBinding()]
param([string] $PackagePath = '')

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($PackagePath)) { $PackagePath = Join-Path $root 'artifacts\release\OmsiLaunch-current.zip' }
$package = Resolve-Path $PackagePath
$extract = Join-Path $root 'artifacts\release\identity-verification'
Expand-Archive -LiteralPath $package -DestinationPath $extract -Force

[xml] $identity = Get-Content -LiteralPath (Join-Path $root 'OmsiLaunch.Version.props') -Raw -Encoding utf8
$source = $identity.Project.PropertyGroup | Select-Object -First 1
$productVersion = $source.OmsiLaunchProductVersion
if ([string]::IsNullOrWhiteSpace($productVersion)) { throw 'OmsiLaunch.Version.props does not define OmsiLaunchProductVersion.' }

$manifest = Get-Content -LiteralPath (Join-Path $extract 'release-manifest.json') -Raw | ConvertFrom-Json
if ($manifest.product -ne 'OmsiLaunch' -or $manifest.product_version -ne $productVersion -or $manifest.package_alias -ne 'current') { throw 'Invalid release manifest identity.' }

$expected = @{ ProductName = $source.OmsiLaunchProductName; CompanyName = $source.OmsiLaunchCompanyName; LegalCopyright = $source.OmsiLaunchLegalCopyright; FileVersion = $source.OmsiLaunchFileVersion; ProductVersion = $source.OmsiLaunchProductVersion }
$results = foreach ($file in Get-ChildItem -LiteralPath $extract -File -Recurse | Where-Object { $_.Extension -in '.exe', '.dll' -and $_.Name -notin 'nethost.dll', 'YamlDotNet.dll' }) {
    $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName)
    foreach ($field in $expected.Keys) { if ($version.$field -ne $expected[$field]) { throw "Invalid $field in $($file.FullName): '$($version.$field)'" } }
    [ordered]@{ path = $file.FullName.Substring($extract.Length + 1); description = $version.FileDescription; internal_name = $version.InternalName; original_filename = $version.OriginalFilename; product_version = $version.ProductVersion }
}

$controller = Join-Path $extract 'OmsiLaunch.exe'
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($controller).InternalName -ne 'OmsiLaunch' -or [Diagnostics.FileVersionInfo]::GetVersionInfo($controller).OriginalFilename -ne 'OmsiLaunch.exe') { throw 'Controller executable identity is invalid.' }
$windowsHost = Join-Path $extract 'OmsiLaunchW.exe'
if (-not (Test-Path -LiteralPath $windowsHost) -or [Diagnostics.FileVersionInfo]::GetVersionInfo($windowsHost).InternalName -ne 'OmsiLaunchW' -or [Diagnostics.FileVersionInfo]::GetVersionInfo($windowsHost).OriginalFilename -ne 'OmsiLaunchW.exe') { throw 'Windows host executable identity is invalid.' }
Add-Type -AssemblyName System.Drawing
if ($null -eq [Drawing.Icon]::ExtractAssociatedIcon($controller)) { throw 'Controller executable has no embedded icon.' }

[ordered]@{ manifest = $manifest; artifacts = $results } | ConvertTo-Json -Depth 6
