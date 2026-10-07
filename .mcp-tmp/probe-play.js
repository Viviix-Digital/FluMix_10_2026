const { spawnSync } = require("child_process");
const fs = require("fs");
const root = "D:/Unity/FluMix_10_2026";
const tmp = root + "/.mcp-tmp";
function pause(ms) { Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms); }
function cli(tool, inputFile, timeout = 90000) {
  let last = "";
  for (let attempt = 0; attempt < 6; attempt++) {
    const r = spawnSync("npx", ["--yes", "unity-mcp-cli", "run-tool", tool, "--path", root, "--input-file", inputFile, "--timeout", String(timeout)], { encoding: "utf8", maxBuffer: 40 * 1024 * 1024, shell: true, cwd: root });
    last = (r.stdout || "") + (r.stderr || "");
    if (r.status === 0 && (r.stdout || "").includes("{")) return r.stdout;
    console.log("RETRY", tool, attempt);
    pause(2000);
  }
  throw new Error(last.slice(0, 800));
}
function parse(s) { return JSON.parse(s.slice(s.indexOf("{"))); }
function exec(code, name) {
  fs.writeFileSync(tmp + "/cap-exec.json", JSON.stringify({ csharpCode: code, className: name, methodName: "Main" }));
  return parse(cli("script-execute", tmp + "/cap-exec.json")).structured.result.value;
}
const open = "using UnityEditor.SceneManagement; public class OpenB { public static string Main(){ var s=EditorSceneManager.OpenScene(\"Assets/FluMix/Scenes/Battle.unity\"); return s.name; } }";
console.log("OPEN", exec(open, "OpenB"));
cli("editor-application-set-state", tmp + "/play.json");
pause(4000);
console.log(exec(fs.readFileSync(tmp + "/probe.cs", "utf8"), "FluMixProbe"));
