const { spawnSync } = require("child_process");
const fs = require("fs");
const root = "D:/Unity/FluMix_10_2026";
const tmp = root + "/.mcp-tmp";
function pause(ms) { Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms); }
function sleep(ms) { return new Promise((r) => setTimeout(r, ms)); }
function cli(tool, inputFile, timeout = 90000) {
  let last = "";
  for (let a = 0; a < 6; a++) {
    const r = spawnSync("npx", ["--yes", "unity-mcp-cli", "run-tool", tool, "--path", root, "--input-file", inputFile, "--timeout", String(timeout)], { encoding: "utf8", maxBuffer: 40 * 1024 * 1024, shell: true, cwd: root });
    last = (r.stdout || "") + (r.stderr || "");
    if (r.status === 0 && (r.stdout || "").includes("{")) return r.stdout || "";
    pause(1500);
  }
  throw new Error(last.slice(0, 600));
}
function parse(s) { return JSON.parse(s.slice(s.indexOf("{"))); }
function exec(code, name) {
  fs.writeFileSync(tmp + "/cap-exec.json", JSON.stringify({ csharpCode: code, className: name, methodName: "Main" }));
  return parse(cli("script-execute", tmp + "/cap-exec.json")).structured.result.value;
}
function shot(name) {
  const j = parse(cli("screenshot-game-view", tmp + "/shot-input.json"));
  fs.writeFileSync(root + "/Assets/FluMix/Screenshots/" + name + ".png", Buffer.from(j.content.find((c) => c.type === "image").data, "base64"));
  console.log("SHOT", name, (j.content.find((c) => c.type === "text") || {}).text);
}
const repaint = "var asm=typeof(UnityEditor.EditorWindow).Assembly; var t=asm.GetType(\"UnityEditor.GameView\"); var w=UnityEditor.EditorWindow.GetWindow(t); w.Repaint(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews();";
async function main() {
  console.log(exec("using UnityEditor; using UnityEditor.SceneManagement; public class OpenB { public static string Main(){ if(EditorApplication.isPlaying) EditorApplication.isPlaying=false; return EditorSceneManager.OpenScene(\"Assets/FluMix/Scenes/Battle.unity\").name; }}", "OpenB"));
  pause(2500);
  cli("editor-application-set-state", tmp + "/play.json");
  pause(3000);
  const code = "using UnityEngine; using System.Reflection; public class ShowQ { public static string Main(){\n" +
    " var b=UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>();\n" +
    " b.OpenQuiz(false);\n" +
    " var panel=b.Quiz.transform.Find(\"Panel\") as RectTransform;\n" +
    " Canvas.ForceUpdateCanvases();\n" +
    repaint + "\n" +
    " return \"active=\"+b.Quiz.gameObject.activeInHierarchy+\" scale=\"+panel.localScale+\" text=\"+panel.Find(\"Prompt\").GetComponent<TMPro.TMP_Text>().text;\n}}";
  console.log("Q", exec(code, "ShowQ"));
  await sleep(400);
  shot("Quiz");
  const code2 = "using UnityEngine; using System.Reflection; public class ShowE { public static string Main(){\n" +
    " var b=UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>();\n" +
    " b.ResolveQuiz(1);\n" +
    " var ex=b.Quiz.transform.Find(\"Explain\");\n" +
    " var panel=b.Quiz.transform.Find(\"Panel\");\n" +
    " if(panel!=null) panel.gameObject.SetActive(false);\n" +
    " ex.gameObject.SetActive(true);\n" +
    " ex.Find(\"Headline\").GetComponent<TMPro.TMP_Text>().text=FluMix.FluMixFlow.Instance.Strings.Get(\"explain.correct\");\n" +
    " ex.Find(\"Affirm\").GetComponent<TMPro.TMP_Text>().text=FluMix.FluMixFlow.Instance.Questions.Get(0).affirmation;\n" +
    " ex.Find(\"Body\").GetComponent<TMPro.TMP_Text>().text=FluMix.FluMixFlow.Instance.Questions.Get(0).body;\n" +
    " ex.Find(\"Cite\").GetComponent<TMPro.TMP_Text>().text=FluMix.FluMixFlow.Instance.Questions.Get(0).citation;\n" +
    " if(b.Hud!=null) b.Hud.Refresh();\n" +
    " Canvas.ForceUpdateCanvases();\n" +
    repaint + "\n" +
    " return \"score=\"+FluMix.GameSession.Score+\" head=\"+ex.Find(\"Headline\").GetComponent<TMPro.TMP_Text>().text;\n}}";
  console.log("E", exec(code2, "ShowE"));
  await sleep(400);
  shot("Explain");
  cli("editor-application-set-state", tmp + "/stop.json");
  console.log("DONE");
}
main().catch((e) => { console.error(e.stack || e.message); process.exit(1); });
