using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cards = Il2CppSystem.Collections.Generic.List<GameplayCardData>;

namespace ShroomQuickSL {
[BepInPlugin("io.github.shroomquicksl", "Shroom Quick SL", "0.2.0")]
public class Plugin : BasePlugin {
    internal static Plugin Instance;
    internal static Snapshot Current;
    internal static Snapshot Restoring;
    internal static float Deadline;
    internal static string Status = "尚无 SL 检查点";
    public override void Load() {
        Instance = this;
        new Harmony("io.github.shroomquicksl").PatchAll();
        AddComponent<PauseMenuSL>();
        Log.LogInfo("Quick SL 0.2.0 loaded. Open Esc menu to retry; F8 removed.");
    }
    internal static V_Player Player() { return UnityEngine.Object.FindObjectOfType<V_Player>(); }
    internal static void Capture(string kind, Reward reward = null) {
        if (Restoring != null) return;
        try {
            var save = ServiceLocator.GetSystem<S_SaveManager>();
            var p = Player();
            if (save == null || p == null || p.Model == null || p.Tunnel == null || !save.HasRunInMemory()) return;
            var run = save.GetRunReadOnlyInMemory().DeepCopy();
            // Rebuild live state, following V_Player.RunSaveCheckpoint's data builder.
            run.PlayerState = p.Model.GetSaveState();
            p.SaveCurrentTunnelPath(run);
            run.PlayerClassIdentity = p.StartingClass.AuthoredData.Identity;
            bool invalid;
            var decks = save.BuildGameplaySaveData(out invalid);
            if (invalid) throw new InvalidOperationException("Gameplay deck snapshot invalid");
            run.CombatDeckSave = decks.Item1;
            run.ExploreDeckSave = decks.Item2;
            run.ForesightDeckSave = save.BuildForesightSaveData(out invalid);
            if (invalid) throw new InvalidOperationException("Foresight snapshot invalid");
            run.SharedProperties = p.SharedProperties.Model.GetSaveData();
            run.CampPlacements = ServiceLocator.GetSystem<CampManager>().GetSaveData();
            run.WaitingVeterans = ServiceLocator.GetSystem<S_EnemyEncounterManager>().ExportWaitingVeterans();
            bool combatReward = reward != null && p.Tunnel.CombatRoomInstance != null && p.Tunnel.CombatRoomInstance.CardRewardChoiceUI != null && p.Tunnel.CombatRoomInstance.CardRewardChoiceUI.name == reward.Name;
            bool combatFinished = kind != "战斗" && (p.Tunnel.IsCombatComplete() || combatReward);
            run.LastCheckpoint = combatFinished ? "AfterCombat" : "EnterTunnel";
            run.SaveInvalidatedDueToError = false;
            Current = new Snapshot { Run = run.DeepCopy(), Meta = save.GetMetaReadOnlyInMemory().DeepCopy(),
                Kind = kind, Position = p.Tunnel.transform.InverseTransformPoint(p.transform.position),
                Rotation = Quaternion.Inverse(p.Tunnel.transform.rotation) * p.transform.rotation,
                CombatFinished = combatFinished, Reward = reward };
            Status = "重开 SL · " + kind;
            Instance.Log.LogInfo("Captured live " + kind + " checkpoint: " + run.CurrentTunnelPath + ", " + run.LastCheckpoint);
        } catch (Exception e) {
            Current = null; Status = "SL 快照失败，请查看日志";
            Instance.Log.LogError(e);
        }
    }
    internal static void Retry() {
        try {
            var boot = ServiceLocator.GetSystem<S_BootManager>();
            var save = ServiceLocator.GetSystem<S_SaveManager>();
            var p = Player();
            if (Current == null || Restoring != null || boot == null || boot.BusyLoading || p == null || p.BusySavingInCheckpoint) return;
            if (!boot.IsMainGameLoop() || save.GetRunReadOnlyInMemory().RunStatsId != Current.Run.RunStatsId) return;
            ServiceLocator.GetSystem<S_PlayerValuesDisplay>().HidePauseMenu();
            Current.SpawnApplied = false;
            Restoring = Current;
            Deadline = Time.realtimeSinceStartup + 60;
            save._inMemoryRun = Current.Run.DeepCopy();
            save._inMemoryMeta = Current.Meta.DeepCopy();
            // Discard pending snapshots from the abandoned timeline.
            save._inMemoryRunSnapshotJson = null;
            save._inMemoryMetaSnapshotJson = null;
            boot.ContinueGame = true;
            boot.SaveDataToLoad = Current.Run.DeepCopy();
            boot.ReloadGameScene();
            Instance.Log.LogInfo("SL reload requested: " + Current.Kind);
        } catch (Exception e) { Restoring = null; Status = "SL 失败，请查看日志"; Instance.Log.LogError(e); }
    }
    [HarmonyPatch(typeof(CombatRoom), "OnEncounterTriggered")]
    class CombatCapture { static void Prefix() { Capture("战斗"); } }
    [HarmonyPatch(typeof(ExploreRoom), "OnEncounterTriggered")]
    class ExploreCapture { static void Prefix() { Capture("探索"); } }
    [HarmonyPatch(typeof(CardChoiceEncounterUI), "OfferCards")]
    class RewardCapture {
        static void Prefix(CardChoiceEncounterUI __instance, BucketCategoryBase category, int numberOfTimesCast, int maxCardCount, bool resetToStartPositionOnEnd) {
            Capture("奖励选择", new Reward { Name = __instance.name, Category = category, Times = numberOfTimesCast, Count = maxCardCount, Reset = resetToStartPositionOnEnd });
        }
    }
    [HarmonyPatch(typeof(CardChoiceEncounterUI), "OfferSpecificCards")]
    class SpecificRewardCapture {
        static void Prefix(CardChoiceEncounterUI __instance, Cards specificCards, Cards allReRollCardOptions, LocalizedFallbackText title, bool resetToStartPositionOnEnd, LocalizedFallbackText detailText) {
            Capture("奖励选择", new Reward { Name = __instance.name, Specific = true, Cards = Copy(specificCards), Options = Copy(allReRollCardOptions), Title = title, Detail = detailText, Reset = resetToStartPositionOnEnd });
        }
        static Cards Copy(Cards source) { if (source == null) return null; var result = new Cards(); foreach (var card in source) result.Add(card); return result; }
    }
    [HarmonyPatch(typeof(TunnelGenerator), "GetCheckpointLoadInLocation")]
    class SpawnOverride {
        static void Postfix(TunnelGenerator __instance, ref SaveLoadCheckpoint.CheckpointSpawnInfo __result) {
            if (Restoring == null || __result == null) return;
            __result.Position = __instance.transform.TransformPoint(Restoring.Position);
            __result.Rotation = __instance.transform.rotation * Restoring.Rotation;
            __result.CombatFinished = Restoring.CombatFinished;
            Restoring.SpawnApplied = true;
            Instance.Log.LogInfo("SL spawn position restored.");
        }
    }
}
internal class Snapshot {
    internal RunSaveData Run; internal MetaSaveData Meta;
    internal string Kind; internal Vector3 Position; internal Quaternion Rotation;
    internal bool CombatFinished; internal bool SpawnApplied; internal Reward Reward;
}
internal class Reward {
    internal string Name; internal BucketCategoryBase Category;
    internal int Times, Count; internal bool Reset, Specific;
    internal Cards Cards, Options; internal LocalizedFallbackText Title, Detail;
    internal void Open(CardChoiceEncounterUI ui) {
        if (Specific) ui.OfferSpecificCards(Cards, Options, Title, Reset, Detail);
        else ui.OfferCards(Category, Times, Count, Reset);
    }
}
public class PauseMenuSL : MonoBehaviour {
    Button button; TextMeshProUGUI label;
    float readyAt;
    public PauseMenuSL(IntPtr pointer) : base(pointer) { }
    public void Update() {
        try {
            var display = ServiceLocator.GetSystem<S_PlayerValuesDisplay>();
            if (display != null && display._pauseMenu != null && display.PauseMenuIsOpen()) {
                if (button == null) CreateButton(display._pauseMenu);
                label.text = Plugin.Status;
                button.interactable = Plugin.Current != null && Plugin.Restoring == null;
            }
            if (Plugin.Restoring == null) { readyAt = 0; return; }
            if (Time.realtimeSinceStartup > Plugin.Deadline) {
                Plugin.Restoring = null; readyAt = 0;
                Plugin.Status = "SL 恢复超时，请查看日志";
                Plugin.Instance.Log.LogError("SL timed out waiting for restored scene."); return;
            }
            var boot = ServiceLocator.GetSystem<S_BootManager>();
            var p = Plugin.Player();
            if (!Plugin.Restoring.SpawnApplied || boot == null || boot.BusyLoading || p == null || p.Model == null || p.BusySavingInCheckpoint) { readyAt = 0; return; }
            if (readyAt == 0) { readyAt = Time.realtimeSinceStartup + 1; return; }
            if (Time.realtimeSinceStartup < readyAt) return;
            var reward = Plugin.Restoring.Reward;
            if (reward != null) {
                CardChoiceEncounterUI target = null;
                foreach (var candidate in UnityEngine.Object.FindObjectsOfType<CardChoiceEncounterUI>(true)) {
                    if (candidate.name != reward.Name) continue;
                    if (target != null) throw new InvalidOperationException("Ambiguous reward UI: " + reward.Name);
                    target = candidate;
                }
                if (target == null) throw new InvalidOperationException("Reward UI not found: " + reward.Name);
                reward.Open(target);
            }
            Plugin.Restoring = null; readyAt = 0;
            Plugin.Instance.Log.LogInfo("SL restoration completed; playtest state correctness.");
        } catch (Exception e) { Plugin.Restoring = null; Plugin.Status = "SL 失败，请查看日志"; Plugin.Instance.Log.LogError(e); }
    }
    void CreateButton(GameObject menu) {
        var go = new GameObject("QuickSLButton", new Il2CppSystem.Type[] { Il2CppType.Of<RectTransform>() });
        go.transform.SetParent(menu.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.78f, 0.5f);
        rect.sizeDelta = new Vector2(330, 64); rect.anchoredPosition = Vector2.zero;
        var image = go.AddComponent<Image>(); image.color = new Color(0.12f, 0.16f, 0.12f, 0.96f);
        button = go.AddComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener((UnityEngine.Events.UnityAction)(Action)Plugin.Retry);
        var textGo = new GameObject("Label", new Il2CppSystem.Type[] { Il2CppType.Of<RectTransform>() });
        textGo.transform.SetParent(go.transform, false);
        var tr = textGo.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(8, 4); tr.offsetMax = new Vector2(-8, -4);
        label = textGo.AddComponent<TextMeshProUGUI>();
        var sample = menu.GetComponentInChildren<TextMeshProUGUI>(true);
        if (sample != null && sample != label) label.font = sample.font;
        label.text = Plugin.Status; label.fontSize = 24; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
    }
}
}
