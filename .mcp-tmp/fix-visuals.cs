using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FluMixFixVisuals
{
    public static string Main()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        string[] names = { "soft", "ring", "pill", "frame", "ribbon", "hero", "castle", "virus", "login-bg", "hall-bg", "battle-bg" };
        foreach (var n in names)
        {
            string path = "Assets/FluMix/Art/" + n + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 100f;
            if (n == "pill") importer.spriteBorder = new Vector4(32, 32, 32, 32);
            if (n == "frame") importer.spriteBorder = new Vector4(40, 40, 40, 40);
            importer.SaveAndReimport();
        }

        Sprite soft = Load("soft");
        Sprite ring = Load("ring");
        Sprite pill = Load("pill");
        Sprite frame = Load("frame");
        Sprite ribbon = Load("ribbon");
        Sprite hero = Load("hero");
        Sprite castle = Load("castle");
        Sprite virus = Load("virus");
        Sprite login = Load("login-bg");
        Sprite hall = Load("hall-bg");
        Sprite battle = Load("battle-bg");

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/FluMix/Art/FluMixFont.asset");
        var sb = new StringBuilder();
        for (int i = 32; i < 127; i++) sb.Append((char)i);
        sb.Append("ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯưẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỴỵỶỷỸỹ⌀⌂");
        string missing;
        bool added = font.TryAddCharacters(sb.ToString(), out missing);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();

        int images = 0;
        string[] scenes = {
            "Assets/FluMix/Scenes/Login.unity",
            "Assets/FluMix/Scenes/HowToPlay.unity",
            "Assets/FluMix/Scenes/Lobby.unity",
            "Assets/FluMix/Scenes/Battle.unity",
            "Assets/FluMix/Scenes/Leaderboard.unity",
            "Assets/FluMix/Scenes/Boot.unity"
        };
        foreach (var scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            images += AssignScene(scene.name, soft, ring, pill, frame, ribbon, hero, castle, virus, login, hall, battle, font);
            EditorSceneManager.SaveScene(scene);
        }

        int prefabs = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/FluMix/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                prefabs += AssignObject(t.gameObject, path, soft, ring, pill, frame, ribbon, hero, castle, virus, login, hall, battle, font);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        return "added=" + added + " missing=" + (missing ?? "") + " images=" + images + " prefabs=" + prefabs
            + " login=" + (login != null) + " fontChars=" + font.characterTable.Count;
    }

    static Sprite Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FluMix/Art/" + name + ".png");
    }

    static int AssignScene(string scene, Sprite soft, Sprite ring, Sprite pill, Sprite frame, Sprite ribbon, Sprite hero, Sprite castle, Sprite virus, Sprite login, Sprite hall, Sprite battle, TMP_FontAsset font)
    {
        int n = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            n += AssignObject(go, scene, soft, ring, pill, frame, ribbon, hero, castle, virus, login, hall, battle, font);
        return n;
    }

    static int AssignObject(GameObject go, string scene, Sprite soft, Sprite ring, Sprite pill, Sprite frame, Sprite ribbon, Sprite hero, Sprite castle, Sprite virus, Sprite login, Sprite hall, Sprite battle, TMP_FontAsset font)
    {
        int n = 0;
        var image = go.GetComponent<Image>();
        if (image != null)
        {
            Sprite pick = PickUi(go.name, scene, soft, ring, pill, frame, ribbon, hero, castle, virus, login, hall);
            if (pick != null && image.sprite != pick)
            {
                image.sprite = pick;
                if (pick == pill || pick == frame) image.type = Image.Type.Sliced;
                if (pick == hero || pick == virus || pick == ribbon) image.preserveAspect = true;
                n++;
            }
        }
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            if (go.name == "Landscape") sr.sprite = battle;
            else if (go.name == "Castle") sr.sprite = castle;
            else if (scene.Contains("Virus")) sr.sprite = virus;
            else sr.sprite = hero;
            n++;
        }
        if (go.name == "g")
        {
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null) tmp.color = new Color(0.05f, 0.28f, 0.72f, 1f);
        }
        if (go.name == "Placeholder")
        {
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null) tmp.color = new Color(0.35f, 0.45f, 0.55f, 1f);
        }
        var any = go.GetComponent<TMP_Text>();
        if (any != null && font != null && any.font != font)
        {
            any.font = font;
            n++;
        }
        return n;
    }

    static Sprite PickUi(string name, string scene, Sprite soft, Sprite ring, Sprite pill, Sprite frame, Sprite ribbon, Sprite hero, Sprite castle, Sprite virus, Sprite login, Sprite hall)
    {
        switch (name)
        {
            case "Background": return scene == "Login" ? login : hall;
            case "Character": return hero;
            case "Card":
            case "Panel": return frame;
            case "Ribbon": return ribbon;
            case "Heart":
            case "Glow": return soft;
            case "Ring": return ring;
            case "Account":
            case "Password":
            case "Submit":
            case "Eye":
            case "Ok":
            case "Home":
            case "Close":
            case "HudCard": return pill;
            case "Dot": return virus;
            default: return null;
        }
    }
}
