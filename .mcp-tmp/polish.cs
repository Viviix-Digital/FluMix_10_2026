using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class FluMixPolish
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) return "playing";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/FluMix/Art/FluMixFont.asset");
        var board = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Leaderboard.unity");
        var title = GameObject.Find("Title");
        int removed = 0;
        if (title != null)
        {
            for (int i = title.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(title.transform.GetChild(i).gameObject);
                removed++;
            }
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(title.transform, false);
            var rt = labelGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.text = "BẢNG XẾP HẠNG";
            text.font = font;
            text.fontSize = 42f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.05f, 0.28f, 0.72f);
            var box = title.GetComponent<RectTransform>();
            box.sizeDelta = new Vector2(1100f, 90f);
        }
        EditorSceneManager.MarkSceneDirty(board);
        EditorSceneManager.SaveScene(board);

        var lobby = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Lobby.unity");
        int jets = 0;
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var main = ps.main;
            main.startSize = 0.55f;
            main.startSpeed = 7f;
            main.startColor = new Color(0.75f, 0.92f, 1f, 0.95f);
            var emission = ps.emission;
            emission.rateOverTime = 120f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) renderer.material = new Material(shader);
            renderer.sortingOrder = 20;
            jets++;
        }
        EditorSceneManager.MarkSceneDirty(lobby);
        EditorSceneManager.SaveScene(lobby);
        return "title=" + (title != null) + " removed=" + removed + " particles=" + jets;
    }
}
