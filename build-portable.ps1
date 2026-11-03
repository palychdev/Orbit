param([string]$Destination = (Join-Path $PSScriptRoot '..\Orbit-win-x64'))
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBuildEnableWorkloadResolver = 'false'
dotnet publish (Join-Path $PSScriptRoot 'Orbit.csproj') -c Release --self-contained false -p:AppHostDotNetSearch=AppRelative -p:AppHostRelativeDotNet=runtime -o $Destination --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
$dotnetPath = Split-Path (Get-Command dotnet).Source
$core = Get-ChildItem (Join-Path $dotnetPath 'shared\Microsoft.NETCore.App') -Directory | Where-Object Name -Like '10.0.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$desktop = Get-ChildItem (Join-Path $dotnetPath 'shared\Microsoft.WindowsDesktop.App') -Directory | Where-Object Name -Like '10.0.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$fxr = Get-ChildItem (Join-Path $dotnetPath 'host\fxr') -Directory | Where-Object Name -Like '10.0.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (!$core -or !$desktop -or !$fxr) { throw 'Install .NET 10 Windows Desktop runtime before packaging' }
$runtime = Join-Path $Destination 'runtime'
New-Item -ItemType Directory -Force (Join-Path $runtime 'shared\Microsoft.NETCore.App'), (Join-Path $runtime 'shared\Microsoft.WindowsDesktop.App'), (Join-Path $runtime 'host\fxr') | Out-Null
Copy-Item -LiteralPath $core.FullName -Destination (Join-Path $runtime 'shared\Microsoft.NETCore.App') -Recurse -Force
Copy-Item -LiteralPath $desktop.FullName -Destination (Join-Path $runtime 'shared\Microsoft.WindowsDesktop.App') -Recurse -Force
Copy-Item -LiteralPath $fxr.FullName -Destination (Join-Path $runtime 'host\fxr') -Recurse -Force
foreach ($name in @('LICENSE.txt','ThirdPartyNotices.txt')) { $license=Join-Path $dotnetPath $name; if(Test-Path -LiteralPath $license) {Copy-Item -LiteralPath $license -Destination $runtime -Force} }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $Destination -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs') -Destination $Destination -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Assets\UIcons\LICENSE') -Destination (Join-Path $Destination 'UIcons-LICENSE.txt') -Force
Write-Output "Portable app: $Destination"
