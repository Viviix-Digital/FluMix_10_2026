using UnityEditor;
using UnityEngine;

public class FluMixScriptDiag
{
    public static string Main()
    {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/FluMix/Scripts/FluMixBattle.cs");
        var cls = script != null ? script.GetClass() : null;
        var root = PrefabUtility.LoadPrefabContents("Assets/FluMix/Prefabs/GuardianG.prefab");
        var existing = root.GetComponent<FluMix.Guardian>();
        var added = root.AddComponent<FluMix.Guardian>();
        var so = new SerializedObject(added);
        var prop = so.FindProperty("m_Script");
        string before = prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "null";
        prop.objectReferenceValue = script;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/FluMix/Prefabs/GuardianG.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        var again = PrefabUtility.LoadPrefabContents("Assets/FluMix/Prefabs/GuardianG.prefab");
        var g = again.GetComponent<FluMix.Guardian>();
        string after = g != null ? g.GetType().FullName : "missing";
        PrefabUtility.UnloadPrefabContents(again);
        return "script=" + (script != null) + " class=" + (cls != null ? cls.FullName : "null") + " existing=" + (existing != null) + " before=" + before + " after=" + after;
    }
}
