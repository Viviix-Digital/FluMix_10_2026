const fs = require("fs");
const path = require("path");

function splitFile(src, classNames, outDir) {
  let text = fs.readFileSync(src, "utf8").replace(/\r\n/g, "\n");
  const usings = text.slice(0, text.indexOf("namespace"));
  for (const name of classNames) {
    const token = "\n    public class " + name;
    const at = text.indexOf(token);
    if (at < 0) throw new Error("missing " + name + " in " + src);
    const start = at + 1;
    const brace = text.indexOf("{", start);
    let depth = 0;
    let end = -1;
    for (let i = brace; i < text.length; i++) {
      if (text[i] === "{") depth++;
      else if (text[i] === "}") {
        depth--;
        if (depth === 0) {
          end = i + 1;
          break;
        }
      }
    }
    if (end < 0) throw new Error("unclosed " + name);
    const body = text.slice(start, end).trimEnd() + "\n";
    const content = usings + "namespace FluMix\n{\n" + body + "}\n";
    fs.writeFileSync(path.join(outDir, name + ".cs"), content);
    text = text.slice(0, start) + text.slice(end).replace(/^\n+/, "\n");
    console.log(name, content.length);
  }
  return text.replace(/\n{3,}/g, "\n\n");
}

const out = "D:/Unity/FluMix_10_2026/.mcp-tmp/split";
fs.mkdirSync(out, { recursive: true });
const battle = splitFile(
  "D:/Unity/FluMix_10_2026/Assets/FluMix/Scripts/FluMixBattle.cs",
  ["Virus", "Guardian", "Beam", "BattleController"],
  out
);
fs.writeFileSync(path.join(out, "FluMixBattle.cs"), battle);
const screens = splitFile(
  "D:/Unity/FluMix_10_2026/Assets/FluMix/Scripts/FluMixScreens.cs",
  ["PillButton", "TitleBob", "LoginController", "HowToController", "LobbyController", "LeaderboardController", "RibbonMotion", "BattleHud", "QuizPresenter"],
  out
);
fs.writeFileSync(path.join(out, "FluMixScreens.cs"), screens);
console.log("battle left", battle.length, "screens left", screens.length);
console.log("--- battle tail ---");
console.log(battle.slice(-200));
console.log("--- screens head ---");
console.log(screens.slice(0, 250));
