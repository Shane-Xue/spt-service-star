[CmdletBinding()]
param([switch]$Deploy)
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
& (Join-Path $PSScriptRoot 'build.ps1')
& (Join-Path $PSScriptRoot 'test.ps1')
& (Join-Path $PSScriptRoot 'tools\audit-bindings.ps1')
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$archive = Join-Path $dist 'ServiceStars-1.2.1-SPT-4.1.6.zip'
$files = [ordered]@{
 'BepInEx/plugins/ServiceStars/ServiceStars.Client.dll' = (Join-Path $PSScriptRoot 'artifacts\ServiceStars.Client.dll')
 'SPT_Runtime/user/mods/ServiceStars/ServiceStars.Server.dll' = (Join-Path $PSScriptRoot 'artifacts\ServiceStars.Server.dll')
 'SPT_Runtime/user/mods/ServiceStars/config.json' = (Join-Path $PSScriptRoot 'config.json')
 'ServiceStars-README.md' = (Join-Path $PSScriptRoot 'README.md')
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [IO.File]::Open($archive, [IO.FileMode]::Create)
$zip = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
 foreach ($entry in $files.GetEnumerator()) {
  [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $entry.Value, $entry.Key, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
 }
} finally { $zip.Dispose(); $stream.Dispose() }
Write-Host "Packaged $archive"
if ($Deploy) {
 if (Get-Process 'SPT.Server','EscapeFromTarkov' -ErrorAction SilentlyContinue) { throw 'Close SPT server and game before deployment.' }
 $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
 foreach ($entry in $files.GetEnumerator()) {
  $target = Join-Path $gameRoot ($entry.Key.Replace('/', '\'))
  New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($target)) | Out-Null
  if (Test-Path -LiteralPath $target) {
   $backup = Join-Path $gameRoot ("backups\service-stars-$stamp\" + $entry.Key.Replace('/', '\'))
   New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($backup)) | Out-Null
   Copy-Item -LiteralPath $target -Destination $backup
   # Preserve the operator's configured star threshold on subsequent deployments.
   if ($entry.Key.EndsWith('config.json')) { continue }
  }
  Copy-Item -LiteralPath $entry.Value -Destination $target -Force
 }
 $message = "`r`n## $((Get-Date).ToString('yyyy-MM-dd')) — 勤务之星 Service Stars 1.2.1`r`n`r`n- 检视与升星通知改为自动跟随游戏 locale，ch 中文、en 英文，其他未翻译语言回退英文；武器名使用游戏本地化，移除手动 Chinese 开关。`r`n- 按伤害记录中的原始物品归属，不用当前手持枪或弹药模板猜测。维持累计计数去重和断线补传。`r`n- 验证：本机编译、56 项自动断言与 16 项接口绑定检查通过；包含实际数据库类别、模拟新增模板、各类别独立存储与战斗伤害边界。未启动游戏，实战/Fika 验收待用户完成。`r`n- 源码 mods-dev/service-stars；验收说明 ServiceStars-README.md；发行包 ServiceStars-1.2.1-SPT-4.1.6.zip。未修改主档案、核心程序集或游戏数据库。`r`n"
 [IO.File]::AppendAllText((Join-Path $gameRoot '本地更改日志.md'), $message, (New-Object Text.UTF8Encoding($false)))
 Write-Host 'Deployed Service Stars only. No server or game was started.'
}
