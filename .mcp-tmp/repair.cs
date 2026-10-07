using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class FluMixRepair
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) return "playing";
        var hero = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/hero.png");
        var virus = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/virus.png");
        var ring = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/ring.png");
        int fixedPrefabs = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/FluMix/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            int missing = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
            if (path.Contains("Guardian") && root.GetComponent<FluMix.Guardian>() == null)
            {
                var g = root.AddComponent<FluMix.Guardian>();
                g.Id = path.Contains("GuardianA") ? FluMix.GuardianId.A : path.Contains("GuardianT") ? FluMix.GuardianId.T : FluMix.GuardianId.G;
            }
            if (path.Contains("Virus") && root.GetComponent<FluMix.Virus>() == null)
                root.AddComponent<FluMix.Virus>();
            if (path.Contains("Beam") && root.GetComponent<FluMix.Beam>() == null)
                root.AddComponent<FluMix.Beam>();
            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
                sr.sprite = path.Contains("Virus") ? virus : hero;
            if (missing > 0) fixedPrefabs++;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        var scene = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Battle.unity");
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name == "GuardianG" || go.name == "GuardianA" || go.name == "GuardianT")
                Object.DestroyImmediate(go);
        }
        FluMix.Guardian Place(string prefab, FluMix.GuardianId id, Vector3 pos)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FluMix/Prefabs/" + prefab + ".prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.55f;
            return go.GetComponent<FluMix.Guardian>();
        }
        var g0 = Place("GuardianG", FluMix.GuardianId.G, new Vector3(-6.4f, 1.15f, 0f));
        var a0 = Place("GuardianA", FluMix.GuardianId.A, new Vector3(-6.5f, -0.15f, 0f));
        var t0 = Place("GuardianT", FluMix.GuardianId.T, new Vector3(-6.3f, -1.45f, 0f));
        var battle = Object.FindAnyObjectByType<FluMix.BattleController>();
        battle.Guardians = new[] { g0, a0, t0 };
        battle.VirusSprite = virus;
        battle.RingSprite = ring;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "fixedPrefabs=" + fixedPrefabs + " G=" + (g0 != null) + " A=" + (a0 != null) + " T=" + (t0 != null);
    }
}
