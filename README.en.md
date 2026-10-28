# Shroom Quick SL

[简体中文](README.md) | **English**

An unofficial Windows in-game combat retry plugin for **Shroom and Gloom**. Captures an in-memory snapshot when combat starts; press **F8** during combat to request snapshot restoration and a scene reload without restarting the game process.

> **0.1.0 experimental**: compilation and plugin loading have been verified. End-to-end combat restoration still requires playtesting. This is not a stable release.

## Installation

1. Exit the game and back up `%USERPROFILE%\AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\SaveData`.
2. Install [BepInEx 6 Unity IL2CPP Windows x64](https://builds.bepinex.dev/projects/bepinex_be) beside the game executable. Development uses `6.0.0-be.791`. Mono builds and BepInEx 5 are unsuitable.
3. Launch the game once to generate interop assemblies, then exit.
4. Copy `BepInEx/plugins/ShroomQuickSL.dll` from the download into the matching game directory.
5. Launch the game and check `BepInEx/LogOutput.log` for `Quick SL loaded`.

## Usage

Enter combat, make your moves, then press **F8**. The hotkey only responds when the game has focus, combat is active, a snapshot exists, and no scene is loading. Requests have a minimum five-second cooldown.

Snapshots are memory-only and disappear when the game closes. This is not an arbitrary save-state system and cannot undo completed combat. Restoration reuses the game continuation flow; the precise restart position and random outcomes still need playtesting.

## Uninstall and troubleshooting

Exit the game and delete `BepInEx/plugins/ShroomQuickSL.dll` to remove this plugin.

- No loading message: check that the IL2CPP x64 loader is installed and the DLL is in the correct directory.
- F8 does nothing: enter combat after installing, focus the game window, and retry before combat ends.
- Incorrect restored state: stop using the plugin and report the game version, encounter, expected and actual results, and relevant logs. Review logs for personal paths before sharing.
- After game updates: wait for compatibility confirmation; this plugin uses internal game interfaces.

## Development

Requires Windows, PowerShell 7 with Roslyn libraries, and a BepInEx IL2CPP installation that has generated game interop assemblies.

```powershell
pwsh -File ./build.ps1 -GamePath 'D:\Steam\steamapps\common\Shroom and Gloom'
```

Output: `dist/ShroomQuickSL.dll`. Compilation references locally generated game interop assemblies. Game assemblies, assets, and personal saves are excluded from the repository and download.

Development target: Windows x64, Unity `2022.3.62f3`, BepInEx `6.0.0-be.791`. Other game builds are unverified.

Not affiliated with the game developer or publisher. Source is MIT licensed. BepInEx is distributed independently by its upstream project.
