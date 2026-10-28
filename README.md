# Shroom Quick SL · 菇域幽魂快速读档

**简体中文** | [English](README.en.md)

《Shroom and Gloom》的非官方 Windows 插件。按 **Esc → 重开 SL**，返回最近的游戏原生检查点，无需重启游戏进程。按钮位于暂停菜单中“继续”的下方。

**0.4.1 公开测试版 · MIT 开源。** 玩家已完成一局并反馈未遇到可见故障；该次日志中 8 次 SL 均通过血量校验。仍存在下文所列已知问题。

## 下载与安装

从 [Releases](https://github.com/sjjjlol/Shroom-and-Gloom-QuickSL/releases) 下载 `ShroomQuickSL-0.4.1-win-x64.zip`，**不要下载 Source code**，后者不含编译后的 DLL。发行页附 ZIP 的 SHA-256 校验文件。

测试环境：Windows x64、游戏 `0.6.41`、Unity `2022.3.62f3`、BepInEx `6.0.0-be.791`。其他版本尚未验证。

1. Steam 中右键游戏 → 管理 → 浏览本地文件，记下含 `Shroom and Gloom.exe` 的目录，退出游戏。
2. 尚未安装加载器时，从 [BepInEx 官方构建站](https://builds.bepinex.dev/projects/bepinex_be) 下载 **BepInEx 6 / Unity.IL2CPP / win-x64**（测试使用构建 791），解压到游戏目录。根目录应出现 `winhttp.dll` 和 `BepInEx`。不要选择 Mono、x86 或 BepInEx 5。
3. 启动一次游戏，等待标题界面出现后退出。首次生成接口可能较慢。
4. 将本插件发行 ZIP 解压到独立文件夹，双击 `Install.cmd`，粘贴游戏目录路径并回车。脚本检查加载器与接口、校验 DLL、备份存档和旧插件，然后安装。
5. 启动游戏，开始或继续一局，按 Esc 查看按钮。加载成功时 `BepInEx/LogOutput.log` 中出现 `Quick SL 0.4.1`。

脚本为英文提示，不联网，不更改系统执行策略；游戏目录可写时无需管理员权限。备份位置：`%LOCALAPPDATA%\ShroomQuickSL\Backups\日期时间`，备份的是当前 Windows 用户的存档。

**手动安装：**先备份 `%USERPROFILE%\AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\SaveData`，完成加载器安装后，将 ZIP 中 `BepInEx/plugins/ShroomQuickSL.dll` 放入游戏目录对应位置。

## 使用与限制

- 战斗、探索和奖励选择时，只要暂停菜单可用且已有有效检查点，即可请求 SL；加载或存档写入期间需等待。
- 目标是**最近的原生检查点**，通常为进入隧道或战斗后，不是任意时刻快照，不保证回到当前战斗或奖励界面的开头。
- 检查点在奖励之前时可能重新经历奖励；不保证随机结果改变，也不保证已领取奖励能直接重选。
- 检查点副本只在内存中保存。退出后继续游戏使用原生存档。没有 F8 快捷键。
- 加载后校验当前及最大血量；失败或超时会禁用 SL，直到游戏加载流程重置。此校验不覆盖全部卡牌、资源、统计或成就。

## 更新、卸载与备份

更新：退出游戏，解压新包并运行 `Install.cmd`。不要同时安装多个版本 DLL。

卸载：退出游戏，运行 `Uninstall.cmd` 并输入游戏目录，或手动删除 `BepInEx/plugins/ShroomQuickSL.dll`。仅移除本插件，保留 BepInEx、其他插件和存档。

备份不会自动覆盖存档。如需还原，退出游戏，另存当前 SaveData，再手动恢复所需备份。启用 Steam 云存档时留意云端与本地的冲突提示。

## 已知问题与反馈

- 本次完整游玩日志仍有 4 次旧场景卸载附近的 `M_Hand.ResetExtraCardsToDrawNextTurn` / `CombatRoutine` 空引用异常。玩家未报告对应故障，原因及影响仍在调查，不能视为已修复。
- 死亡流程、全部奖励类型、其他模组组合及后续游戏版本尚未全面验证。
- 按钮不可用：等待加载／写入完成，确认已有原生检查点。按钮缺失：检查 DLL 加载日志和加载器类型。
- 卡关、状态不符或超时时，请停止重复 SL，在 [Issues](https://github.com/sjjjlol/Shroom-and-Gloom-QuickSL/issues) 提交版本、所在阶段、操作步骤、预期与实际结果。附 `BepInEx/LogOutput.log` 和上述 LocalLow 游戏目录中 `Player.log` 的相关片段，公开前删去个人路径、账号标识。无需上传存档。

## 源码构建

需要 PowerShell 7（含 Roslyn）、本机游戏和已生成接口的 BepInEx IL2CPP 环境：

```powershell
pwsh -File ./build.ps1 -GamePath 'D:\Steam\steamapps\common\Shroom and Gloom'
pwsh -File ./package.ps1 -GamePath 'D:\Steam\steamapps\common\Shroom and Gloom'
```

输出位于 `dist/`。发行包只含插件 DLL、安装脚本、说明、许可证和校验清单，不含游戏程序集、资源、个人存档或 BepInEx 加载器。构建依赖本机游戏接口，未配置托管 CI 编译。

[更新记录](CHANGELOG.md) · [MIT 许可证](LICENSE)。本项目与游戏开发商、发行商及 BepInEx 团队无关联。
