using UnityEditor;
using UnityEngine;

public class FluMixProbe
{
    public static string Main()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        int missing = 0;
        int guards = 0;
        int quiz = 0;
        int lobby = 0;
        int battle = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (go.GetComponent<FluMix.Guardian>() != null) guards++;
            if (go.GetComponent<FluMix.QuizPresenter>() != null) quiz++;
            if (go.GetComponent<FluMix.LobbyController>() != null) lobby++;
            if (go.GetComponent<FluMix.BattleController>() != null) battle++;
        }
        var b = Object.FindAnyObjectByType<FluMix.BattleController>();
        string gref = "no-battle";
        if (b != null)
        {
            gref = b.Guardians == null ? "arr-null" : ("len=" + b.Guardians.Length);
            if (b.Guardians != null)
                foreach (var g in b.Guardians) gref += g == null ? " x" : " " + g.Id;
            gref += " quizRef=" + (b.Quiz != null);
        }
        return "scene=" + scene + " playing=" + Application.isPlaying + " missing=" + missing + " guards=" + guards + " quiz=" + quiz + " lobby=" + lobby + " battle=" + battle + " " + gref;
    }
}
