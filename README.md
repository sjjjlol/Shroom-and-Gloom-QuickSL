# Shroom Quick SL · 菇域幽魂快速重试

**简体中文** | [English](README.en.md)

《Shroom and Gloom》的非官方 Windows 游戏内战斗重试插件。在战斗、探索和奖励选择开始时捕获当前状态。按 **Esc** 打开暂停菜单，点击按钮列表中“继续”下方的 **重开 SL** 请求恢复，无需退出游戏进程。

> **0.3.0 实验版**：补充先见奖励快照、游戏检查点兜底及菜单列表按钮。已通过编译；菜单布局和实际恢复效果仍待玩家验证。

## 安装

1. 退出游戏，备份 `%USERPROFILE%\AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\SaveData`。
2. 安装 [BepInEx 6 Unity IL2CPP Windows x64](https://builds.bepinex.dev/projects/bepinex_be)，将加载器解压至游戏 EXE 所在目录。开发使用 `6.0.0-be.791`。不要使用 Mono 或 BepInEx 5 版本。
3. 启动一次游戏，等待 BepInEx 生成接口文件，再退出。
4. 将下载包中的 `BepInEx/plugins/ShroomQuickSL.dll` 复制到同名目录。
5. 启动游戏，检查 `BepInEx/LogOutput.log` 是否出现 `Quick SL loaded`。

## 使用

按 Esc，点击中间按钮列表里“继续”下方的“重开 SL”。战斗、探索和奖励界面均可请求返回最近的有效存档点；没有插件快照时，会使用游戏已有的检查点。新快照捕获失败不会清除上一个有效点。场景加载或存档写入期间需等待完成，避免重入。F8 已移除。

战斗快照捕获于触发战斗前；探索快照捕获于进入探索时；普通卡牌和先见奖励快照捕获于打开选牌前。恢复会重建场景，奖励模式会重新打开选牌界面。固定奖励保留原候选，随机奖励再次调用游戏原有发牌逻辑，不保证随机结果一定改变。

插件快照仅保存在内存中，退出游戏后丢失；重新进入游戏会使用游戏自身的存档点，新的阶段或游戏检查点会替换旧快照。不是任意时刻存档。不能保证恢复所有未序列化的房间交互、统计和成就状态；奖励消费、卡牌重复、第一战和后续房间仍需验证。遇到异常请停止测试并报告。

## 卸载与排错

退出游戏后删除 `BepInEx/plugins/ShroomQuickSL.dll` 即可卸载本插件。

- 没有加载日志：确认安装的是 IL2CPP x64 加载器，且 DLL 放在正确目录。
- 菜单按钮不可用：先进入战斗、探索或打开奖励选择以生成快照，等待场景加载完成。
- 按钮出现失败或超时：查看日志中 SL 错误，不要连续重复操作。
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
