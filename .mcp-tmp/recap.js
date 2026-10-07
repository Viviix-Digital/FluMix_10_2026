const { spawnSync } = require("child_process");
const fs = require("fs");
const root = "D:/Unity/FluMix_10_2026";
const tmp = root + "/.mcp-tmp";
function pause(ms) { Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms); }
function sleep(ms) { return new Promise((r) => setTimeout(r, ms)); }
function cli(tool, inputFile, timeout = 90000) {
  let last = "";
  for (let a = 0; a < 8; a++) {
    const r = spawnSync("npx", ["--yes", "unity-mcp-cli", "run-tool", tool, "--path", root, "--input-file", inputFile, "--timeout", String(timeout)], { encoding: "utf8", maxBuffer: 40 * 1024 * 1024, shell: true, cwd: root });
    last = (r.stdout || "") + (r.stderr || "");
    if (r.status === 0 && (r.stdout || "").includes("{")) return r.stdout || "";
    pause(2000);
  }
  throw new Error(last.slice(0, 700));
}
function parse(s) { return JSON.parse(s.slice(s.indexOf("{"))); }
function exec(code, name) {
  fs.writeFileSync(tmp + "/cap-exec.json", JSON.stringify({ csharpCode: code, className: name, methodName: "Main" }));
  return parse(cli("script-execute", tmp + "/cap-exec.json")).structured.result.value;
}
function state() { return parse(cli("editor-application-get-state", tmp + "/get-state.json")).structured.result; }
async function until(pred) { for (let i = 0; i < 25; i++) { if (pred(state())) return; await sleep(400); } throw new Error("timeout"); }
function shot(name) {
  const j = parse(cli("screenshot-game-view", tmp + "/shot-input.json"));
  const img = j.content.find((c) => c.type === "image");
  fs.writeFileSync(root + "/Assets/FluMix/Screenshots/" + name + ".png", Buffer.from(img.data, "base64"));
  console.log("SHOT", name);
}
async function stop() { cli("editor-application-set-state", tmp + "/stop.json"); await until((s) => !s.IsPlaying && !s.IsPlayingOrWillChangePlaymode); }
async function play(scene) {
  if (state().IsPlaying || state().IsPlayingOrWillChangePlaymode) await stop();
  console.log(exec("using UnityEditor.SceneManagement; public class OpenOnly { public static string Main(){ return EditorSceneManager.OpenScene(\"Assets/FluMix/Scenes/" + scene + ".unity\").name; } }", "OpenOnly"));
  cli("editor-application-set-state", tmp + "/play.json");
  await sleep(1800);
  await until((s) => s.IsPlaying);
  await sleep(700);
}
async function main() {
  await play("Battle");
  console.log("QUIZ", exec("using UnityEngine; public class Q2 { public static string Main(){ var b=UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>(); b.OpenQuiz(false); return b.Quiz.transform.Find(\"Panel/Prompt\").GetComponent<TMPro.TMP_Text>().text; }}", "Q2"));
  await sleep(500);
  shot("Quiz");
  console.log("OK", exec("using UnityEngine; public class Q3 { public static string Main(){ var b=UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>(); b.ResolveQuiz(1); return \"score=\"+FluMix.GameSession.Score; }}", "Q3"));
  await sleep(900);
  shot("Explain");
  await stop();
  await play("Lobby");
  exec("using UnityEngine; public class W2 { public static string Main(){ UnityEngine.Object.FindAnyObjectByType<FluMix.LobbyController>().PlayWaterIsolated(); return \"w\"; }}", "W2");
  await sleep(700);
  shot("LobbyWater");
  await stop();
  await play("Leaderboard");
  shot("Leaderboard");
  await stop();
  console.log("DONE");
}
main().catch((e) => { console.error(e.stack || e.message); process.exit(1); });
