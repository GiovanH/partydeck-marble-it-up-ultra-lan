using UnityEngine;

namespace MIUULan;

// Corner label so identical windows can be told apart and command results stay visible.
// Only created in DEBUG_OVERLAY builds.
public class Overlay : MonoBehaviour
{
    GUIStyle style;

    public static void Create()
    {
        var go = new GameObject("MIUULan Overlay");
        DontDestroyOnLoad(go);
        go.AddComponent<Overlay>();
    }

    void OnGUI()
    {
        if (!Lan.Enabled)
            return;
        style ??= new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        string text = $"P{Lan.Id + 1}  port {Lan.LocalPort}  {Lan.Status}";
        GUI.color = Color.black;
        GUI.Label(new Rect(11, 11, Screen.width, 30), text, style);
        GUI.color = Color.yellow;
        GUI.Label(new Rect(10, 10, Screen.width, 30), text, style);
    }
}
