using System;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace ShroomQuickSL {
[BepInPlugin("io.github.shroomquicksl", "Shroom Quick SL", "0.1.0")]
public class Plugin : BasePlugin {
    internal static Plugin Instance;
    internal static RunSaveData Snapshot;
    internal static MetaSaveData Meta;
    internal static CombatRoom Room;
    public override void Load() {
        Instance = this;
        new Harmony("io.github.shroomquicksl").PatchAll();
        AddComponent<Hotkeys>();
        Log.LogInfo("Quick SL loaded. F8: retry active combat (experimental).");
    }
    [HarmonyPatch(typeof(CombatRoom), "OnEncounterTriggered")]
    class Capture {
        static void Prefix(CombatRoom __instance) {
            Snapshot = null; Meta = null; Room = null;
            try {
                var save = ServiceLocator.GetSystem<S_SaveManager>();
                if (save == null || !save.HasRunInMemory()) return;
                Snapshot = save.GetRunReadOnlyInMemory().DeepCopy();
                Meta = save.GetMetaReadOnlyInMemory().DeepCopy();
                Room = __instance;
                Instance.Log.LogInfo("Combat retry snapshot captured.");
            } catch (Exception e) { Instance.Log.LogError(e); }
        }
    }
}
public class Hotkeys : MonoBehaviour {
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("kernel32.dll")] static extern uint GetCurrentProcessId();
    bool held;
    float nextAllowed;
    public Hotkeys(IntPtr pointer) : base(pointer) { }
    public void Update() {
        bool down = (GetAsyncKeyState(0x77) & 0x8000) != 0;
        bool pressed = down && !held; held = down;
        if (!pressed || Time.realtimeSinceStartup < nextAllowed) return;
        uint pid; GetWindowThreadProcessId(GetForegroundWindow(), out pid);
        if (pid != GetCurrentProcessId()) return;
        try {
            var boot = ServiceLocator.GetSystem<S_BootManager>();
            var save = ServiceLocator.GetSystem<S_SaveManager>();
            if (boot == null || save == null || boot.BusyLoading || !boot.IsMainGameLoop()) return;
            if (Plugin.Room == null || Plugin.Snapshot == null || Plugin.Meta == null ||
                Plugin.Room.CurrentCombatState != CombatRoom.CombatState.CombatActive) return;
            var run = Plugin.Snapshot.DeepCopy();
            var meta = Plugin.Meta.DeepCopy();
            nextAllowed = Time.realtimeSinceStartup + 5;
            save._inMemoryRun = run.DeepCopy();
            save._inMemoryMeta = meta;
            boot.ContinueGame = true;
            boot.SaveDataToLoad = run;
            boot.ReloadGameScene();
            Plugin.Room = null;
            Plugin.Instance.Log.LogInfo("Combat retry requested.");
        } catch (Exception e) { Plugin.Instance.Log.LogError(e); }
    }
}
}
