const { spawnSync } = require("child_process");
const fs = require("fs");
const root = "D:/Unity/FluMix_10_2026";
const tmp = root + "/.mcp-tmp";
function pause(ms) { Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms); }
function sleep(ms) { return new Promise((r) => setTimeout(r, ms)); }
function cli(tool, inputFile, timeout = 90000) {
  let last = "";
  for (let attempt = 0; attempt < 8; attempt++) {
    const r = spawnSync("npx", ["--yes", "unity-mcp-cli", "run-tool", tool, "--path", root, "--input-file", inputFile, "--timeout", String(timeout)], { encoding: "utf8", maxBuffer: 40 * 1024 * 1024, shell: true, cwd: root });
    last = (r.stdout || "") + (r.stderr || "");
    if (r.status === 0 && (r.stdout || "").includes("{")) return r.stdout || "";
    console.log("RETRY", tool, attempt);
    pause(2000);
  }
  throw new Error(last.slice(0, 900));
}
function parse(stdout) { return JSON.parse(stdout.slice(stdout.indexOf("{"))); }
function exec(code, className) {
  fs.writeFileSync(tmp + "/cap-exec.json", JSON.stringify({ csharpCode: code, className, methodName: "Main" }));
  return parse(cli("script-execute", tmp + "/cap-exec.json", 120000)).structured.result.value;
}
function state() { return parse(cli("editor-application-get-state", tmp + "/get-state.json", 30000)).structured.result; }
async function until(pred, label) {
  for (let i = 0; i < 25; i++) { if (pred(state())) return; await sleep(400); }
  throw new Error("timeout " + label);
}
function shot(name) {
  const j = parse(cli("screenshot-game-view", tmp + "/shot-input.json", 60000));
  const img = (j.content || []).find((c) => c.type === "image");
  fs.writeFileSync(root + "/Assets/FluMix/Screenshots/" + name + ".png", Buffer.from(img.data, "base64"));
  console.log("SHOT", name);
}
async function stop() {
  cli("editor-application-set-state", tmp + "/stop.json", 60000);
  await until((s) => !s.IsPlaying && !s.IsPlayingOrWillChangePlaymode, "stop");
}
async function play(scene) {
  if (state().IsPlaying || state().IsPlayingOrWillChangePlaymode) await stop();
  console.log("OPEN", exec("using UnityEditor.SceneManagement; public class OpenOnly { public static string Main(){ return EditorSceneManager.OpenScene(\"Assets/FluMix/Scenes/" + scene + ".unity\").name; } }", "OpenOnly"));
  cli("editor-application-set-state", tmp + "/play.json", 60000);
  await sleep(2000);
  await until((s) => s.IsPlaying, scene);
  await sleep(800);
}
async function main() {
  await play("Battle");
  const combat = "using UnityEngine; public class Combat2 { public static string Main(){\n" +
    " var b=UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>();\n" +
    " if (b.QuizOpen) b.CloseQuiz();\n" +
    " b.SelectGuardian(0);\n" +
    " var hit=b.Spawn(FluMix.FluMixFlow.Instance.ArchetypeFor(FluMix.GuardianId.G), true);\n" +
    " hit.transform.position=new Vector3(-1.2f,0f,0f);\n" +
    " foreach (var a in b.Alive) if (a!=null && a!=hit) a.transform.position=new Vector3(8f,1f,0f);\n" +
    " int s0=FluMix.GameSession.Score; b.ConfirmShot(); int s1=FluMix.GameSession.Score;\n" +
    " var miss=b.Spawn(FluMix.FluMixFlow.Instance.ArchetypeFor(FluMix.GuardianId.A), true);\n" +
    " miss.transform.position=new Vector3(-1f,0.5f,0f);\n" +
    " int t0=miss.Tier; b.ConfirmShot();\n" +
    " return \"hit=\"+(s1-s0)+\" miss=\"+(FluMix.GameSession.Score-s1)+\" tier=\"+t0+\"->\"+miss.Tier+\" score=\"+FluMix.GameSession.Score+\" cue=\"+FluMix.GameSession.LastCue+\" :: \"+string.Join(\" | \", FluMix.FluMixLog.Tail);\n}}";
  console.log("COMBAT", exec(combat, "Combat2"));
  console.log("QUIZ", exec("using UnityEngine; public class Q2 { public static string Main(){ var b=UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>(); b.OpenQuiz(false); var q=b.Quiz; return \"open=\"+b.QuizOpen+\" active=\"+(q!=null && q.gameObject.activeInHierarchy)+\" prompt=\"+(q!=null? q.transform.Find(\"Panel/Prompt\").GetComponent<TMPro.TMP_Text>().text: \"\"); }}", "Q2"));
  await sleep(600);
  shot("Quiz");
  console.log("CORRECT", exec("using UnityEngine; public class Q3 { public static string Main(){ var b=UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>(); int s0=FluMix.GameSession.Score; b.ResolveQuiz(1); var ex=b.Quiz.transform.Find(\"Explain\"); var head=ex.Find(\"Headline\").GetComponent<TMPro.TMP_Text>().text; return \"delta=\"+(FluMix.GameSession.Score-s0)+\" head=\"+head+\" active=\"+ex.gameObject.activeInHierarchy+\" cue=\"+FluMix.GameSession.LastCue; }}", "Q3"));
  await sleep(500);
  shot("Explain");
  await stop();
  await play("Lobby");
  console.log("WATER", exec("using UnityEngine; public class W2 { public static string Main(){ var l=UnityEngine.Object.FindAnyObjectByType<FluMix.LobbyController>(); l.PlayWaterIsolated(); return \"ok\"; }}", "W2"));
  await sleep(700);
  shot("LobbyWater");
  await stop();
  await play("Leaderboard");
  shot("Leaderboard");
  await stop();
  console.log("DONE");
}
main().catch((e) => { console.error(e.stack || e.message); process.exit(1); });
