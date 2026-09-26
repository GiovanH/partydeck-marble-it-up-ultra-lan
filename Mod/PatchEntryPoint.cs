using System.Linq;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace MIUULan;

public static class PatchEntryPoint
{
    public static void Start()
    {
        Lan.ParseArgs();
        SceneManager.sceneLoaded += BeginPatch;
    }

    static void BeginPatch(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= BeginPatch;
        var harmony = new Harmony("com.giovanh.miuulan");
        harmony.PatchAll();
        Lan.ApplyLanSettings();
#if DEBUG_OVERLAY
        Overlay.Create();
#endif
        Lan.Log($"loaded (lan={Lan.Enabled}, id={Lan.Id}, {harmony.GetPatchedMethods().Count()} methods patched)");
    }
}
