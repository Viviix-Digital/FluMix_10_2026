using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class FluMixWire
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) return "playing";
        var battleScene = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Battle.unity");
        var battle = Object.FindAnyObjectByType<FluMix.BattleController>();
        var guards = Object.FindObjectsByType<FluMix.Guardian>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var arr = new FluMix.Guardian[3];
        foreach (var g in guards)
        {
            int i = (int)g.Id;
            if (i >= 0 && i < 3) arr[i] = g;
        }
        battle.Guardians = arr;
        battle.VirusSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/virus.png");
        battle.RingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/ring.png");
        EditorSceneManager.MarkSceneDirty(battleScene);
        EditorSceneManager.SaveScene(battleScene);

        var how = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/HowToPlay.unity");
        int texts = 0;
        foreach (var tmp in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tmp.gameObject.name == "H") tmp.color = new Color(0.05f, 0.22f, 0.55f);
            if (tmp.gameObject.name == "B" || tmp.gameObject.name == "Vn") tmp.color = new Color(0.08f, 0.12f, 0.22f);
            tmp.overflowMode = TextOverflowModes.Overflow;
            texts++;
        }
        foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img.gameObject.name.StartsWith("C") && img.gameObject.name.Length == 2)
                img.color = new Color(1f, 1f, 1f, 0.94f);
        }
        EditorSceneManager.MarkSceneDirty(how);
        EditorSceneManager.SaveScene(how);
        return "guards " + (arr[0] != null) + (arr[1] != null) + (arr[2] != null) + " n=" + guards.Length + " texts=" + texts + " virus=" + (battle.VirusSprite != null);
    }
}
