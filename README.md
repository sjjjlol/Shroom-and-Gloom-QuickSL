# Shroom Quick SL · 菇域幽魂快速重试

**简体中文** | [English](README.en.md)

《Shroom and Gloom》的非官方 Windows 游戏内战斗重试插件。进入战斗时捕获内存快照，在战斗中按 **F8** 请求恢复快照并重新加载游戏场景，无需退出游戏进程。

> **0.1.0 实验版**：已通过编译及插件加载验证，完整战斗恢复仍待实测。暂不视为稳定版本。

## 安装

1. 退出游戏，备份 `%USERPROFILE%\AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\SaveData`。
2. 安装 [BepInEx 6 Unity IL2CPP Windows x64](https://builds.bepinex.dev/projects/bepinex_be)，将加载器解压至游戏 EXE 所在目录。开发使用 `6.0.0-be.791`。不要使用 Mono 或 BepInEx 5 版本。
3. 启动一次游戏，等待 BepInEx 生成接口文件，再退出。
4. 将下载包中的 `BepInEx/plugins/ShroomQuickSL.dll` 复制到同名目录。
5. 启动游戏，检查 `BepInEx/LogOutput.log` 是否出现 `Quick SL loaded`。

## 使用

进入战斗，操作后按 **F8**。仅当前游戏窗口处于前台、战斗仍在进行、快照存在且场景未在加载时响应。重复触发间隔至少 5 秒。

快照保存在内存中，关闭游戏即丢失。插件不会提供任意时刻存档，也不能在战斗结束后撤销结果。当前恢复入口复用游戏的继续游戏流程，实际回退位置和随机结果需实测确认。

## 卸载与排错

退出游戏后删除 `BepInEx/plugins/ShroomQuickSL.dll` 即可卸载本插件。

- 没有加载日志：确认安装的是 IL2CPP x64 加载器，且 DLL 放在正确目录。
- F8 无响应：确认已经在安装插件后进入战斗，游戏有焦点，战斗尚未结束。
- 恢复状态不正确：停止使用，保留日志并提交 issue，说明游戏版本、战斗位置、预期和实际结果。分享日志前检查个人路径等信息。
- 游戏更新后：先等待兼容性确认。本插件依赖游戏内部接口。

## 开发

需要 Windows、PowerShell 7（含 Roslyn 编译库）和已启动过一次的 BepInEx IL2CPP 游戏环境。

```powershell
pwsh -File ./build.ps1 -GamePath 'D:\Steam\steamapps\common\Shroom and Gloom'
```

输出：`dist/ShroomQuickSL.dll`。编译引用本机游戏生成的接口程序集。仓库和下载包不包含游戏程序集、游戏资源或个人存档。

开发目标：Windows x64、Unity `2022.3.62f3`、BepInEx `6.0.0-be.791`。其他游戏构建尚未验证。

本项目与游戏开发商及发行商无关联。源码采用 MIT 许可证；BepInEx 由其上游独立分发。
