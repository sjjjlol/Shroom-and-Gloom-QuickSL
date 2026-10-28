using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShroomQuickSL {
[BepInPlugin("io.github.shroomquicksl", "Shroom Quick SL", "0.4.0")]
public class Plugin : BasePlugin {
    internal static Plugin Instance;
    internal static Snapshot Current, Restoring;
    internal static float Deadline;
    internal static bool Failed;
    public override void Load() {
        Instance=this;
        new Harmony("io.github.shroomquicksl").PatchAll();
        AddComponent<PauseMenuSL>();
        Log.LogInfo("Quick SL 0.4.0: native checkpoints only; experimental encounter snapshots removed.");
    }
    internal static V_Player Player() { return UnityEngine.Object.FindObjectOfType<V_Player>(); }
    internal static void Capture(S_SaveManager save, RunSaveData run, string origin) {
        if (Restoring != null || run == null || run.SaveInvalidatedDueToError || run.RunComplete || run.PlayerState == null) return;
        var copy=run.DeepCopy();
        Current=new Snapshot { Run=copy, Meta=save.GetMetaReadOnlyInMemory().DeepCopy() };
        Instance.Log.LogInfo("Checkpoint captured ["+origin+"]: path="+copy.CurrentTunnelPath+", checkpoint="+copy.LastCheckpoint+", health="+copy.PlayerState.HealthData.CurrentHealth+"/"+copy.PlayerState.HealthData.MaxHealth);
    }
    [HarmonyPatch(typeof(S_SaveManager), "SnapshotAllInMemoryForSaveToStorage")]
    class CheckpointHook {
        static void Postfix(S_SaveManager __instance) {
            try { Capture(__instance,__instance.GetRunReadOnlyInMemory(),"game checkpoint"); }
            catch(Exception e) { Instance.Log.LogError(e); }
        }
    }
    // Capture the data handed to the native continuation flow, before gameplay mutates it.
    [HarmonyPatch(typeof(S_BootManager), "LoadScene")]
    class LoadHook {
        static void Prefix(S_BootManager __instance) {
            if(Restoring!=null) return;
            Current=null; Failed=false;
            try {
                if(__instance.ContinueGame && __instance.SaveDataToLoad!=null)
                    Capture(ServiceLocator.GetSystem<S_SaveManager>(),__instance.SaveDataToLoad,"continue game");
            } catch(Exception e) { Instance.Log.LogError(e); }
        }
    }
    internal static void Retry() {
        try {
            var boot=ServiceLocator.GetSystem<S_BootManager>();
            var save=ServiceLocator.GetSystem<S_SaveManager>();
            var p=Player();
            if(Failed || Current==null || Restoring!=null || boot==null || boot.BusyLoading || p==null || p.BusySavingInCheckpoint || save.HasUnsavedSnapshotPending()) return;
            if(!boot.IsMainGameLoop() || save.GetRunReadOnlyInMemory().RunStatsId!=Current.Run.RunStatsId) return;
            var target=Current.Run.GetLastSavedTunnelSceneReference();
            if(target==null) throw new InvalidOperationException("Checkpoint scene is unavailable");
            ServiceLocator.GetSystem<S_PlayerValuesDisplay>().HidePauseMenu();
            Restoring=Current; Restoring.LoadObserved=false;
            Deadline=Time.realtimeSinceStartup+90;
            save._inMemoryRun=Current.Run.DeepCopy();
            save._inMemoryMeta=Current.Meta.DeepCopy();
            boot.FirstTunnelSceneToLoad=target;
            boot.ContinueGame=true;
            boot.SaveDataToLoad=Current.Run.DeepCopy();
            boot.ReloadGameScene();
            Instance.Log.LogInfo("Native SL requested");
        } catch(Exception e) { Failed=true; Restoring=null; Instance.Log.LogError(e); }
    }
    [HarmonyPatch(typeof(RunSaveData), "GeneratePlayerClassFromSave")]
    class PlayerLoadAudit {
        static void Postfix(RunSaveData __instance, RuntimePlayerClass __result) {
            if(Restoring==null) return;
            Instance.Log.LogInfo("Native player load: source health="+__instance.PlayerState.HealthData.CurrentHealth+", generated health="+__result.PlayerState.HealthData.CurrentHealth);
            Restoring.LoadObserved=true;
        }
    }
}
internal class Snapshot { internal RunSaveData Run; internal MetaSaveData Meta; internal bool LoadObserved; }
public class PauseMenuSL : MonoBehaviour {
    Button button; TextMeshProUGUI label;
    float nextMenuAttempt, readyAt;
    bool menuHierarchyLogged;
    public PauseMenuSL(IntPtr pointer):base(pointer) {}
    public void Update() {
        try {
            var display=ServiceLocator.GetSystem<S_PlayerValuesDisplay>();
            if(display!=null && display._pauseMenu!=null && display.PauseMenuIsOpen()) {
                if(button==null && Time.realtimeSinceStartup>=nextMenuAttempt) {
                    nextMenuAttempt=Time.realtimeSinceStartup+5; CreateButton(display._pauseMenu);
                }
                if(button!=null) {
                    label.text=Plugin.Failed ? "SL 失败" : "重开 SL";
                    button.interactable=Plugin.Current!=null && Plugin.Restoring==null && !Plugin.Failed;
                }
            }
            if(Plugin.Restoring==null) { readyAt=0; return; }
            if(Time.realtimeSinceStartup>Plugin.Deadline) throw new InvalidOperationException("Native restore timed out; no success assumed");
            var boot=ServiceLocator.GetSystem<S_BootManager>();
            var p=Plugin.Player();
            if(!Plugin.Restoring.LoadObserved || boot==null || boot.BusyLoading || p==null || p.Model==null || p.BusySavingInCheckpoint) {readyAt=0;return;}
            if(readyAt==0) {readyAt=Time.realtimeSinceStartup+1;return;}
            if(Time.realtimeSinceStartup<readyAt)return;
            var expected=Plugin.Restoring.Run.PlayerState.HealthData;
            var actual=p.Model.GetSaveState().HealthData;
            if(expected.CurrentHealth!=actual.CurrentHealth || expected.MaxHealth!=actual.MaxHealth)
                throw new InvalidOperationException("Restored health mismatch: expected "+expected.CurrentHealth+"/"+expected.MaxHealth+", actual "+actual.CurrentHealth+"/"+actual.MaxHealth);
            Plugin.Instance.Log.LogInfo("Native scene/player initialization and health verified; gameplay flow still needs validation");
            Plugin.Restoring=null; readyAt=0;
        } catch(Exception e) {Plugin.Failed=true;Plugin.Restoring=null;Plugin.Instance.Log.LogError(e);}
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
