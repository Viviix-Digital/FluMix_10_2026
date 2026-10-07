const { execFileSync } = require("child_process");
const fs = require("fs");
const path = require("path");

const root = "D:/Unity/FluMix_10_2026";
const files = [
  ["FluMixCore.cs", "Assets/FluMix/Scripts/FluMixCore.cs"],
  ["FluMixFlow.cs", "Assets/FluMix/Scripts/FluMixFlow.cs"],
  ["FluMixAudio.cs", "Assets/FluMix/Scripts/FluMixAudio.cs"],
  ["FluMixBattle.cs", "Assets/FluMix/Scripts/FluMixBattle.cs"],
  ["FluMixScreens.cs", "Assets/FluMix/Scripts/FluMixScreens.cs"],
  ["FluMixDebug.cs", "Assets/FluMix/Scripts/FluMixDebug.cs"],
  ["FluMixSceneBuilder.cs", "Assets/FluMix/Scripts/Editor/FluMixSceneBuilder.cs"],
];

for (const [src, dest] of files) {
  const content = fs.readFileSync(path.join(root, ".mcp-tmp", src), "utf8");
  const payloadPath = path.join(root, ".mcp-tmp", "payload.json");
  fs.writeFileSync(payloadPath, JSON.stringify({ filePath: dest, content }));
  console.log("PUSH", dest, content.length);
  try {
    const out = execFileSync(
      "npx",
      ["--yes", "unity-mcp-cli", "run-tool", "script-update-or-create", "--path", root, "--input-file", payloadPath, "--timeout", "180000"],
      { encoding: "utf8", shell: true, maxBuffer: 20 * 1024 * 1024 }
    );
    console.log(out.slice(0, 1200));
  } catch (err) {
    console.log("FAIL", dest);
    console.log((err.stdout || "").slice(0, 2000));
    console.log((err.stderr || "").slice(0, 2000));
    process.exit(1);
  }
}
console.log("ALL PUSHED");
