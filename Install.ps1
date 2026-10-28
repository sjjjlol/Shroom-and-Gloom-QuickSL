param([string]$GamePath, [switch]$Uninstall)
$ErrorActionPreference = 'Stop'
if (!$GamePath) { $GamePath = Read-Host 'Paste the game folder path (Steam > Manage > Browse local files)' }
$GamePath = $GamePath.Trim().Trim('"')
$root = (Resolve-Path -LiteralPath $GamePath).Path
if (!(Test-Path -LiteralPath (Join-Path $root 'Shroom and Gloom.exe'))) { throw 'Not a Shroom and Gloom game folder.' }
if (Get-Process -Name 'Shroom and Gloom' -ErrorAction SilentlyContinue) { throw 'Exit the game before installing or uninstalling.' }
$target = Join-Path $root 'BepInEx\plugins\ShroomQuickSL.dll'
if ($Uninstall) {
    if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
    Write-Host 'Plugin removed. BepInEx and saves were kept.'
    return
}
if (!(Test-Path -LiteralPath (Join-Path $root 'BepInEx\core\BepInEx.Unity.IL2CPP.dll')) -or !(Test-Path -LiteralPath (Join-Path $root 'winhttp.dll'))) {
    throw 'Install BepInEx 6 Unity IL2CPP Windows x64 first. See README.en.md / README.md.'
}
if (!(Test-Path -LiteralPath (Join-Path $root 'BepInEx\interop\Assembly-CSharp.dll'))) {
    throw 'Launch the game once with BepInEx, wait until the title screen, then exit and run this installer again.'
}
$source = Join-Path $PSScriptRoot 'BepInEx\plugins\ShroomQuickSL.dll'
if (!(Test-Path -LiteralPath $source)) { throw 'Plugin DLL missing. Download and extract the release ZIP, not Source code.' }
$manifest = Join-Path $PSScriptRoot 'SHA256SUMS.txt'
if (!(Test-Path -LiteralPath $manifest)) { throw 'Package checksum manifest missing. Extract the complete release ZIP.' }
$entry = Get-Content -LiteralPath $manifest | Where-Object { $_ -match '  BepInEx/plugins/ShroomQuickSL.dll$' }
if (@($entry).Count -ne 1 -or (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne ($entry -split '  ',2)[0]) { throw 'Plugin checksum mismatch. Download a fresh release ZIP.' }
$backup = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) ('ShroomQuickSL\Backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$saves = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\SaveData'
New-Item -ItemType Directory -Path $backup -Force | Out-Null
if (Test-Path -LiteralPath $saves) { Copy-Item -LiteralPath $saves -Destination (Join-Path $backup 'SaveData') -Recurse }
if (Test-Path -LiteralPath $target) { Copy-Item -LiteralPath $target -Destination (Join-Path $backup 'ShroomQuickSL.dll') }
New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
Copy-Item -LiteralPath $source -Destination $target -Force
if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw 'Installed file verification failed.' }
Write-Host "Installed. Backups: $backup"
Write-Host 'Start the game, press Esc and choose the SL button below Continue.'
