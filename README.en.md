# Shroom Quick SL

[简体中文](README.md) | **English**

An unofficial Windows mod for **Shroom and Gloom**. Press **Esc → 重开 SL** (Retry SL), below Continue, to restore the latest native checkpoint without restarting the game process. The in-game button currently uses Chinese labels.

**0.4.1 public beta · MIT licensed.** One player completed a full run without reported visible bugs; all 8 SL attempts in that session passed health verification. Known issues remain below.

## Download and installation

Download `ShroomQuickSL-0.4.1-win-x64.zip` from [Releases](https://github.com/sjjjlol/Shroom-and-Gloom-QuickSL/releases). **Do not choose Source code**, which has no compiled DLL. A SHA-256 checksum for the ZIP is provided.

Test environment: Windows x64, game `0.6.41`, Unity `2022.3.62f3`, BepInEx `6.0.0-be.791`. Other versions are unverified.

1. In Steam, right-click the game → Manage → Browse local files. Note the folder containing `Shroom and Gloom.exe`, then exit the game.
2. If needed, install **BepInEx 6 / Unity.IL2CPP / win-x64** from the [official builds site](https://builds.bepinex.dev/projects/bepinex_be) (build 791 was tested). Extract into the game folder, so `winhttp.dll` and `BepInEx` appear beside the EXE. Do not use Mono, x86, or BepInEx 5.
3. Launch once, wait for the title screen, then exit. Initial interop generation may take longer.
4. Extract the mod release ZIP into a separate folder. Double-click `Install.cmd`, paste the game folder path, and press Enter. It checks the loader and interop assemblies, verifies the DLL, backs up saves and any previous plugin, and installs it.
5. Start or continue a run and press Esc. Successful loading produces `Quick SL 0.4.1` in `BepInEx/LogOutput.log`.

The scripts make no network requests or system execution-policy changes. No administrator access is needed when the game folder is writable. Backups of the current Windows user's saves are stored in `%LOCALAPPDATA%\ShroomQuickSL\Backups\timestamp`.

**Manual installation:** back up `%USERPROFILE%\AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\SaveData`, install the loader, then copy `BepInEx/plugins/ShroomQuickSL.dll` from the ZIP to the matching game folder.

## Usage and limitations

- Combat, exploration, and reward screens can request SL when the pause menu is available and a valid checkpoint exists. Wait for loading and save writes to finish.
- The target is the **latest native checkpoint**, typically tunnel entry or post-combat. This is not an arbitrary save-state system or a guarantee of restarting the current encounter or reward screen.
- A checkpoint before rewards may let you encounter them again. Reward rerolling or directly reselecting already claimed rewards is not guaranteed.
- Checkpoint copies are memory-only. Continuing after a restart uses the game's own save. There is no F8 shortcut.
- Current and maximum health are verified after loading. Failure or timeout disables SL until the game loading flow resets it. This does not verify every card, resource, statistic, or achievement.

## Updates, uninstalling, and backups

To update, exit the game and run `Install.cmd` from the new extracted release. Avoid keeping multiple DLL versions installed.

To uninstall, exit and run `Uninstall.cmd`, or delete `BepInEx/plugins/ShroomQuickSL.dll`. BepInEx, other mods, and saves are kept.

Backups are not automatically restored. To restore one manually, exit the game, preserve the current SaveData separately, then copy the desired backup back. Pay attention to local/cloud conflicts if Steam Cloud is enabled.

## Known issues and reporting

- The full-run log still contains 4 null-reference exceptions in `M_Hand.ResetExtraCardsToDrawNextTurn` / `CombatRoutine` near old-scene unloading. No corresponding visible failure was reported. Cause and impact remain under investigation; this is not claimed fixed.
- Death handling, every reward type, other mod combinations, and later game versions have not been exhaustively tested.
- Disabled button: wait for loading/writes and ensure a native checkpoint exists. Missing button: check loader type and plugin loading logs.
- For softlocks, incorrect state, or timeouts, stop retrying and open an [issue](https://github.com/sjjjlol/Shroom-and-Gloom-QuickSL/issues) with versions, encounter, steps, expected and actual behavior. Include relevant excerpts from `BepInEx/LogOutput.log` and `Player.log` in the LocalLow game folder above. Remove personal paths and account identifiers before sharing. Save uploads are not required.

## Building from source

Requires PowerShell 7 with Roslyn, a local game installation, and generated BepInEx IL2CPP interop assemblies:

```powershell
pwsh -File ./build.ps1 -GamePath 'D:\Steam\steamapps\common\Shroom and Gloom'
pwsh -File ./package.ps1 -GamePath 'D:\Steam\steamapps\common\Shroom and Gloom'
```

Outputs go to `dist/`. Packages contain the plugin DLL, installer scripts, documentation, license, and checksums; no game assemblies, assets, personal saves, or BepInEx loader. Builds need local game interfaces, so hosted compilation CI is not configured.

[Changelog](CHANGELOG.md) · [MIT license](LICENSE). Not affiliated with the game developer, publisher, or BepInEx team.
