param([string]$Assembly, [string]$Pattern, [string]$Method = '', [switch]$IL, [switch]$MethodsOnly)
$ErrorActionPreference = 'Stop'
Add-Type -Path 'M:\SPT\BepInEx\core\Mono.Cecil.dll'
$a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $Assembly).Path)
function Flatten($types) { foreach($entry in $types) { $entry; Flatten $entry.NestedTypes } }
foreach ($t in (Flatten $a.MainModule.Types)) {
 if ($t.FullName -notmatch $Pattern) { continue }
 "TYPE $($t.FullName) : $($t.BaseType)"
 if (!$MethodsOnly) {
 foreach ($p in $t.Properties) { "  PROP $($p.PropertyType) $($p.Name)" }
 foreach ($f in $t.Fields) { "  FIELD $($f.FieldType) $($f.Name)" }
 }
 foreach ($m in $t.Methods) {
  if ($Method -and $m.Name -notmatch $Method) { continue }
  "  $m"
  if ($IL -and $m.HasBody) { foreach ($i in $m.Body.Instructions) { "    $i" } }
 }
}
$a.Dispose()
