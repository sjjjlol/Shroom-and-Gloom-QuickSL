param([string]$GamePath='D:\Steam\steamapps\common\Shroom and Gloom')
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -GamePath $GamePath
$source=Get-Content -Raw (Join-Path $PSScriptRoot 'src\Plugin.cs')
if ($source -notmatch 'BepInPlugin\([^\r\n]+"(\d+\.\d+\.\d+)"\)') { throw 'Plugin version missing' }
$version=$Matches[1]
$stage=Join-Path $PSScriptRoot ('dist\package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $stage 'BepInEx\plugins') -Force | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'dist\ShroomQuickSL.dll') (Join-Path $stage 'BepInEx\plugins\ShroomQuickSL.dll')
foreach($name in @('README.md','README.en.md','LICENSE','CHANGELOG.md','Install.ps1','Install.cmd','Uninstall.cmd')) { Copy-Item (Join-Path $PSScriptRoot $name) $stage }
$hashes=Get-ChildItem $stage -Recurse -File | Sort-Object FullName | ForEach-Object {
    (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $_.FullName.Substring($stage.Length+1).Replace('\','/')
}
[IO.File]::WriteAllLines((Join-Path $stage 'SHA256SUMS.txt'),$hashes,[Text.UTF8Encoding]::new($false))
$zip=Join-Path $PSScriptRoot "dist\ShroomQuickSL-$version-win-x64.zip"
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
$hash=(Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$zip.sha256", "$hash  $([IO.Path]::GetFileName($zip))`n",[Text.UTF8Encoding]::new($false))
Write-Host "Package: $zip"
