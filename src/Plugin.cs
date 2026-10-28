using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cards = Il2CppSystem.Collections.Generic.List<GameplayCardData>;

namespace ShroomQuickSL {
[BepInPlugin("io.github.shroomquicksl", "Shroom Quick SL", "0.3.1")]
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
        Log.LogInfo("Quick SL 0.3.1 loaded. Open Esc menu to retry; F8 removed.");
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
            Status = Current == null ? "重开 SL" : "重开 SL · " + Current.Kind;
            Instance.Log.LogError(e);
        }
    }
    // Keep the last valid checkpoint even when a new capture cannot be made.
    internal static void EnsureCheckpoint(bool replace = false) {
        if (Restoring != null) return;
        var save = ServiceLocator.GetSystem<S_SaveManager>();
        if (save == null || !save.HasRunInMemory()) return;
        var run = save.GetRunReadOnlyInMemory();
        if (Current != null && Current.Run.RunStatsId != run.RunStatsId) Current = null;
        if (!replace && Current != null) return;
        if (run.SaveInvalidatedDueToError || run.RunComplete) return;
        Current = new Snapshot { Run = run.DeepCopy(), Meta = save.GetMetaReadOnlyInMemory().DeepCopy(), Kind = "最近存档", NativeCheckpoint = true };
        Status = "重开 SL";
        Instance.Log.LogInfo("Native checkpoint captured: " + run.CurrentTunnelPath + ", " + run.LastCheckpoint);
    }
    [HarmonyPatch(typeof(S_SaveManager), "SnapshotAllInMemoryForSaveToStorage")]
    class NativeCapture { static void Postfix() {
        try { EnsureCheckpoint(true); } catch (Exception e) { Instance.Log.LogError(e); }
    } }
    [HarmonyPatch(typeof(ForesightChoiceEncounterUI), "OfferForesights")]
    class ForesightCapture {
        static void Prefix(ForesightChoiceEncounterUI __instance, ForesightBucketCategory category, int cardCount, bool resetToStartPositionOnEnd) {
            var overrides = new Il2CppSystem.Collections.Generic.List<ForesightCardData>();
            if (__instance._overrideRewards != null) foreach (var card in __instance._overrideRewards) overrides.Add(card);
            Capture("先见奖励", new Reward { Name = __instance.name, Foresight = true, ForesightCategory = category, ForesightOverrides = overrides, Count = cardCount, Reset = resetToStartPositionOnEnd });
        }
    }
    internal static void Retry() {
        try {
            EnsureCheckpoint();
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
            if (Restoring.NativeCheckpoint) { Restoring.SpawnApplied = true; return; }
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
    internal bool NativeCheckpoint; internal bool CombatFinished; internal bool SpawnApplied; internal Reward Reward;
}
internal class Reward {
    internal bool Foresight;
    internal ForesightBucketCategory ForesightCategory;
    internal Il2CppSystem.Collections.Generic.List<ForesightCardData> ForesightOverrides;
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
    float nextMenuAttempt;
    bool menuHierarchyLogged;
    public PauseMenuSL(IntPtr pointer) : base(pointer) { }
    public void Update() {
        try {
            var display = ServiceLocator.GetSystem<S_PlayerValuesDisplay>();
            if (display != null && display._pauseMenu != null && display.PauseMenuIsOpen()) {
                Plugin.EnsureCheckpoint();
                if (button == null) {
                    if (Time.realtimeSinceStartup < nextMenuAttempt) return;
                    nextMenuAttempt = Time.realtimeSinceStartup + 5;
                    CreateButton(display._pauseMenu);
                }
                label.text = "重开 SL";
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
            if (reward != null && reward.Foresight) {
                ForesightChoiceEncounterUI target = null;
                foreach (var candidate in UnityEngine.Object.FindObjectsOfType<ForesightChoiceEncounterUI>(true)) {
                    if (candidate.name != reward.Name) continue;
                    if (target != null) throw new InvalidOperationException("Ambiguous foresight UI: " + reward.Name);
                    target = candidate;
                }
                if (target == null) throw new InvalidOperationException("Foresight UI not found: " + reward.Name);
                target._overrideRewards = reward.ForesightOverrides;
                target.OfferForesights(reward.ForesightCategory, reward.Count, reward.Reset);
            } else if (reward != null) {
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
        var originals = new System.Collections.Generic.List<Button>();
        foreach (var candidate in menu.GetComponentsInChildren<Button>(true)) {
            if (candidate.gameObject.activeSelf) originals.Add(candidate);
        }
        if (originals.Count < 2) {
            if (!menuHierarchyLogged) {
                menuHierarchyLogged = true;
                foreach (var t in menu.GetComponentsInChildren<Transform>(true)) {
                    string types = "";
                    foreach (var c in t.GetComponents<Component>()) if (c != null) types += c.GetIl2CppType().FullName + ";";
                    Plugin.Instance.Log.LogInfo("Pause UI: " + t.name + " [" + types + "]");
                }
            }
            throw new InvalidOperationException("Native Unity pause buttons not found; hierarchy logged");
        }
        originals.Sort((a,b) => b.transform.position.y.CompareTo(a.transform.position.y));
        var first = originals[0];
        var parent = first.transform.parent;
        originals.RemoveAll(b => b.transform.parent != parent);
        if (originals.Count < 2) throw new InvalidOperationException("Pause menu buttons do not share a container");
        var firstRect = first.GetComponent<RectTransform>();
        var lastRect = originals[originals.Count-1].GetComponent<RectTransform>();
        float top = firstRect.anchoredPosition.y, bottom = lastRect.anchoredPosition.y;
        float height = firstRect.rect.height;
        var group = parent.GetComponent<VerticalLayoutGroup>();
        if (group != null) group.enabled = false;
        button = UnityEngine.Object.Instantiate<Button>(first, parent);
        button.name = "QuickSLButton";
        // Replace the complete UnityEvent, including serialized persistent actions.
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener((UnityEngine.Events.UnityAction)(Action)Plugin.Retry);
        label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null) throw new InvalidOperationException("Cloned pause button has no TMP label");
        label.text = "重开 SL";
        button.transform.SetSiblingIndex(first.transform.GetSiblingIndex() + 1);
        originals.Insert(1, button);
        float step = (top - bottom) / (originals.Count - 1);
        for (int i = 0; i < originals.Count; i++) {
            var rect = originals[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(firstRect.anchoredPosition.x, top - i * step);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(height, Mathf.Abs(step) * 0.82f));
        }
        Plugin.Instance.Log.LogInfo("SL button inserted after Continue in pause-menu list.");
    }
}
}
