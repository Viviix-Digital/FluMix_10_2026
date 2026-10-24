#if UNITY_EDITOR
using System.IO;
using FluMix.Api;
using UnityEditor;
using UnityEngine;

namespace FluMix.EditorTools
{
    public static class FluMistApiSmokeMenu
    {
        [MenuItem("FluMix/Run API Smoke Test")]
        public static void RunSmoke()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("FluMist Smoke", "Enter Play Mode first, then run this menu again.", "OK");
                return;
            }

            var go = new GameObject("FluMistSmokeRunner");
            var api = go.AddComponent<FluMistApi>();
            var smoke = go.AddComponent<FluMistApiSmokeTest>();
            var field = typeof(FluMistApiSmokeTest).GetField("api", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(smoke, api);
            smoke.RunFromMenu();
            Debug.Log("[FluMist] Smoke started — watch Console for [FluMistSmoke] PASS/FAIL lines.");
        }

        [MenuItem("FluMix/Dump Smoke Report Path")]
        public static void DumpPath()
        {
            string path = Path.Combine(Application.dataPath, "../Logs/flumist-api-smoke.txt");
            Debug.Log("Expected PowerShell smoke log: " + Path.GetFullPath(path));
        }
    }
}
#endif
