const fs = require("fs");
const path = "D:/Unity/FluMix_10_2026/Assets/FluMix/Scripts/";

function dump(filePath, content, outName) {
  const p = "D:/Unity/FluMix_10_2026/.mcp-tmp/" + outName;
  fs.writeFileSync(p, JSON.stringify({ filePath, content }));
  console.log(outName, content.length);
}

function read(file) {
  return fs.readFileSync(file, "utf8").replace(/\r\n/g, "\n");
}
let dbg = read(path + "FluMixDebug.cs");
if (!dbg.includes("bool _visible = true")) throw new Error("visible");
dbg = dbg.replace("bool _visible = true", "bool _visible = false");
const buildEnd = "            Refresh();\n        }\n\n        static Image NewImage";
if (!dbg.includes(buildEnd)) throw new Error("build end");
dbg = dbg.replace(buildEnd, "            Refresh();\n            _canvas.gameObject.SetActive(_visible);\n        }\n\n        static Image NewImage");
dump("Assets/FluMix/Scripts/FluMixDebug.cs", dbg, "upd-debug.json");

let b = read(path + "Editor/FluMixSceneBuilder.cs");
if (!b.includes("importer.textureType = TextureImporterType.Sprite;")) throw new Error("importer");
if (!b.includes("spriteImportMode = SpriteImportMode.Single")) {
  b = b.replace(
    "importer.textureType = TextureImporterType.Sprite;",
    "importer.textureType = TextureImporterType.Sprite;\n            importer.spriteImportMode = SpriteImportMode.Single;"
  );
}
dump("Assets/FluMix/Scripts/Editor/FluMixSceneBuilder.cs", b, "upd-builder.json");

let f = read(path + "FluMixFlow.cs");
const marker = 'if (gameObject.scene.name == "Boot")';
if (!f.includes(marker)) throw new Error("start missing");
if (!f.includes("AdoptScene")) {
  f = f.replace(
    `                Go(FlowState.Login, true);
        }`,
    `                Go(FlowState.Login, true);
            else
                AdoptScene(SceneManager.GetActiveScene().name);
        }

        public void AdoptScene(string sceneName)
        {
            foreach (FlowState state in System.Enum.GetValues(typeof(FlowState)))
            {
                if (SceneFor(state) == sceneName)
                {
                    State = state;
                    return;
                }
            }
        }`
  );
}
if (!f.includes("AdoptScene")) throw new Error("adopt failed");
dump("Assets/FluMix/Scripts/FluMixFlow.cs", f, "upd-flow.json");
