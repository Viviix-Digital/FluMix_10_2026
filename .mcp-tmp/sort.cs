using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class FluMixSort
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) return "playing";
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var battle = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Battle.unity");
        int canvases = 0;
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 50;
                canvases++;
            }
        }
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sr.sortingOrder > 10) sr.sortingOrder = 2;
        }
        EditorSceneManager.MarkSceneDirty(battle);
        EditorSceneManager.SaveScene(battle);

        var lobby = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Lobby.unity");
        int n = 0;
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var main = ps.main;
            main.startSize = 1.1f;
            main.startSpeed = 6f;
            main.startColor = new Color(0.55f, 0.88f, 1f, 1f);
            var emission = ps.emission;
            emission.rateOverTime = 180f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", new Color(0.55f, 0.88f, 1f, 1f));
                renderer.material = mat;
            }
            renderer.sortingOrder = 5;
            n++;
        }
        EditorSceneManager.MarkSceneDirty(lobby);
        EditorSceneManager.SaveScene(lobby);

        var board = EditorSceneManager.OpenScene("Assets/FluMix/Scenes/Leaderboard.unity");
        var panel = GameObject.Find("Panel");
        if (panel != null)
        {
            var img = panel.GetComponent<Image>();
            if (img != null) img.color = new Color(0.96f, 0.98f, 1f, 0.98f);
        }
        EditorSceneManager.MarkSceneDirty(board);
        EditorSceneManager.SaveScene(board);
        return "shader=" + (shader != null) + " canvases=" + canvases + " particles=" + n + " panel=" + (panel != null);
    }
}
