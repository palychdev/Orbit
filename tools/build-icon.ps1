$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot
dotnet build (Join-Path $projectRoot 'Orbit.csproj') -c Release -p:AppHostDotNetSearch=Global
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$exe=Join-Path $projectRoot 'bin\Release\net10.0-windows\Orbit.exe'
$process=Start-Process -FilePath $exe -ArgumentList @('--export-icon',('"'+(Join-Path $projectRoot 'Assets\orbit.ico')+'"')) -Wait -PassThru -WindowStyle Hidden
if($process.ExitCode -ne 0){throw 'Icon export failed'}
dotnet build (Join-Path $projectRoot 'Orbit.csproj') -c Release -p:AppHostDotNetSearch=Global
if($LASTEXITCODE -ne 0){throw 'Build failed'}
