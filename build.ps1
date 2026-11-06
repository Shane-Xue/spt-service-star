[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$out = Join-Path $PSScriptRoot 'artifacts'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$managed = Join-Path $root 'EscapeFromTarkov_Data\Managed'
$clientRefs = @('Assembly-CSharp.dll','Comfort.dll','DissonanceVoip.dll','ItemComponent.Types.dll',
 'ItemTemplate.Types.dll','netstandard.dll','Sirenix.Serialization.dll','Newtonsoft.Json.dll',
 'UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.UIModule.dll','UnityEngine.AudioModule.dll','UnityEngine.ImageConversionModule.dll','UnityEngine.UI.dll','Unity.TextMeshPro.dll') |
 ForEach-Object { Join-Path $managed $_ }
$clientRefs += @((Join-Path $root 'BepInEx\core\BepInEx.dll'),
 (Join-Path $root 'BepInEx\core\0Harmony.dll'),(Join-Path $root 'BepInEx\plugins\spt\spt-common.dll'))
$argsClient = @('/nologo','/target:library','/optimize+','/codepage:65001',"/out:$out\ServiceStars.Client.dll")
$argsClient += "/resource:$(Join-Path $PSScriptRoot 'assets\service-star.png'),ServiceStars.service-star.png"
$argsClient += "/resource:$(Join-Path $PSScriptRoot 'assets\locales.json'),ServiceStars.locales.json"
$argsClient += $clientRefs | ForEach-Object { "/reference:$_" }
$argsClient += Get-ChildItem (Join-Path $PSScriptRoot 'client') -Filter '*.cs' | ForEach-Object FullName
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' @argsClient
if ($LASTEXITCODE -ne 0) { throw 'Client compilation failed.' }
$sdk = (Get-ChildItem 'C:\Program Files\dotnet\sdk' -Directory | Sort-Object Name -Descending | Select-Object -First 1).FullName
$ref = (Get-ChildItem 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref' -Directory | Sort-Object Name -Descending | Select-Object -First 1).FullName
$serverRefs = Get-ChildItem (Join-Path $ref 'ref\net10.0') -Filter '*.dll' | ForEach-Object FullName
$serverRefs += @('SPTarkov.Server.Core.dll','SPTarkov.Common.dll','SPTarkov.DI.dll','SemanticVersioning.dll') |
 ForEach-Object { Join-Path $root "SPT_Runtime\$_" }
$response = @('/nologo','/target:library','/langversion:latest','/nullable:enable','/optimize+',"/out:`"$out\ServiceStars.Server.dll`"")
$response += $serverRefs | ForEach-Object { "/reference:`"$_`"" }
$response += Get-ChildItem (Join-Path $PSScriptRoot 'server') -Filter '*.cs' | ForEach-Object { '"' + $_.FullName + '"' }
$rsp = Join-Path $out 'server.rsp'
[IO.File]::WriteAllLines($rsp, $response)
& dotnet (Join-Path $sdk 'Roslyn\bincore\csc.dll') "@$rsp"
if ($LASTEXITCODE -ne 0) { throw 'Server compilation failed.' }
Write-Host "Built client and server in $out"
