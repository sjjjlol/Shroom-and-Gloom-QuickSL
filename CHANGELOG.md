# Changelog / 更新记录

## 0.4.1 — Public beta / 公开测试版

- Restore native checkpoints from the pause menu without restarting the process. / 暂停菜单恢复原生检查点，无需重启进程。
- Check service availability to prevent repeated errors during startup and unloading. / 查询前检查服务状态，消除日志刷屏。
- Verify current and maximum health and prevent overlapping restores. / 校验当前及最大血量，防止重复加载。
- Add install/uninstall scripts, save/plugin backups, checksums and bilingual instructions. / 加入安装卸载脚本、存档和旧插件备份、校验与双语说明。
- Player completed one full run; 8 SL health checks passed. Four combat-coroutine exceptions remain near scene unloading. / 玩家完成一局，8 次 SL 血量检查通过；卸载附近仍有 4 次战斗协程异常。

## Earlier development / 早期开发

0.1–0.3 used custom combat/reward snapshots with incorrect restoration and softlocks. 0.4.0 removed them in favor of native checkpoints. These builds were not publicly released and should not be installed.

0.1–0.3 自定义战斗与奖励快照存在恢复错误和卡关。0.4.0 移除这些快照，改用原生检查点。旧版未公开发布，不建议安装。
