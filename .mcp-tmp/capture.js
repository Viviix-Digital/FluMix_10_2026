const { spawnSync } = require("child_process");
const fs = require("fs");

const root = "D:/Unity/FluMix_10_2026";
const tmp = root + "/.mcp-tmp";

function pause(ms) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms);
}

function cli(tool, inputFile, timeout = 90000) {
  let last = "";
  for (let attempt = 0; attempt < 8; attempt++) {
    const r = spawnSync(
      "npx",
      ["--yes", "unity-mcp-cli", "run-tool", tool, "--path", root, "--input-file", inputFile, "--timeout", String(timeout)],
      { encoding: "utf8", maxBuffer: 40 * 1024 * 1024, shell: true, cwd: root }
    );
    last = (r.stdout || "") + (r.stderr || "");
    if (r.status === 0 && (r.stdout || "").includes("{")) return r.stdout || "";
    console.log("RETRY", tool, attempt, last.slice(0, 180).replace(/\s+/g, " "));
    pause(2500);
  }
  throw new Error(tool + " failed\n" + last.slice(0, 1200));
}

function parse(stdout) {
  const i = stdout.indexOf("{");
  if (i < 0) throw new Error("no json\n" + stdout.slice(0, 800));
  return JSON.parse(stdout.slice(i));
}

function exec(code, className) {
  const file = tmp + "/cap-exec.json";
  fs.writeFileSync(file, JSON.stringify({ csharpCode: code, className, methodName: "Main" }));
  const j = parse(cli("script-execute", file, 120000));
  return j.structured.result.value;
}

function state() {
  const j = parse(cli("editor-application-get-state", tmp + "/get-state.json", 30000));
  return j.structured.result;
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function until(pred, label) {
  for (let i = 0; i < 25; i++) {
    const s = state();
    if (pred(s)) return s;
    await sleep(400);
  }
  throw new Error("timeout " + label);
}

function shot(name) {
  const j = parse(cli("screenshot-game-view", tmp + "/shot-input.json", 60000));
  const img = (j.content || []).find((c) => c.type === "image");
  if (!img) throw new Error("no image " + name + "\n" + JSON.stringify(j).slice(0, 500));
  const dest = root + "/Assets/FluMix/Screenshots/" + name + ".png";
  fs.writeFileSync(dest, Buffer.from(img.data, "base64"));
  const caption = ((j.content || []).find((c) => c.type === "text") || {}).text || "";
  console.log("SHOT", name, caption);
}

async function play(scene) {
  let s = state();
  if (s.IsPlaying || s.IsPlayingOrWillChangePlaymode) await stop();
  const open =
    "using UnityEditor.SceneManagement;\npublic class OpenOnly {\n" +
    "  public static string Main() {\n" +
    "    var scene = EditorSceneManager.OpenScene(\"Assets/FluMix/Scenes/" + scene + ".unity\");\n" +
    "    return scene.name;\n  }\n}\n";
  console.log("OPEN", exec(open, "OpenOnly"));
  cli("editor-application-set-state", tmp + "/play.json", 60000);
  await sleep(2500);
  await until((s2) => s2.IsPlaying === true, "playing " + scene);
  await sleep(1500);
}

async function stop() {
  cli("editor-application-set-state", tmp + "/stop.json", 60000);
  await until((s) => s.IsPlaying === false && s.IsPlayingOrWillChangePlaymode === false, "stopped");
}

async function main() {
  fs.mkdirSync(root + "/Assets/FluMix/Screenshots", { recursive: true });
  await play("HowToPlay");
  shot("HowToPlay");
  await stop();

  await play("Lobby");
  shot("Lobby");
  const water =
    "using UnityEngine; public class WaterNow { public static string Main() {\n" +
    "  var lobby = UnityEngine.Object.FindAnyObjectByType<FluMix.LobbyController>();\n" +
    "  if (lobby == null) return \"no lobby\";\n" +
    "  lobby.PlayWaterIsolated();\n" +
    "  return \"water\";\n}}\n";
  console.log("WATER", exec(water, "WaterNow"));
  await sleep(800);
  shot("LobbyWater");
  await stop();

  await play("Battle");
  shot("Battle");
  const combat =
    "using UnityEngine; public class CombatProbe { public static string Main() {\n" +
    "  var b = UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>();\n" +
    "  if (b == null) return \"no battle\";\n" +
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
  const quiz =
    "using UnityEngine; public class QuizOpen { public static string Main() {\n" +
    "  var b = UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>();\n" +
    "  var q = FluMix.FluMixFlow.Instance.Questions.Get(0);\n" +
    "  b.OpenQuiz(false);\n" +
    "  return q.prompt + \" | \" + q.options[0] + \" / \" + q.options[1] + \" / \" + q.options[2] + \" / \" + q.options[3] + \" idx=\" + q.correctIndex;\n}}\n";
  console.log("QUIZ", exec(quiz, "QuizOpen"));
  await sleep(700);
  shot("Quiz");
  const correct =
    "using UnityEngine; public class QuizOk { public static string Main() {\n" +
    "  var b = UnityEngine.Object.FindAnyObjectByType<FluMix.BattleController>();\n" +
    "  int s0 = FluMix.GameSession.Score;\n" +
    "  b.ResolveQuiz(1);\n" +
    "  return \"quizDelta=\" + (FluMix.GameSession.Score - s0) + \" cue=\" + FluMix.GameSession.LastCue;\n}}\n";
  console.log("CORRECT", exec(correct, "QuizOk"));
  await sleep(700);
  shot("Explain");
  await stop();

  await play("Leaderboard");
  shot("Leaderboard");
  await stop();

  console.log("DONE");
}

main().catch((e) => {
  console.error(e.stack || e.message);
  process.exit(1);
});
