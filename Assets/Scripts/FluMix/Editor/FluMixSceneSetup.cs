using System.IO;
using FluMix.Data;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace FluMix.EditorTools
{
    public static class FluMixSceneSetup
    {
        const string ScenePath = "Assets/Scenes/FluMix.unity";
        const string SamplePath = "Assets/Scenes/SampleScene.unity";
        const string ConfigPath = "Assets/Configs/GameConfig.asset";
        const string FontPath = "Assets/Fonts/FluMixVietnamese SDF.asset";
        const string SourceFontPath = "Assets/Fonts/SegoeUI.ttf";
        const string VirusPrefabPath = "Assets/Prefabs/FluMix/Virus.prefab";

        [MenuItem("FluMix/Setup Scene")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Configs");
            Directory.CreateDirectory("Assets/Fonts");
            Directory.CreateDirectory("Assets/Prefabs/FluMix");
            Directory.CreateDirectory("Assets/Art/SpriteCook");

            FixSprites();
            TMP_FontAsset font = LoadOrCreateFont();
            FluMixUi.Font = font;
            GameConfig config = LoadOrCreateConfig();

            if (!File.Exists(ScenePath))
            {
                var sample = EditorSceneManager.OpenScene(SamplePath, OpenSceneMode.Single);
                EditorSceneManager.SaveScene(sample, ScenePath);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("FluMix setup could not find Canvas.");
                return;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.transform.localScale = Vector3.one;

            ApplyFont(canvas, font);

            Transform root = canvas.transform;
            var loginRoot = Find(root, "1. Home Login");
            var rulesRoot = Find(root, "2. Rules");
            var gameplay = Find(root, "3. GamePlay");
            var resultRoot = Find(root, "4. Result");
            var boardRoot = Find(root, "5. Leaderboard");
            var arena = Find(gameplay, "Arena");

            var userField = MakeInput(Find(loginRoot, "Login Panel/User Name"), "User Name Text (TMP)", font, false, "Tên đăng nhập");
            var passField = MakeInput(Find(loginRoot, "Login Panel/Password"), "Password Text (TMP)", font, true, "Mật khẩu");
            var eye = Find(loginRoot, "Login Panel/Password/PreviewPassWord");
            eye.SetAsLastSibling();
            var eyeImage = eye.GetComponent<Image>();
            Sprite eyeOpen = eyeImage.sprite;
            Sprite eyeClosed = LoadSprite("Assets/Design/Elements/HiddenEyeIcon.png");
            var forgot = EnsureButton(Find(loginRoot, "Login Panel"), "ForgotPassword", "Quên mật khẩu?", font, new Vector2(250f, -78f), new Vector2(280f, 36f));
            var forgotMessage = EnsureLabel(Find(loginRoot, "Login Panel"), "ForgotMessage", "Mật khẩu lưu trên máy này", font, new Vector2(0f, -130f), new Vector2(640f, 40f));
            forgotMessage.gameObject.SetActive(false);
            forgotMessage.color = new Color(1f, 0.92f, 0.4f);

            var spawn = EnsureAnchor(arena, "SpawnPoint", new Vector2(860f, -30f));
            var wall = EnsureAnchor(arena, "WallPoint", new Vector2(-210f, -30f));
            SetupHero(Find(arena, "Hero A"), HeroId.A, new Vector2(78f, 36f));
            SetupHero(Find(arena, "Hero G"), HeroId.G, new Vector2(74f, 48f));
            SetupHero(Find(arena, "Hero T"), HeroId.T, new Vector2(62f, 62f));
            VirusView virusPrefab = EnsureVirusPrefab(arena);

            var darken = EnsureDarken(gameplay);
            var countdown = EnsureImage(gameplay, "Countdown", LoadSprite("Assets/Design/Elements/3S.png"), new Vector2(0f, -310f), new Vector2(460f, 170f));
            countdown.preserveAspect = true;
            var winButtonImage = EnsureImage(gameplay, "WinButton", LoadSprite("Assets/Design/Elements/TIẾP TỤC Button.png"), new Vector2(0f, -340f), new Vector2(520f, 130f));
            winButtonImage.preserveAspect = true;
            var winButton = winButtonImage.GetComponent<Button>() ?? winButtonImage.gameObject.AddComponent<Button>();
            winButton.targetGraphic = winButtonImage;

            var question = Find(gameplay, "Question Panel");
            var answerRoot = Find(question, "Answer Option");
            var labels = new TMP_Text[4];
            var buttons = new Button[4];
            string[] optionNames = { "Answer Option A Text (TMP)", "Answer Option B Text (TMP)", "Answer Option C Text (TMP)", "Answer Option D Text (TMP)" };
            for (int i = 0; i < optionNames.Length; i++)
            {
                var option = FindLoose(answerRoot, optionNames[i]);
                labels[i] = option.GetComponent<TMP_Text>();
                labels[i].alignment = TextAlignmentOptions.MidlineLeft;
                labels[i].margin = new Vector4(78f, 0f, 12f, 0f);
                buttons[i] = option.GetComponent<Button>() ?? option.gameObject.AddComponent<Button>();
                buttons[i].targetGraphic = labels[i];
            }

            var explanation = EnsureLabel(question, "Explanation", string.Empty, font, new Vector2(0f, -250f), new Vector2(860f, 70f));
            explanation.alignment = TextAlignmentOptions.Center;
            explanation.fontSize = 24f;
            explanation.gameObject.SetActive(false);

            var right = Find(answerRoot, "Right Answer").GetComponent<Image>();
            var wrong = Find(answerRoot, "Wrong Answer").GetComponent<Image>();
            right.raycastTarget = false;
            wrong.raycastTarget = false;

            var rows = Find(boardRoot, "Leaderboard Panel").Find("Rows");
            if (rows == null)
            {
                var rect = FluMixUi.CreateRect(Find(boardRoot, "Leaderboard Panel"), "Rows", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
                rect.offsetMin = new Vector2(150f, 80f);
                rect.offsetMax = new Vector2(-150f, -250f);
                rows = rect;
            }

            var boss = Find(gameplay, "Boss");
            boss.GetComponent<RectTransform>().anchoredPosition = new Vector2(150f, -20f);
            var bossGroup = boss.GetComponent<CanvasGroup>() ?? boss.gameObject.AddComponent<CanvasGroup>();
            var bossImage = boss.GetComponent<Image>();
            bossImage.raycastTarget = false;
            var bossSheet = boss.GetComponent<SpriteSheetPlayer>() ?? boss.gameObject.AddComponent<SpriteSheetPlayer>();
            bossSheet.Bind(bossImage, config);

            Deactivate(Find(gameplay, "Boss Warning Panel"));
            Deactivate(boss);
            Deactivate(question);
            Deactivate(Find(gameplay, "WinBoss"));
            Deactivate(countdown.transform);
            Deactivate(winButtonImage.transform);
            darken.color = new Color(0f, 0f, 0f, 0f);

            var servicesGo = GameObject.Find("FluMix");
            if (servicesGo == null)
                servicesGo = new GameObject("FluMix");

            var questionsRepo = servicesGo.GetComponent<LocalJsonQuestionRepository>() ?? servicesGo.AddComponent<LocalJsonQuestionRepository>();
            var playersRepo = servicesGo.GetComponent<LocalJsonPlayerRepository>() ?? servicesGo.AddComponent<LocalJsonPlayerRepository>();
            var boardRepo = servicesGo.GetComponent<LocalJsonLeaderboardRepository>() ?? servicesGo.AddComponent<LocalJsonLeaderboardRepository>();
            var services = servicesGo.GetComponent<AppServices>() ?? servicesGo.AddComponent<AppServices>();
            var router = servicesGo.GetComponent<ScreenRouter>() ?? servicesGo.AddComponent<ScreenRouter>();
            var score = servicesGo.GetComponent<ScoreKeeper>() ?? servicesGo.AddComponent<ScoreKeeper>();
            var battle = servicesGo.GetComponent<BattleController>() ?? servicesGo.AddComponent<BattleController>();
            var presenter = servicesGo.GetComponent<QuestionPresenter>() ?? servicesGo.AddComponent<QuestionPresenter>();
            var effects = servicesGo.GetComponent<FxPlayer>() ?? servicesGo.AddComponent<FxPlayer>();
            var sequence = servicesGo.GetComponent<BossSequence>() ?? servicesGo.AddComponent<BossSequence>();
            var flow = servicesGo.GetComponent<GameFlow>() ?? servicesGo.AddComponent<GameFlow>();
            var login = servicesGo.GetComponent<LoginView>() ?? servicesGo.AddComponent<LoginView>();
            var rules = servicesGo.GetComponent<RulesView>() ?? servicesGo.AddComponent<RulesView>();
            var result = servicesGo.GetComponent<ResultView>() ?? servicesGo.AddComponent<ResultView>();
            var leaderboard = servicesGo.GetComponent<LeaderboardView>() ?? servicesGo.AddComponent<LeaderboardView>();
            var debug = servicesGo.GetComponent<DebugPanel>() ?? servicesGo.AddComponent<DebugPanel>();

            SetRef(services, "questions", questionsRepo);
            SetRef(services, "players", playersRepo);
            SetRef(services, "leaderboard", boardRepo);

            router.Bind(loginRoot.gameObject, rulesRoot.gameObject, gameplay.gameObject, resultRoot.gameObject, boardRoot.gameObject);
            score.Bind(config);

            var heroes = new[]
            {
                Find(arena, "Hero A").GetComponent<HeroView>(),
                Find(arena, "Hero G").GetComponent<HeroView>(),
                Find(arena, "Hero T").GetComponent<HeroView>()
            };
            battle.Bind(
                config, score, effects, arena.GetComponent<RectTransform>(), spawn, wall, heroes, virusPrefab,
                LoadSprite("Assets/Design/Characters/GreenVirus.png"),
                LoadSprite("Assets/Design/Characters/OrangeVirus.png"),
                LoadSprite("Assets/Design/Characters/RedVirus.png"));

            presenter.Bind(
                config,
                Find(question, "Question Name Text (TMP)").GetComponent<TMP_Text>(),
                Find(question, "Question Text (TMP)").GetComponent<TMP_Text>(),
                Find(question, "Question Time/Question Time Text (TMP)").GetComponent<TMP_Text>(),
                explanation,
                buttons,
                labels,
                right,
                wrong);

            effects.Bind(
                config,
                arena.GetComponent<RectTransform>(),
                gameplay.GetComponent<RectTransform>(),
                darken,
                Find(gameplay, "Boss Warning Panel").GetComponent<RectTransform>(),
                countdown,
                new[]
                {
                    LoadSprite("Assets/Design/Elements/1S.png"),
                    LoadSprite("Assets/Design/Elements/2S.png"),
                    LoadSprite("Assets/Design/Elements/3S.png")
                },
                boss.GetComponent<RectTransform>(),
                bossGroup,
                bossSheet,
                question.GetComponent<Image>(),
                Find(gameplay, "WinBoss").GetComponent<RectTransform>(),
                winButtonImage,
                LoadSprite("Assets/Design/Elements/TIẾP TỤC Button.png"),
                LoadSprite("Assets/Design/Elements/XEM KẾT QUẢ Button.png"),
                LoadSprite("Assets/Design/Effect/Win Particle Effect.png"),
                LoadSprite("Assets/Design/Effect/VỆT BẮN A.png"),
                LoadSprite("Assets/Design/Effect/VỆT BẮN G.png"),
                LoadSprite("Assets/Design/Effect/VỆT BẮN T.png"),
                LoadSprite("Assets/Design/Effect/Normal Shooting Effect.png"),
                LoadSprite("Assets/Design/Elements/WrongAnswer.png"),
                LoadSprite("Assets/Design/Elements/Plus100.png"),
                LoadSprite("Assets/Design/Elements/Minus50.png"),
                LoadSprite("Assets/Design/Elements/Plus200.png"));
            effects.SetBobTargets(new[]
            {
                Find(arena, "Hero A").GetComponent<RectTransform>(),
                Find(arena, "Hero G").GetComponent<RectTransform>(),
                Find(arena, "Hero T").GetComponent<RectTransform>(),
                Find(rulesRoot, "Mascot").GetComponent<RectTransform>(),
                Find(resultRoot, "Mascot").GetComponent<RectTransform>(),
                Find(boardRoot, "Mascot").GetComponent<RectTransform>()
            });
            AssignSpriteCookSheets(heroes, virusPrefab, bossSheet, effects);

            sequence.Bind(config, flow, score, battle, presenter, effects, services);
            login.Bind(flow, services, userField, passField, Find(loginRoot, "StartButton").GetComponent<Button>(), eye.GetComponent<Button>(), eyeImage, eyeOpen != null ? eyeOpen : eyeClosed, eyeClosed, forgot, forgotMessage, Find(loginRoot, "Login Panel").GetComponent<RectTransform>());
            rules.Bind(flow, Find(rulesRoot, "Next Button").GetComponent<Button>());
            result.Bind(
                flow,
                Find(resultRoot, "Result Panel/Score Text (TMP)").GetComponent<TMP_Text>(),
                Find(resultRoot, "Result Panel/Time Result Text (TMP)").GetComponent<TMP_Text>(),
                Find(resultRoot, "Result Panel/Question Result Text (TMP)").GetComponent<TMP_Text>(),
                Find(resultRoot, "Result Panel/Rank Result Text (TMP)").GetComponent<TMP_Text>(),
                Find(resultRoot, "Play Again Button").GetComponent<Button>(),
                Find(resultRoot, "Ranking Button").GetComponent<Button>());
            leaderboard.Bind(
                flow,
                services,
                rows.GetComponent<RectTransform>(),
                Find(boardRoot, "Play Again Button").GetComponent<Button>(),
                LoadSprite("Assets/Design/Elements/Biểu tượng kim cương Top 1.png"),
                LoadSprite("Assets/Design/Elements/Biểu tượng cúp vàng hạng nhì.png"),
                LoadSprite("Assets/Design/Elements/Cúp bạc hạng ba 03-1.png"));

            var debugCanvas = GameObject.Find("DebugCanvas");
            if (debugCanvas == null)
            {
                debugCanvas = new GameObject("DebugCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var debugScaler = debugCanvas.GetComponent<CanvasScaler>();
                debugScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                debugScaler.referenceResolution = new Vector2(1920f, 1080f);
                debugScaler.matchWidthOrHeight = 0.5f;
                var canvasComponent = debugCanvas.GetComponent<Canvas>();
                canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasComponent.sortingOrder = 100;
            }

            Transform debugPanel = debugCanvas.transform.Find("Panel");
            if (debugPanel == null)
            {
                var panel = FluMixUi.CreateRect(debugCanvas.transform, "Panel", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
                panel.sizeDelta = new Vector2(440f, 0f);
                panel.anchoredPosition = Vector2.zero;
                debugPanel = panel;
            }

            debug.Bind(flow, debugPanel.gameObject);
            flow.SetWinButton(winButton);
            SetRef(flow, "config", config);
            SetRef(flow, "services", services);
            SetRef(flow, "router", router);
            SetRef(flow, "score", score);
            SetRef(flow, "battle", battle);
            SetRef(flow, "boss", sequence);
            SetRef(flow, "questions", presenter);
            SetRef(flow, "fx", effects);
            SetRef(flow, "resultView", result);
            SetRef(flow, "leaderboardView", leaderboard);
            SetRef(flow, "timeText", Find(gameplay, "Status Panel/Time Status Text (TMP)").GetComponent<TMP_Text>());
            SetRef(flow, "scoreText", Find(gameplay, "Status Panel/Score Status Text (TMP) (1)").GetComponent<TMP_Text>());
            SetRef(flow, "winButton", winButton);
            SetRef(flow, "font", font);

            debugPanel.gameObject.SetActive(false);
            Deactivate(root.Find("Fade Transition Effect"));
            ApplyFont(canvas, font);
            router.Show(FluMixScreen.Login);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("FluMix scene setup saved " + ScenePath);
        }

        static void FixSprites()
        {
            string[] folders = { "Assets/Design/Characters", "Assets/Design/Effect", "Assets/Design/Elements" };
            foreach (string folder in folders)
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null || !path.EndsWith(".png"))
                        continue;

                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    bool dirty = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        dirty = true;
                    }

                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        dirty = true;
                    }

                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        dirty = true;
                    }

                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        dirty = true;
                    }

                    if (settings.spriteMeshType != SpriteMeshType.FullRect)
                    {
                        settings.spriteMeshType = SpriteMeshType.FullRect;
                        dirty = true;
                    }

                    if (settings.spriteAlignment != (int)SpriteAlignment.Center)
                    {
                        settings.spriteAlignment = (int)SpriteAlignment.Center;
                        dirty = true;
                    }

                    if (!dirty)
                        continue;

                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }
        }

        static TMP_FontAsset LoadOrCreateFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null)
                return existing;

            if (!File.Exists(SourceFontPath))
            {
                string source = File.Exists(@"C:\Windows\Fonts\segoeui.ttf")
                    ? @"C:\Windows\Fonts\segoeui.ttf"
                    : @"C:\Windows\Fonts\arial.ttf";
                File.Copy(source, SourceFontPath);
                AssetDatabase.ImportAsset(SourceFontPath);
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = "FluMixVietnamese SDF";
            asset.TryAddCharacters("ĂÂÊÔƠƯĐáàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵĂÂÊÔƠƯĐÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴ", out _);
            AssetDatabase.CreateAsset(asset, FontPath);
            if (asset.material != null && !AssetDatabase.Contains(asset.material))
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            if (asset.atlasTextures != null)
            {
                for (int i = 0; i < asset.atlasTextures.Length; i++)
                {
                    Texture2D atlas = asset.atlasTextures[i];
                    if (atlas != null && !AssetDatabase.Contains(atlas))
                        AssetDatabase.AddObjectToAsset(atlas, asset);
                }
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static GameConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        static void AssignSpriteCookSheets(HeroView[] heroes, VirusView virusPrefab, SpriteSheetPlayer bossSheet, FxPlayer effects)
        {
            Sprite[] heroA = LoadSheet("Assets/Art/SpriteCook/hero_a_attack.png");
            Sprite[] heroG = LoadSheet("Assets/Art/SpriteCook/hero_g_attack.png");
            Sprite[] virus = LoadSheet("Assets/Art/SpriteCook/virus_states.png");
            Sprite[] boss = LoadSheet("Assets/Art/SpriteCook/boss_states.png");
            Sprite greenIdle = LoadSprite("Assets/Design/Characters/GreenVirus.png");
            Sprite bossIdle = LoadSprite("Assets/Design/Characters/BÀ BOSS TRÙM.png");

            if (heroes != null)
            {
                BindHeroSheet(heroes, HeroId.A, heroA);
                BindHeroSheet(heroes, HeroId.G, heroG);
            }

            if (virusPrefab != null)
            {
                virusPrefab.SetClip(0, 8, 8, 4, 0, 0);
                SpriteSheetPlayer sheet = virusPrefab.Sheet != null
                    ? virusPrefab.Sheet
                    : virusPrefab.GetComponent<SpriteSheetPlayer>();
                if (sheet != null && virus != null && virus.Length > 0)
                    sheet.SetFrames(virus, greenIdle);
                EditorUtility.SetDirty(virusPrefab.gameObject);
            }

            if (bossSheet != null && boss != null && boss.Length > 0)
            {
                bossSheet.SetFrames(boss, bossIdle);
                EditorUtility.SetDirty(bossSheet);
            }

            if (effects != null)
            {
                effects.SetBossClip(0, 4, 4, 4, 8, 4);
                EditorUtility.SetDirty(effects);
            }
        }

        static void BindHeroSheet(HeroView[] heroes, HeroId id, Sprite[] frames)
        {
            if (heroes == null || frames == null || frames.Length == 0)
                return;

            for (int i = 0; i < heroes.Length; i++)
            {
                HeroView hero = heroes[i];
                if (hero == null || hero.Id != id)
                    continue;
                SpriteSheetPlayer sheet = hero.Sheet != null ? hero.Sheet : hero.GetComponent<SpriteSheetPlayer>();
                if (sheet == null)
                    continue;
                Sprite idle = hero.Image != null ? hero.Image.sprite : hero.GetComponent<Image>().sprite;
                sheet.SetFrames(frames, idle);
                EditorUtility.SetDirty(hero);
            }
        }

        static Sprite[] LoadSheet(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var frames = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                var sprite = assets[i] as Sprite;
                if (sprite != null && sprite.name.IndexOf('_') >= 0)
                    frames.Add(sprite);
            }

            frames.Sort((a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
            return frames.ToArray();
        }

        static int FrameIndex(string name)
        {
            int underscore = name.LastIndexOf('_');
            if (underscore < 0 || underscore == name.Length - 1)
                return 0;
            int value;
            return int.TryParse(name.Substring(underscore + 1), out value) ? value : 0;
        }

        static VirusView EnsureVirusPrefab(Transform arena)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(VirusPrefabPath);
            Transform source = Find(arena, "Green Virus");
            if (source != null)
            {
                var view = source.GetComponent<VirusView>() ?? source.gameObject.AddComponent<VirusView>();
                var image = source.GetComponent<Image>();
                image.raycastTarget = false;
                var sheet = source.GetComponent<SpriteSheetPlayer>() ?? source.gameObject.AddComponent<SpriteSheetPlayer>();
                sheet.Bind(image, AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath));
                PrefabUtility.SaveAsPrefabAsset(source.gameObject, VirusPrefabPath);
                DestroySceneVirus(Find(arena, "Green Virus"));
                DestroySceneVirus(Find(arena, "Orange Virus"));
                DestroySceneVirus(Find(arena, "Red Virus"));
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VirusPrefabPath);
            return prefab != null ? prefab.GetComponent<VirusView>() : null;
        }

        static void DestroySceneVirus(Transform virus)
        {
            if (virus != null)
                Object.DestroyImmediate(virus.gameObject);
        }

        static void SetupHero(Transform hero, HeroId id, Vector2 muzzleLocal)
        {
            var image = hero.GetComponent<Image>();
            image.raycastTarget = true;
            var button = hero.GetComponent<Button>() ?? hero.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var sheet = hero.GetComponent<SpriteSheetPlayer>() ?? hero.gameObject.AddComponent<SpriteSheetPlayer>();
            sheet.Bind(image, AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath));
            var view = hero.GetComponent<HeroView>() ?? hero.gameObject.AddComponent<HeroView>();
            Transform muzzle = hero.Find("Muzzle");
            if (muzzle == null)
            {
                var rect = FluMixUi.CreateRect(hero, "Muzzle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                rect.anchoredPosition = muzzleLocal;
                rect.sizeDelta = new Vector2(10f, 10f);
                muzzle = rect;
            }

            view.Bind(id, (RectTransform)muzzle);
            string prefabPath = "Assets/Prefabs/FluMix/Hero" + id + ".prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(hero.gameObject, prefabPath, InteractionMode.AutomatedAction);
        }

        static TMP_InputField MakeInput(Transform field, string textName, TMP_FontAsset font, bool password, string placeholderText)
        {
            var existing = field.GetComponent<TMP_InputField>();
            if (existing != null)
                return existing;

            var text = FindLoose(field, textName).GetComponent<TMP_Text>();
            var area = FluMixUi.CreateRect(field, "Text Area", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            area.offsetMin = new Vector2(78f, 10f);
            area.offsetMax = new Vector2(password ? -78f : -16f, -10f);
            area.gameObject.AddComponent<RectMask2D>();
            text.transform.SetParent(area, false);
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.font = font;
            text.text = string.Empty;
            text.color = Color.white;

            var placeholderObject = Object.Instantiate(text.gameObject, area);
            placeholderObject.name = "Placeholder";
            var placeholder = placeholderObject.GetComponent<TMP_Text>();
            placeholder.text = placeholderText;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = new Color(1f, 1f, 1f, 0.55f);
            placeholder.raycastTarget = false;

            var input = field.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = font;
            input.pointSize = text.fontSize > 1f ? text.fontSize : 32f;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            return input;
        }

        static Image EnsureDarken(Transform gameplay)
        {
            Transform existing = gameplay.Find("Darken");
            if (existing != null)
                return existing.GetComponent<Image>();

            var rect = FluMixUi.CreateRect(gameplay, "Darken", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = FluMixUi.White;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            rect.SetSiblingIndex(1);
            return image;
        }

        static Image EnsureImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            Transform existing = parent.Find(name);
            Image image;
            RectTransform rect;
            if (existing == null)
            {
                image = FluMixUi.CreateImage(parent, name, sprite, size, true);
                rect = image.rectTransform;
            }
            else
            {
                rect = (RectTransform)existing;
                image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
                image.sprite = sprite;
            }

            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        static Button EnsureButton(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position, Vector2 size)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<Button>();

            FluMixUi.Font = font;
            Image image = FluMixUi.CreateImage(parent, name, FluMixUi.White, size, true);
            image.color = new Color(1f, 1f, 1f, 0f);
            image.rectTransform.anchoredPosition = position;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = FluMixUi.CreateText(image.transform, "Label", label, 22f, TextAlignmentOptions.MidlineRight);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.color = new Color(0.85f, 0.95f, 1f);
            return button;
        }

        static TMP_Text EnsureLabel(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position, Vector2 size)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<TMP_Text>();

            FluMixUi.Font = font;
            var text = FluMixUi.CreateText(parent, name, label, 24f, TextAlignmentOptions.Center);
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = size;
            return text;
        }

        static RectTransform EnsureAnchor(Transform parent, string name, Vector2 position)
        {
            Transform existing = parent.Find(name);
            RectTransform rect = existing != null
                ? (RectTransform)existing
                : FluMixUi.CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(20f, 20f);
            return rect;
        }

        static void ApplyFont(GameObject root, TMP_FontAsset font)
        {
            if (font == null)
                return;
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
                texts[i].font = font;
        }

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError("Missing sprite " + path);
            return sprite;
        }

        static Transform Find(Transform root, string path)
        {
            if (root == null)
                return null;
            Transform found = root.Find(path);
            if (found == null)
                Debug.LogError("Missing " + root.name + "/" + path);
            return found;
        }

        static Transform FindLoose(Transform parent, string name)
        {
            string trimmed = name.Trim();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.Trim() == trimmed)
                    return child;
            }

            Debug.LogError("Missing child " + name + " under " + parent.name);
            return null;
        }

        static void Deactivate(Transform target)
        {
            if (target != null)
                target.gameObject.SetActive(false);
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("Missing field " + field + " on " + target.GetType().Name);
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
