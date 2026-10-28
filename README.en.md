# Shroom Quick SL

[简体中文](README.md) | **English**

An unofficial Windows in-game retry plugin for **Shroom and Gloom**. Captures live state at combat, exploration, and card reward entry. Press **Esc**, then click **重开 SL** (Retry SL) below Continue in the pause-menu button list to restore without restarting the game process. The in-game button currently uses Chinese labels.

> **0.3.0 experimental**: adds foresight reward capture, native checkpoint fallback and an integrated pause-menu button. Compilation has passed; layout and restoration still require player verification.

## Installation

1. Exit the game and back up `%USERPROFILE%\AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\SaveData`.
2. Install [BepInEx 6 Unity IL2CPP Windows x64](https://builds.bepinex.dev/projects/bepinex_be) beside the game executable. Development uses `6.0.0-be.791`. Mono builds and BepInEx 5 are unsuitable.
3. Launch the game once to generate interop assemblies, then exit.
4. Copy `BepInEx/plugins/ShroomQuickSL.dll` from the download into the matching game directory.
5. Launch the game and check `BepInEx/LogOutput.log` for `Quick SL loaded`.

## Usage

Press Esc and click **重开 SL** below Continue in the central button list. Combat, exploration and reward screens can all request restoration to the latest valid checkpoint. When no plugin snapshot exists, the native game checkpoint is used. Failed captures preserve the previous valid snapshot. Wait for scene loading or save writes to finish before retrying. F8 has been removed.

Snapshots are taken before combat triggers, exploration begins, or ordinary card / foresight reward choices open. Restoration rebuilds the scene and reopens the reward UI when needed. Fixed rewards retain their candidates; random rewards use the original offering logic again, with no guarantee that outcomes change.

Plugin snapshots are memory-only and disappear on exit; after restarting, the native game checkpoint is used. New captured stages or native checkpoints replace older snapshots. This is not an arbitrary save-state system. Unserialized room interactions, statistics and achievements may not roll back. Reward costs, duplicate cards, first-combat restoration and later rooms need verification. Stop testing and report unexpected behavior.

## Uninstall and troubleshooting

Exit the game and delete `BepInEx/plugins/ShroomQuickSL.dll` to remove this plugin.

- No loading message: check that the IL2CPP x64 loader is installed and the DLL is in the correct directory.
- Button disabled: enter combat, exploration, or a reward choice to capture a snapshot, then wait for loading to finish.
- Failure or timeout: inspect SL errors in the log rather than retrying repeatedly.
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
