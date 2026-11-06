$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$out = Join-Path $PSScriptRoot 'artifacts'
$sdk = (Get-ChildItem 'C:\Program Files\dotnet\sdk' -Directory | Sort-Object Name -Descending | Select-Object -First 1).FullName
$argsTest = Get-Content (Join-Path $out 'server.rsp') | Where-Object { $_ -notmatch '^/target:|^/out:|\\server\\' }
$argsTest += @('/target:exe',"/out:`"$out\ServiceStars.Tests.dll`"", "/reference:`"$out\ServiceStars.Server.dll`"",
 "/reference:`"$root\EscapeFromTarkov_Data\Managed\Newtonsoft.Json.dll`"",
 ('"' + (Join-Path $PSScriptRoot 'tests\Program.cs') + '"'),
 ('"' + (Join-Path $PSScriptRoot 'client\Ledger.cs') + '"'),
 ('"' + (Join-Path $PSScriptRoot 'client\CombatDamage.cs') + '"'))
$argsTest += ('"' + (Join-Path $PSScriptRoot 'client\ModLocale.cs') + '"')
$argsTest += "/resource:`"$(Join-Path $PSScriptRoot 'assets\locales.json')`",ServiceStars.locales.json"
$rsp = Join-Path $out 'tests.rsp'
[IO.File]::WriteAllLines($rsp, $argsTest)
& dotnet (Join-Path $sdk 'Roslyn\bincore\csc.dll') "@$rsp"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
Copy-Item -LiteralPath (Join-Path $root 'SPT_Runtime\SPT.Server.runtimeconfig.json') -Destination (Join-Path $out 'ServiceStars.Tests.runtimeconfig.json')
& dotnet (Join-Path $out 'ServiceStars.Tests.dll') (Join-Path $root 'SPT_Runtime')
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
