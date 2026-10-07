using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class FluMixVisible
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) return "playing";
        var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/FluMix/Art/pill.png");
        importer.spriteBorder = new Vector4(24, 24, 24, 24);
        importer.SaveAndReimport();

        var battle = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Battle.unity");
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
        }
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sr.sortingOrder = -5;
        EditorSceneManager.MarkSceneDirty(battle);
        EditorSceneManager.SaveScene(battle);

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FluMix/Art/soft.png");
        var lobby = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Lobby.unity");
        int n = 0;
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var main = ps.main;
            main.startSize = 0.9f;
            main.startSpeed = 5f;
            main.startColor = Color.white;
            var emission = ps.emission;
            emission.rateOverTime = 160f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (shader != null)
            {
                var mat = new Material(shader);
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTexture("_MainTex", tex);
                }
                mat.SetColor("_BaseColor", new Color(0.45f, 0.85f, 1f, 1f));
                renderer.material = mat;
            }
            renderer.sortingOrder = 8;
            n++;
        }
        EditorSceneManager.MarkSceneDirty(lobby);
        EditorSceneManager.SaveScene(lobby);

        var pill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/pill.png");
        var board = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Leaderboard.unity");
        foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img.gameObject.name == "Panel" || img.gameObject.name.StartsWith("Slot") || img.gameObject.name == "Card")
            {
                img.sprite = pill;
                img.type = Image.Type.Sliced;
                if (img.color.a < 0.9f) img.color = new Color(img.color.r, img.color.g, img.color.b, 0.98f);
            }
        }
        var panel = GameObject.Find("Panel");
        if (panel != null)
        {
            var image = panel.GetComponent<Image>();
            image.sprite = pill;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.97f, 0.99f, 1f, 0.98f);
        }
        EditorSceneManager.MarkSceneDirty(board);
        EditorSceneManager.SaveScene(board);
        return "particles=" + n + " pill=" + (pill != null) + " panel=" + (panel != null);
    }
}
