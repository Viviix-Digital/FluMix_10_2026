using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class FluMixAddGuardian
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) return "playing";
        var scene = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Battle.unity");
        var hero = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/hero.png");
        var virus = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/virus.png");
        var ring = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/ring.png");
        FluMix.Guardian Make(string name, FluMix.GuardianId id, Vector3 pos, Color color)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = hero;
            sr.color = color;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.55f;
            var g = go.AddComponent<FluMix.Guardian>();
            g.Id = id;
            return g;
        }
        var old = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in old)
        {
            if (go.name.StartsWith("Guardian")) Object.DestroyImmediate(go);
        }
        var g = Make("GuardianG", FluMix.GuardianId.G, new Vector3(-6.4f, 1.15f, 0f), Color.white);
        var a = Make("GuardianA", FluMix.GuardianId.A, new Vector3(-6.5f, -0.15f, 0f), Color.white);
        var t = Make("GuardianT", FluMix.GuardianId.T, new Vector3(-6.3f, -1.45f, 0f), Color.white);
        var battle = Object.FindAnyObjectByType<FluMix.BattleController>();
        battle.Guardians = new[] { g, a, t };
        battle.VirusSprite = virus;
        battle.RingSprite = ring;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        var found = Object.FindObjectsByType<FluMix.Guardian>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return "made=" + (g != null) + " found=" + found.Length + " id0=" + (found.Length > 0 ? found[0].Id.ToString() : "-");
    }
}
