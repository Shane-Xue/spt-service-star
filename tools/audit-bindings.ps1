$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Add-Type -Path (Join-Path $root 'BepInEx\core\Mono.Cecil.dll')
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $root 'EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll'))
function Assert($condition, $description) {
 if (!$condition) { throw "Binding mismatch: $description" }
 Write-Host "PASS $description"
}
$stats = $asm.MainModule.Types | Where-Object FullName -eq 'EFT.BaseStatisticsManager'
$kill = @($stats.Methods | Where-Object Name -eq 'OnEnemyKill')
Assert ($kill.Count -eq 1) 'unique kill hook'
Assert ($kill[0].Parameters[0].ParameterType.FullName -eq 'EFT.Ballistics.DamageInfo') 'kill damage argument'
Assert ($kill[0].Parameters[1].ParameterType.FullName -eq 'EFT.EDamageType') 'kill damage type argument'
Assert ($kill[0].Parameters[6].Name -eq 'playerProfileId') 'victim ID argument index'
Assert ($kill[0].Parameters[8].Name -eq 'groupId') 'victim group argument index'
Assert (@($stats.Fields | Where-Object { $_.Name -eq 'Player' -and $_.FieldType.FullName -eq 'EFT.Player' }).Count -eq 1) 'statistics owner field'
$panel = $asm.MainModule.Types | Where-Object FullName -eq 'EFT.UI.ItemSpecificationPanel'
foreach ($name in @('Show','OnItemExaminedEvent')) {
 Assert (@($panel.Methods | Where-Object Name -eq $name).Count -eq 1) "unique UI hook $name"
}
foreach ($name in @('_item','_charPanelContainer','_masteringText','_itemLabels')) {
 Assert (@($panel.Fields | Where-Object Name -eq $name).Count -eq 1) "UI field $name"
}
$labels = $asm.MainModule.Types | Where-Object FullName -eq 'EFT.UI.ItemInfoWindowLabels'
Assert (@($labels.Fields | Where-Object Name -eq '_caption').Count -eq 1) 'localized caption font source'
$localization = $asm.MainModule.Types | Where-Object FullName -eq 'EFT.LocalizationManager'
Assert (@($localization.Methods | Where-Object Name -eq 'get_Culture').Count -eq 1) 'current game locale API'
Assert (@($localization.Methods | Where-Object Name -eq 'get_Instance').Count -eq 1) 'localization manager instance API'
$asm.Dispose()
$core = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $root 'BepInEx\plugins\spt\spt-core.dll'))
$plugin = $core.MainModule.Types | Where-Object FullName -eq 'SPT.Core.SPTCorePlugin'
$attribute = $plugin.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'BepInEx.BepInPlugin' }
Assert ($attribute.ConstructorArguments[0].Value -eq 'com.SPT.core') 'BepInEx dependency GUID'
$core.Dispose()
