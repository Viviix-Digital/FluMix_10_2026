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
    console.log("RETRY", tool, attempt, last.slice(0, 160).replace(/\s+/g, " "));
    pause(2500);
  }
  throw new Error(tool + "\n" + last.slice(0, 900));
}
function parse(stdout) { return JSON.parse(stdout.slice(stdout.indexOf("{"))); }
function exec(code, className) {
  fs.writeFileSync(tmp + "/cap-exec.json", JSON.stringify({ csharpCode: code, className, methodName: "Main" }));
  const j = parse(cli("script-execute", tmp + "/cap-exec.json", 120000));
  return j.structured.result.value;
}
function state() { return parse(cli("editor-application-get-state", tmp + "/get-state.json", 30000)).structured.result; }
async function until(pred, label) {
  for (let i = 0; i < 25; i++) {
    const s = state();
    if (pred(s)) return;
    await sleep(400);
  }
  throw new Error("timeout " + label);
}
function shot(name) {
  const j = parse(cli("screenshot-game-view", tmp + "/shot-input.json", 60000));
  const img = (j.content || []).find((c) => c.type === "image");
  if (!img) throw new Error("no image " + name);
  fs.writeFileSync(root + "/Assets/FluMix/Screenshots/" + name + ".png", Buffer.from(img.data, "base64"));
  console.log("SHOT", name, ((j.content || []).find((c) => c.type === "text") || {}).text || "");
}
async function stop() {
  cli("editor-application-set-state", tmp + "/stop.json", 60000);
  await until((s) => s.IsPlaying === false && s.IsPlayingOrWillChangePlaymode === false, "stop");
}
async function play(scene) {
  const s = state();
  if (s.IsPlaying || s.IsPlayingOrWillChangePlaymode) await stop();
  const open = "using UnityEditor.SceneManagement;\npublic class OpenOnly { public static string Main(){ var scene=EditorSceneManager.OpenScene(\"Assets/FluMix/Scenes/" + scene + ".unity\"); return scene.name; } }";
  console.log("OPEN", exec(open, "OpenOnly"));
  cli("editor-application-set-state", tmp + "/play.json", 60000);
  await sleep(2500);
  await until((s2) => s2.IsPlaying === true, scene);
  await sleep(1200);
}

async function main() {
  const combat =
    "using UnityEngine; public class CombatProbe { public static string Main() {\n" +
    "  var b = UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>();\n" +
    "  b.SelectGuardian(0);\n" +
    "  var hit = b.Spawn(FluMix.FluMixFlow.Instance.ArchetypeFor(FluMix.GuardianId.G), true);\n" +
    "  hit.transform.position = new Vector3(-1.5f, 0f, 0f);\n" +
    "  foreach (var a in b.Alive) if (a != null && a != hit) a.transform.position = new Vector3(8f, 0f, 0f);\n" +
    "  int s0 = FluMix.GameSession.Score;\n" +
    "  b.ConfirmShot();\n" +
    "  int s1 = FluMix.GameSession.Score;\n" +
    "  b.SelectGuardian(0);\n" +
    "  var miss = b.Spawn(FluMix.FluMixFlow.Instance.ArchetypeFor(FluMix.GuardianId.A), true);\n" +
    "  miss.transform.position = new Vector3(-1.2f, 0.4f, 0f);\n" +
    "  int tier0 = miss.Tier;\n" +
    "  b.ConfirmShot();\n" +
    "  return \"hit=\" + (s1 - s0) + \" missDelta=\" + (FluMix.GameSession.Score - s1) + \" tier=\" + tier0 + \"->\" + miss.Tier + \" cue=\" + FluMix.GameSession.LastCue + \" :: \" + string.Join(\" | \", FluMix.FluMixLog.Tail);\n}}\n";
  console.log("COMBAT", exec(combat, "CombatProbe"));
  shot("Battle");
  const quiz =
    "using UnityEngine; public class QuizOpen { public static string Main() {\n" +
    "  var b = UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>();\n" +
    "  b.OpenQuiz(false);\n" +
    "  var q = FluMix.FluMixFlow.Instance.Questions.Get(FluMix.GameSession.QuestionIndex);\n" +
    "  return \"open=\" + b.QuizOpen + \" \" + q.prompt;\n}}\n";
  console.log("QUIZ", exec(quiz, "QuizOpen"));
  await sleep(800);
  shot("Quiz");
  const correct =
    "using UnityEngine; public class QuizOk { public static string Main() {\n" +
    "  var b = UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>();\n" +
    "  int s0 = FluMix.GameSession.Score;\n" +
    "  b.ResolveQuiz(1);\n" +
    "  return \"quizDelta=\" + (FluMix.GameSession.Score - s0) + \" cue=\" + FluMix.GameSession.LastCue;\n}}\n";
  console.log("CORRECT", exec(correct, "QuizOk"));
  await sleep(800);
  shot("Explain");
  await stop();

  await play("Lobby");
  shot("Lobby");
  console.log("WATER", exec("using UnityEngine; public class WaterNow { public static string Main(){ var lobby=UnityEngine.Object.FindAnyObjectByType<FluMix.LobbyController>(); if(lobby==null) return \"no lobby\"; lobby.PlayWaterIsolated(); return \"water\"; } }", "WaterNow"));
  await sleep(800);
  shot("LobbyWater");
  await stop();

  await play("HowToPlay");
  shot("HowToPlay");
  await stop();

  await play("Leaderboard");
  shot("Leaderboard");
  await stop();
  console.log("DONE");
}

main().catch((e) => { console.error(e.stack || e.message); process.exit(1); });
