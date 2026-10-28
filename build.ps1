param([string]$GamePath='D:\Steam\steamapps\common\Shroom and Gloom')
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
$refs = [System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
foreach ($folder in @('dotnet','BepInEx\core','BepInEx\interop')) {
 foreach ($dll in (Get-ChildItem -LiteralPath (Join-Path $GamePath $folder) -Filter '*.dll')) {
  try { [Reflection.AssemblyName]::GetAssemblyName($dll.FullName) | Out-Null } catch { continue }
  $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($dll.FullName))
 }
}
$tree=[Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText((Join-Path $PWD 'src\Plugin.cs')))
$options=[Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$compilation=[Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('ShroomQuickSL',[Microsoft.CodeAnalysis.SyntaxTree[]]@($tree),$refs,$options)
New-Item -ItemType Directory -Force dist | Out-Null
$stream=[IO.File]::Create((Join-Path $PWD 'dist\ShroomQuickSL.dll'))
try { $result=$compilation.Emit($stream) } finally { $stream.Dispose() }
$result.Diagnostics | ForEach-Object { $_.ToString() }
if (!$result.Success) { throw 'Compilation failed' }
Write-Host 'Built dist/ShroomQuickSL.dll'
