using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class FluMixSolidCards
{
    public static string Main()
    {
        var pill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/pill.png");
        string[] scenes = {
            "Assets/FluMix/Scenes/Login.unity",
            "Assets/FluMix/Scenes/HowToPlay.unity",
            "Assets/FluMix/Scenes/Lobby.unity",
            "Assets/FluMix/Scenes/Battle.unity",
            "Assets/FluMix/Scenes/Leaderboard.unity"
        };
        int n = 0;
        foreach (var path in scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string name = img.gameObject.name;
                if (name == "Card" || name == "Panel" || name == "Result" || name == "Explain")
                {
                    img.sprite = pill;
                    img.type = Image.Type.Sliced;
                    img.color = new Color(0.93f, 0.98f, 1f, 0.96f);
                    n++;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        return "cards=" + n + " pill=" + (pill != null);
    }
}
