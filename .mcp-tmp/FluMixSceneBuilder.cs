using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FluMix.EditorTools
{
    public static class FluMixSceneBuilder
    {
        static TMP_FontAsset _font;
        static Sprite _soft;
        static Sprite _ring;
        static Sprite _pill;
        static Sprite _frame;
        static Sprite _ribbon;
        static Sprite _hero;
        static Sprite _castle;
        static Sprite _virus;
        static Sprite _loginBg;
        static Sprite _hallBg;
        static Sprite _battleBg;
        static StringTable _strings;
        static QuestionBank _questions;
        static RoundRules _rules;
        static UiMotionConfig _motion;
        static AudioBank _audio;
        static VirusArchetype _green;
        static VirusArchetype _blue;
        static VirusArchetype _pink;
        static UnityEngine.Audio.AudioMixer _mixer;

        public static void BuildAll()
        {
            EnsureFolder("Assets/FluMix/Art");
            EnsureFolder("Assets/FluMix/Data");
            EnsureFolder("Assets/FluMix/Audio");
            EnsureFolder("Assets/FluMix/Prefabs");
            EnsureFolder("Assets/FluMix/Scenes");
            EnsureFolder("Assets/FluMix/Screenshots");
            BuildArt();
            BuildFont();
            BuildData();
            BuildAudio();
            BuildPrefabs();
            BuildBoot();
            BuildLogin();
            BuildHowTo();
            BuildLobby();
            BuildBattle();
            BuildLeaderboard();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/FluMix/Scenes/Boot.unity", true),
                new EditorBuildSettingsScene("Assets/FluMix/Scenes/Login.unity", true),
                new EditorBuildSettingsScene("Assets/FluMix/Scenes/Lobby.unity", true),
                new EditorBuildSettingsScene("Assets/FluMix/Scenes/HowToPlay.unity", true),
                new EditorBuildSettingsScene("Assets/FluMix/Scenes/Battle.unity", true),
                new EditorBuildSettingsScene("Assets/FluMix/Scenes/Leaderboard.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[FluMix] BuildAll complete");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static void BuildArt()
        {
            _soft = SaveSprite("Assets/FluMix/Art/soft.png", PaintCircle(128, false), 100f, Vector4.zero);
            _ring = SaveSprite("Assets/FluMix/Art/ring.png", PaintCircle(128, true), 100f, Vector4.zero);
            _pill = SaveSprite("Assets/FluMix/Art/pill.png", PaintRoundRect(128, 64, 28, true), 100f, new Vector4(32, 32, 32, 32));
            _frame = SaveSprite("Assets/FluMix/Art/frame.png", PaintRoundRect(128, 128, 28, false), 100f, new Vector4(40, 40, 40, 40));
            _ribbon = SaveSprite("Assets/FluMix/Art/ribbon.png", PaintRibbon(256, 48), 100f, Vector4.zero);
            _hero = SaveSprite("Assets/FluMix/Art/hero.png", PaintHero(256, 360), 100f, Vector4.zero);
            _castle = SaveSprite("Assets/FluMix/Art/castle.png", PaintCastle(320, 360), 100f, Vector4.zero);
            _virus = SaveSprite("Assets/FluMix/Art/virus.png", PaintVirus(160), 100f, Vector4.zero);
            _loginBg = SaveSprite("Assets/FluMix/Art/login-bg.png", PaintLoginBg(960, 540), 100f, Vector4.zero);
            _hallBg = SaveSprite("Assets/FluMix/Art/hall-bg.png", PaintHall(960, 540), 100f, Vector4.zero);
            _battleBg = SaveSprite("Assets/FluMix/Art/battle-bg.png", PaintBattleBg(960, 540), 100f, Vector4.zero);
        }

        static void BuildFont()
        {
            string path = "Assets/FluMix/Art/FluMixFont.asset";
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (_font != null) return;
            Font os = null;
            foreach (var name in new[] { "Segoe UI", "Arial", "Tahoma" })
            {
                os = Font.CreateDynamicFontFromOSFont(name, 64);
                if (os != null) break;
            }
            if (os != null)
            {
                try
                {
                    _font = TMP_FontAsset.CreateFontAsset(os, 64, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[FluMix] Dynamic font failed: " + e.Message);
                }
            }
            if (_font == null)
            {
                _font = TMP_Settings.defaultFontAsset;
                Debug.LogWarning("[FluMix] Using default TMP font.");
                return;
            }
            AssetDatabase.CreateAsset(_font, path);
            EditorUtility.SetDirty(_font);
        }

        static void BuildData()
        {
            _strings = LoadOrCreate<StringTable>("Assets/FluMix/Data/StringTable.asset");
            FluMixContent.ApplyStrings(_strings);
            _questions = LoadOrCreate<QuestionBank>("Assets/FluMix/Data/QuestionBank.asset");
            FluMixContent.ApplyQuestions(_questions);
            _rules = LoadOrCreate<RoundRules>("Assets/FluMix/Data/RoundRules.asset");
            FluMixContent.ApplyRules(_rules);
            _motion = LoadOrCreate<UiMotionConfig>("Assets/FluMix/Data/UiMotionConfig.asset");
            _green = LoadOrCreate<VirusArchetype>("Assets/FluMix/Data/VirusGreen.asset");
            _blue = LoadOrCreate<VirusArchetype>("Assets/FluMix/Data/VirusBlue.asset");
            _pink = LoadOrCreate<VirusArchetype>("Assets/FluMix/Data/VirusPink.asset");
            _green.guardian = GuardianId.G;
            _green.color = new Color(0.35f, 0.95f, 0.35f);
            _green.displayNameId = "howto.green";
            _green.baseSpeed = 1.35f;
            _blue.guardian = GuardianId.A;
            _blue.color = new Color(0.25f, 0.75f, 1f);
            _blue.displayNameId = "howto.blue";
            _blue.baseSpeed = 1.35f;
            _pink.guardian = GuardianId.T;
            _pink.color = new Color(1f, 0.35f, 0.7f);
            _pink.displayNameId = "howto.pink";
            _pink.baseSpeed = 1.35f;
            EditorUtility.SetDirty(_strings);
            EditorUtility.SetDirty(_questions);
            EditorUtility.SetDirty(_rules);
            EditorUtility.SetDirty(_green);
            EditorUtility.SetDirty(_blue);
            EditorUtility.SetDirty(_pink);
            _audio = LoadOrCreate<AudioBank>("Assets/FluMix/Data/AudioBank.asset");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void BuildAudio()
        {
            string[] cues =
            {
                "music_menu", "music_battle", "stinger_sos", "ui_hover", "ui_confirm", "ui_deny",
                "ui_modal_open", "ui_modal_close", "ui_page", "input_key", "input_password",
                "quiz_select", "quiz_unlock", "quiz_correct", "quiz_wrong", "quiz_explain",
                "beam_green", "beam_blue", "beam_pink", "impact_correct", "impact_wrong",
                "virus_spawn", "virus_evolve", "virus_leak", "timer_tick", "timer_end",
                "warning_alarm", "water_loop", "podium_land"
            };
            _audio.cues.Clear();
            for (int i = 0; i < cues.Length; i++)
            {
                float freq = 220f + (i * 37f) % 880f;
                float seconds = cues[i].StartsWith("music_") || cues[i] == "water_loop" || cues[i] == "warning_alarm" ? 1.6f : 0.28f;
                bool noise = cues[i].Contains("water") || cues[i].Contains("leak") || cues[i].Contains("wrong");
                string path = "Assets/FluMix/Audio/" + cues[i] + ".wav";
                if (!File.Exists(path)) WriteWav(path, freq, seconds, 0.25f, noise);
                AssetDatabase.ImportAsset(path);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                _audio.cues.Add(new CueClip { id = cues[i], clip = clip });
            }
            EditorUtility.SetDirty(_audio);
            try
            {
                string mixerPath = "Assets/FluMix/Audio/FluMix.mixer";
                if (!File.Exists(mixerPath))
                {
                    var mixerType = Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");
                    var create = mixerType.GetMethod("CreateMixerControllerAtPath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    var created = create.Invoke(null, new object[] { mixerPath });
                    var createGroup = mixerType.GetMethod("CreateNewGroup", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var addChild = mixerType.GetMethod("AddChildToParent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var master = mixerType.GetProperty("masterGroup", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(created);
                    foreach (var groupName in new[] { "Music", "SFX", "UI", "Voice" })
                    {
                        var group = createGroup.Invoke(created, new object[] { groupName, false });
                        addChild.Invoke(created, new object[] { group, master });
                    }
                    EditorUtility.SetDirty((UnityEngine.Object)created);
                    AssetDatabase.SaveAssets();
                }
                _mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(mixerPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FluMix] Mixer create failed: " + e.Message);
            }
        }

        static void WriteWav(string path, float freq, float seconds, float volume, bool noise)
        {
            int rate = 22050;
            int count = Mathf.Max(1, (int)(rate * seconds));
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int dataSize = count * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataSize);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(rate);
                w.Write(rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                w.Write(dataSize);
                var rng = new System.Random(freq.GetHashCode());
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)rate;
                    float env = Mathf.Clamp01(t / 0.015f) * Mathf.Clamp01((seconds - t) / 0.08f);
                    float s = noise ? (float)(rng.NextDouble() * 2.0 - 1.0) : Mathf.Sin(t * freq * Mathf.PI * 2f);
                    if (freq > 600f) s += 0.4f * Mathf.Sin(t * freq * 2f * Mathf.PI * 2f);
                    short sample = (short)(Mathf.Clamp(s * env * volume, -1f, 1f) * short.MaxValue);
                    w.Write(sample);
                }
            }
        }

        static void BuildPrefabs()
        {
            SavePrefab(MakePillObject("UiPillButton"), "Assets/FluMix/Prefabs/UiPillButton.prefab");
            var frame = Ui(null, "UiNeonFrame", typeof(UnityEngine.UI.Image));
            var img = frame.GetComponent<Image>();
            img.sprite = _frame;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = new Color(0.35f, 0.9f, 1f, 1f);
            Place(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 240));
            SavePrefab(frame, "Assets/FluMix/Prefabs/UiNeonFrame.prefab");
            var hud = Ui(null, "HudCard", typeof(UnityEngine.UI.Image));
            hud.GetComponent<Image>().sprite = _pill;
            hud.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            hud.GetComponent<Image>().color = new Color(0.75f, 0.95f, 1f, 0.95f);
            Place(hud, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(420, 180));
            SavePrefab(hud, "Assets/FluMix/Prefabs/HudCard.prefab");
            var jet = new GameObject("WaterJet");
            var ps = jet.AddComponent<ParticleSystem>();
            ConfigureJet(ps, true);
            SavePrefab(jet, "Assets/FluMix/Prefabs/WaterJet.prefab");
            var beam = new GameObject("Beam");
            beam.AddComponent<Beam>();
            SavePrefab(beam, "Assets/FluMix/Prefabs/Beam.prefab");
            SaveGuardianPrefab(GuardianId.G, "Assets/FluMix/Prefabs/GuardianG.prefab");
            SaveGuardianPrefab(GuardianId.A, "Assets/FluMix/Prefabs/GuardianA.prefab");
            SaveGuardianPrefab(GuardianId.T, "Assets/FluMix/Prefabs/GuardianT.prefab");
            SaveVirusPrefab(_green, "Assets/FluMix/Prefabs/VirusGreen.prefab");
            SaveVirusPrefab(_blue, "Assets/FluMix/Prefabs/VirusBlue.prefab");
            SaveVirusPrefab(_pink, "Assets/FluMix/Prefabs/VirusPink.prefab");
        }

        static void SaveGuardianPrefab(GuardianId id, string path)
        {
            var go = new GameObject(id.ToString());
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _hero;
            sr.color = Color.white;
            var g = go.AddComponent<Guardian>();
            g.Id = id;
            SavePrefab(go, path);
        }

        static void SaveVirusPrefab(VirusArchetype archetype, string path)
        {
            var go = new GameObject(archetype.guardian.ToString());
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _virus;
            sr.color = archetype.color;
            go.AddComponent<Virus>();
            SavePrefab(go, path);
        }

        static void SavePrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
        }

        static void ConfigureJet(ParticleSystem ps, bool fromLeft)
        {
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.7f;
            main.startSpeed = 9f;
            main.startSize = 0.18f;
            main.startColor = new Color(0.85f, 0.95f, 1f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var emission = ps.emission;
            emission.rateOverTime = 80f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.05f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3f;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            ps.transform.rotation = Quaternion.Euler(0f, 0f, fromLeft ? -18f : 198f);
        }

        static void BuildBoot()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            StyleCamera(Camera.main, new Color(0.05f, 0.1f, 0.2f));
            MakeRoot();
            EditorSceneManager.SaveScene(scene, "Assets/FluMix/Scenes/Boot.unity");
        }

        static FluMixFlow MakeRoot()
        {
            var go = new GameObject("FluMixRoot");
            var flow = go.AddComponent<FluMixFlow>();
            flow.Strings = _strings;
            flow.Questions = _questions;
            flow.Rules = _rules;
            flow.Motion = _motion;
            flow.AudioBank = _audio;
            flow.Green = _green;
            flow.Blue = _blue;
            flow.Pink = _pink;
            flow.Mixer = _mixer;
            return flow;
        }

        static void StyleCamera(Camera cam, Color bg)
        {
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
        }

        static Canvas MakeCanvas(Camera cam)
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 6f;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
            return canvas;
        }

        static void BuildLogin()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            StyleCamera(cam, new Color(0.45f, 0.78f, 1f));
            MakeRoot();
            var canvas = MakeCanvas(cam);
            canvas.gameObject.AddComponent<LoginController>();
            FullBleed(canvas.transform, "Background", _loginBg);
            var hero = Image(canvas.transform, "Character", _hero, Color.white);
            Place(hero, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 20f), new Vector2(520f, 720f));
            var card = Image(canvas.transform, "Card", _frame, Color.white);
            card.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            card.GetComponent<Image>().color = new Color(0.93f, 0.98f, 1f, 0.94f);
            Place(card, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70f, 30f), new Vector2(640f, 620f));
            card.AddComponent<CanvasGroup>();
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.2f, 0.7f, 1f, 0.8f);
            shadow.effectDistance = new Vector2(0f, 0f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.85f, 1f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
            Title(card.transform, "ĐĂNG NHẬP", 54f, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(560f, 80f));
            InputField(card.transform, "Account", _strings.Get("login.account"), false, new Vector2(0f, 80f));
            InputField(card.transform, "Password", _strings.Get("login.password"), true, new Vector2(0f, -10f));
            var eye = Pill(card.transform, "Eye", "◉", new Color(0.2f, 0.75f, 1f), new Vector2(64f, 64f));
            Place(eye.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-36f, -10f), new Vector2(56f, 56f));
            var forgot = Tmp(card.transform, "Forgot", _strings.Get("login.forgot"), 22f, new Color(0.15f, 0.45f, 0.75f), TextAlignmentOptions.Right);
            Place(forgot.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, -70f), new Vector2(260f, 36f));
            forgot.raycastTarget = true;
            var forgotBtn = forgot.gameObject.AddComponent<Button>();
            forgotBtn.targetGraphic = forgot;
            var submit = Pill(card.transform, "Submit", _strings.Get("login.submit"), new Color(1f, 0.35f, 0.65f), new Vector2(280f, 72f));
            Place(submit.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(300f, 74f));
            var ribbon = Image(canvas.transform, "Ribbon", _ribbon, Color.white);
            ribbon.AddComponent<RibbonMotion>();
            Place(ribbon, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-390f, -310f), new Vector2(520f, 70f));
            var heart = Image(ribbon.transform, "Heart", _soft, new Color(0.2f, 0.85f, 1f));
            Place(heart, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f));
            var modal = Ui(canvas.transform, "ForgotModal", typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            Stretch(modal);
            modal.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.12f, 0.45f);
            var panel = Image(modal.transform, "Panel", _frame, new Color(0.95f, 0.98f, 1f, 0.98f));
            panel.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 280f));
            var body = Tmp(panel.transform, "Body", _strings.Get("login.forgot_body"), 26f, new Color(0.1f, 0.2f, 0.35f), TextAlignmentOptions.Center);
            Place(body.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(560f, 120f));
            var ok = Pill(panel.transform, "Ok", _strings.Get("login.forgot_ok"), new Color(0.2f, 0.75f, 1f), new Vector2(220f, 64f));
            Place(ok.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(220f, 64f));
            modal.SetActive(false);
            EditorSceneManager.SaveScene(scene, "Assets/FluMix/Scenes/Login.unity");
        }

        static void BuildHowTo()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            StyleCamera(cam, new Color(0.55f, 0.75f, 1f));
            MakeRoot();
            var canvas = MakeCanvas(cam);
            canvas.gameObject.AddComponent<HowToController>();
            FullBleed(canvas.transform, "Background", _hallBg);
            var hero = Image(canvas.transform, "Character", _hero, Color.white);
            Place(hero, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(340f, 480f));
            var panel = Image(canvas.transform, "Panel", _frame, new Color(0.9f, 0.97f, 1f, 0.94f));
            panel.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            panel.AddComponent<CanvasGroup>();
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, 40f), new Vector2(1500f, 820f));
            Title(panel.transform, "LUẬT CHƠI", 48f, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(700f, 80f));
            HowCard(panel.transform, "C1", _strings.Get("howto.s1"), _strings.Get("howto.s1b"), new Vector2(-480f, 180f), true);
            HowCard(panel.transform, "C2", _strings.Get("howto.s2"), _strings.Get("howto.s2b"), new Vector2(0f, 180f), false);
            HowCard(panel.transform, "C3", _strings.Get("howto.s3"), _strings.Get("howto.s3a") + "\n" + _strings.Get("howto.s3b") + "\n" + _strings.Get("howto.s3c"), new Vector2(480f, 180f), false);
            HowCard(panel.transform, "C4", _strings.Get("howto.s4"), _strings.Get("howto.s4a") + "\n" + _strings.Get("howto.s4b"), new Vector2(-480f, -120f), false);
            HowCard(panel.transform, "C5", _strings.Get("howto.s5"), _strings.Get("howto.s5a") + "\n" + _strings.Get("howto.s5b"), new Vector2(0f, -120f), false);
            HowCard(panel.transform, "C6", _strings.Get("howto.s6"), _strings.Get("howto.s6b") + "\n" + _strings.Get("howto.top1") + "\n" + _strings.Get("howto.top2") + "\n" + _strings.Get("howto.top3"), new Vector2(480f, -120f), false);
            var ready = Pill(canvas.transform, "Ready", _strings.Get("howto.ready"), new Color(1f, 0.35f, 0.65f), new Vector2(460f, 78f));
            Place(ready.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(80f, 18f), new Vector2(520f, 78f));
            EditorSceneManager.SaveScene(scene, "Assets/FluMix/Scenes/HowToPlay.unity");
        }

        static void HowCard(Transform parent, string name, string title, string body, Vector2 pos, bool viruses)
        {
            var card = Image(parent, name, _pill, new Color(1f, 1f, 1f, 0.72f));
            card.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            card.AddComponent<CanvasGroup>();
            Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(440f, 250f));
            var h = Tmp(card.transform, "H", title, 26f, new Color(0.1f, 0.45f, 0.85f), TextAlignmentOptions.Left);
            Place(h.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-24f, 40f));
            var b = Tmp(card.transform, "B", body, 20f, new Color(0.1f, 0.2f, 0.35f), TextAlignmentOptions.TopLeft);
            Place(b.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(-28f, -56f));
            if (!viruses) return;
            Dot(card.transform, new Color(0.3f, 0.95f, 0.35f), _strings.Get("howto.green"), -20f);
            Dot(card.transform, new Color(0.25f, 0.7f, 1f), _strings.Get("howto.blue"), -58f);
            Dot(card.transform, new Color(1f, 0.35f, 0.7f), _strings.Get("howto.pink"), -96f);
        }

        static void Dot(Transform parent, Color color, string label, float y)
        {
            var dot = Image(parent, "Dot", _virus, color);
            Place(dot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, y), new Vector2(28f, 28f));
            var text = Tmp(parent, "Vn", label, 18f, new Color(0.1f, 0.25f, 0.4f), TextAlignmentOptions.Left);
            Place(text.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(52f, y), new Vector2(-70f, 28f));
        }

        static void BuildLobby()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            StyleCamera(cam, new Color(0.35f, 0.55f, 0.9f));
            MakeRoot();
            var bg = new GameObject("Hall");
            var sr = bg.AddComponent<SpriteRenderer>();
            sr.sprite = _hallBg;
            sr.sortingOrder = -10;
            bg.transform.position = new Vector3(0f, 0f, 2f);
            bg.transform.localScale = new Vector3(1.9f, 1.9f, 1f);
            var left = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FluMix/Prefabs/WaterJet.prefab")) as GameObject;
            left.transform.position = new Vector3(-6.2f, 1.2f, 0f);
            left.transform.rotation = Quaternion.Euler(0f, 0f, -20f);
            var right = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FluMix/Prefabs/WaterJet.prefab")) as GameObject;
            right.transform.position = new Vector3(6.2f, 1.2f, 0f);
            right.transform.rotation = Quaternion.Euler(0f, 0f, 200f);
            var splashL = new GameObject("SplashL");
            splashL.transform.position = new Vector3(-1.5f, -2.2f, 0f);
            ConfigureSplash(splashL.AddComponent<ParticleSystem>());
            var splashR = new GameObject("SplashR");
            splashR.transform.position = new Vector3(1.5f, -2.2f, 0f);
            ConfigureSplash(splashR.AddComponent<ParticleSystem>());
            var canvas = MakeCanvas(cam);
            var lobby = canvas.gameObject.AddComponent<LobbyController>();
            lobby.LeftJet = left.GetComponent<ParticleSystem>();
            lobby.RightJet = right.GetComponent<ParticleSystem>();
            lobby.LeftSplash = splashL.GetComponent<ParticleSystem>();
            lobby.RightSplash = splashR.GetComponent<ParticleSystem>();
            left.GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            right.GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var hero = Image(canvas.transform, "Character", _hero, Color.white);
            Place(hero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(280f, 400f));
            var siren = Image(canvas.transform, "Siren", _soft, new Color(1f, 0.15f, 0.2f));
            Place(siren, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(90f, 70f));
            var rings = Ui(canvas.transform, "Rings");
            Place(rings, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(200f, 200f));
            for (int i = 0; i < 3; i++)
            {
                var ring = Image(rings.transform, "Ring" + i, _ring, new Color(1f, 0.2f, 0.25f, 0.8f));
                Place(ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 160f));
            }
            var sosGo = Ui(canvas.transform, "Sos", typeof(CanvasGroup));
            Place(sosGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(220f, 70f));
            var sosBg = Image(sosGo.transform, "Plate", _pill, new Color(0.9f, 0.1f, 0.15f));
            sosBg.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(sosBg);
            var sos = Tmp(sosGo.transform, "Label", "SOS", 42f, Color.white, TextAlignmentOptions.Center);
            Stretch(sos.gameObject);
            var howto = Pill(canvas.transform, "HowTo", _strings.Get("lobby.howto"), new Color(0.2f, 0.75f, 1f), new Vector2(240f, 70f));
            Place(howto.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-280f, 28f), new Vector2(250f, 70f));
            var fight = Pill(canvas.transform, "Fight", _strings.Get("lobby.fight"), new Color(1f, 0.35f, 0.65f), new Vector2(240f, 70f));
            Place(fight.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(250f, 70f));
            var board = Pill(canvas.transform, "Board", _strings.Get("lobby.board"), new Color(0.45f, 0.85f, 0.4f), new Vector2(280f, 70f));
            Place(board.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(290f, 28f), new Vector2(300f, 70f));
            var flash = Image(canvas.transform, "Flash", _soft, new Color(1f, 1f, 1f, 0f));
            Stretch(flash);
            flash.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            flash.SetActive(false);
            EditorSceneManager.SaveScene(scene, "Assets/FluMix/Scenes/Lobby.unity");
        }

        static void ConfigureSplash(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.4f;
            main.startSpeed = 2.5f;
            main.startSize = 0.2f;
            main.startColor = new Color(0.9f, 0.97f, 1f, 0.7f);
            main.maxParticles = 80;
            var emission = ps.emission;
            emission.rateOverTime = 30f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        static void BuildBattle()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            StyleCamera(cam, new Color(0.45f, 0.8f, 1f));
            MakeRoot();
            var bg = new GameObject("Landscape");
            var bsr = bg.AddComponent<SpriteRenderer>();
            bsr.sprite = _battleBg;
            bsr.sortingOrder = -20;
            bg.transform.position = new Vector3(0f, 0f, 4f);
            bg.transform.localScale = new Vector3(2.05f, 2.05f, 1f);
            var castle = new GameObject("Castle");
            var csr = castle.AddComponent<SpriteRenderer>();
            csr.sprite = _castle;
            csr.sortingOrder = 2;
            castle.transform.position = new Vector3(-6.1f, -0.4f, 0f);
            castle.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
            var root = new GameObject("Viruses").transform;
            var gate = new GameObject("Gate");
            gate.transform.position = new Vector3(-4.6f, -0.6f, 0f);
            Guardian G = PlaceGuardian(GuardianId.G, new Vector3(-6.4f, 1.15f, 0f));
            Guardian A = PlaceGuardian(GuardianId.A, new Vector3(-6.5f, -0.15f, 0f));
            Guardian T = PlaceGuardian(GuardianId.T, new Vector3(-6.3f, -1.45f, 0f));
            var canvas = MakeCanvas(cam);
            var battle = canvas.gameObject.AddComponent<BattleController>();
            battle.VirusRoot = root;
            battle.Gate = gate.transform;
            battle.Guardians = new[] { G, A, T };
            battle.VirusSprite = _virus;
            battle.RingSprite = _ring;
            var hud = canvas.gameObject.AddComponent<BattleHud>();
            battle.Hud = hud;
            var card = Image(canvas.transform, "HudCard", _pill, new Color(0.82f, 0.95f, 1f, 0.95f));
            card.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            Place(card, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -16f), new Vector2(430f, 170f));
            var ribbon = Image(card.transform, "Ribbon", _ribbon, Color.white);
            Place(ribbon, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(360f, 28f));
            var time = Tmp(card.transform, "Time", "02:00", 54f, Color.white, TextAlignmentOptions.Right);
            Place(time.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -8f), new Vector2(220f, 70f));
            var timeLabel = Tmp(card.transform, "TimeLabel", "TIME", 22f, new Color(0.15f, 0.35f, 0.6f), TextAlignmentOptions.Left);
            Place(timeLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -28f), new Vector2(120f, 36f));
            var score = Tmp(card.transform, "Score", "0", 42f, Color.white, TextAlignmentOptions.Right);
            Place(score.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -78f), new Vector2(160f, 56f));
            var scoreLabel = Tmp(card.transform, "ScoreLabel", "SCORE", 22f, new Color(0.15f, 0.35f, 0.6f), TextAlignmentOptions.Left);
            Place(scoreLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -92f), new Vector2(140f, 36f));
            hud.TimeLabel = time;
            hud.ScoreLabel = score;
            var controls = Ui(canvas.transform, "Controls");
            Place(controls, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-80f, 24f), new Vector2(520f, 80f));
            var g = Pill(controls.transform, "G", "G", new Color(0.3f, 0.9f, 0.35f), new Vector2(90f, 70f));
            Place(g.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(90f, 70f));
            var a = Pill(controls.transform, "A", "A", new Color(0.25f, 0.75f, 1f), new Vector2(90f, 70f));
            Place(a.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(90f, 70f));
            var tBtn = Pill(controls.transform, "T", "T", new Color(1f, 0.35f, 0.7f), new Vector2(90f, 70f));
            Place(tBtn.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(220f, 0f), new Vector2(90f, 70f));
            var ready = Pill(canvas.transform, "BattleReady", _strings.Get("battle.ready"), new Color(0.15f, 0.55f, 1f), new Vector2(240f, 70f));
            Place(ready.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 24f), new Vector2(240f, 70f));
            hud.Ready = ready;
            var warn = Image(canvas.transform, "Warning", _pill, new Color(0.9f, 0.1f, 0.15f, 0.95f));
            warn.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            Place(warn, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(520f, 80f));
            var warnText = Tmp(warn.transform, "Label", _strings.Get("warn.title"), 36f, Color.white, TextAlignmentOptions.Center);
            Stretch(warnText.gameObject);
            hud.Warning = warn.GetComponent<Image>();
            warn.SetActive(false);
            var result = Image(canvas.transform, "Result", _frame, new Color(0.95f, 0.98f, 1f, 0.98f));
            result.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            Place(result, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 420f));
            var rh = Tmp(result.transform, "Title", _strings.Get("result.title"), 48f, new Color(0.1f, 0.4f, 0.8f), TextAlignmentOptions.Center);
            Place(rh.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(500f, 70f));
            var rs = Tmp(result.transform, "Score", "", 36f, new Color(0.1f, 0.2f, 0.35f), TextAlignmentOptions.Center);
            Place(rs.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(500f, 50f));
            var rl = Tmp(result.transform, "Leaks", "", 28f, new Color(0.1f, 0.2f, 0.35f), TextAlignmentOptions.Center);
            Place(rl.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(500f, 40f));
            var rb = Pill(result.transform, "Board", _strings.Get("result.board"), new Color(1f, 0.35f, 0.65f), new Vector2(360f, 70f));
            Place(rb.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(360f, 70f));
            hud.Result = result;
            result.SetActive(false);
            var quiz = BuildQuiz(canvas.transform);
            var presenter = quiz.GetComponent<QuizPresenter>();
            presenter.Battle = battle;
            battle.Quiz = presenter;
            var quizPrefab = PrefabUtility.SaveAsPrefabAsset(quiz, "Assets/FluMix/Prefabs/QuizModal.prefab");
            var explain = quiz.transform.Find("Explain").gameObject;
            PrefabUtility.SaveAsPrefabAsset(explain, "Assets/FluMix/Prefabs/ExplainModal.prefab");
            quiz.SetActive(false);
            EditorSceneManager.SaveScene(scene, "Assets/FluMix/Scenes/Battle.unity");
        }

        static Guardian PlaceGuardian(GuardianId id, Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FluMix/Prefabs/Guardian" + id + ".prefab");
            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.55f;
            return go.GetComponent<Guardian>();
        }

        static GameObject BuildQuiz(Transform parent)
        {
            var root = Ui(parent, "Quiz");
            Stretch(root);
            var dim = Ui(root.transform, "Dim", typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            Stretch(dim);
            dim.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.16f, 0.45f);
            var panel = Image(root.transform, "Panel", _frame, new Color(0.93f, 0.98f, 1f, 0.97f));
            panel.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            var glow = panel.AddComponent<Outline>();
            glow.effectColor = new Color(0.2f, 0.85f, 1f, 1f);
            glow.effectDistance = new Vector2(4f, -4f);
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(980f, 640f));
            var tab = Tmp(panel.transform, "Tab", "CÂU 1", 28f, Color.white, TextAlignmentOptions.Center);
            var tabBg = tab.gameObject.AddComponent<LayoutElement>();
            Place(tab.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(180f, 48f));
            tab.color = new Color(0.05f, 0.25f, 0.55f);
            var prompt = Tmp(panel.transform, "Prompt", "", 30f, new Color(0.1f, 0.25f, 0.5f), TextAlignmentOptions.Center);
            Place(prompt.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(860f, 80f));
            float y = 40f;
            for (int i = 0; i < 4; i++)
            {
                var opt = Image(panel.transform, "Opt" + i, _pill, new Color(0.85f, 0.95f, 1f, 0.95f));
                opt.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
                opt.AddComponent<Button>();
                Place(opt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(820f, 72f));
                var label = Tmp(opt.transform, "Label", "", 28f, new Color(0.1f, 0.25f, 0.45f), TextAlignmentOptions.Left);
                Place(label.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(-40f, 60f));
                var sheen = Image(opt.transform, "Sheen", _soft, new Color(1f, 1f, 1f, 0f));
                Place(sheen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 70f));
                sheen.SetActive(false);
                y -= 86f;
            }
            var ready = Pill(root.transform, "Ready", _strings.Get("battle.ready"), new Color(0.15f, 0.45f, 0.95f), new Vector2(280f, 72f));
            Place(ready.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(300f, 72f));
            var explain = Image(root.transform, "Explain", _frame, new Color(0.95f, 0.99f, 1f, 0.98f));
            explain.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            Place(explain, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(900f, 520f));
            var head = Tmp(explain.transform, "Headline", "", 48f, new Color(0.35f, 0.85f, 0.25f), TextAlignmentOptions.Center);
            Place(head.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 80f));
            var affirm = Tmp(explain.transform, "Affirm", "", 28f, new Color(0.1f, 0.3f, 0.6f), TextAlignmentOptions.Center);
            Place(affirm.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(780f, 50f));
            var body = Tmp(explain.transform, "Body", "", 24f, new Color(0.1f, 0.2f, 0.35f), TextAlignmentOptions.Center);
            Place(body.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(760f, 140f));
            var cite = Tmp(explain.transform, "Cite", "", 18f, new Color(0.2f, 0.35f, 0.5f), TextAlignmentOptions.Left);
            Place(cite.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(760f, 40f));
            explain.SetActive(false);
            root.AddComponent<QuizPresenter>();
            return root;
        }

        static void BuildLeaderboard()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            StyleCamera(cam, new Color(0.5f, 0.7f, 1f));
            MakeRoot();
            var canvas = MakeCanvas(cam);
            canvas.gameObject.AddComponent<LeaderboardController>();
            FullBleed(canvas.transform, "Background", _hallBg);
            var hero = Image(canvas.transform, "Character", _hero, Color.white);
            Place(hero, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(320f, 460f));
            var panel = Image(canvas.transform, "Panel", _frame, new Color(0.9f, 0.97f, 1f, 0.94f));
            panel.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            panel.AddComponent<CanvasGroup>();
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90f, 30f), new Vector2(1280f, 760f));
            Title(panel.transform, "BẢNG XẾP HẠNG", 46f, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(900f, 80f));
            var sub = Tmp(panel.transform, "Sub", _strings.Get("board.sub"), 26f, new Color(0.15f, 0.4f, 0.75f), TextAlignmentOptions.Center);
            Place(sub.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(500f, 40f));
            var slots = Ui(panel.transform, "Slots");
            Place(slots, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1000f, 420f));
            Color[] medal = { new Color(1f, 0.82f, 0.2f), new Color(0.45f, 0.85f, 1f), new Color(0.75f, 0.78f, 0.82f), new Color(1f, 1f, 1f, 0.4f), new Color(1f, 1f, 1f, 0.4f) };
            Vector2[] pos = { new Vector2(-280f, -20f), new Vector2(0f, 30f), new Vector2(280f, -20f), new Vector2(-180f, -180f), new Vector2(180f, -180f) };
            Vector2[] size = { new Vector2(240f, 220f), new Vector2(280f, 260f), new Vector2(240f, 220f), new Vector2(300f, 80f), new Vector2(300f, 80f) };
            for (int i = 0; i < 5; i++)
            {
                var slot = Image(slots.transform, "Slot" + i, _pill, medal[i]);
                slot.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
                Place(slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos[i], size[i]);
                var label = Tmp(slot.transform, "Label", "", 24f, new Color(0.1f, 0.2f, 0.35f), TextAlignmentOptions.Center);
                Stretch(label.gameObject);
            }
            var prev = Pill(canvas.transform, "Prev", "<", new Color(0.2f, 0.7f, 1f), new Vector2(70f, 56f));
            Place(prev.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-40f, 110f), new Vector2(70f, 56f));
            var page = Tmp(canvas.transform, "Page", "1/4", 28f, new Color(0.1f, 0.3f, 0.55f), TextAlignmentOptions.Center);
            Place(page.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(90f, 110f), new Vector2(80f, 50f));
            var next = Pill(canvas.transform, "Next", ">", new Color(0.2f, 0.7f, 1f), new Vector2(70f, 56f));
            Place(next.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(210f, 110f), new Vector2(70f, 56f));
            var home = Pill(canvas.transform, "Home", "⌂", new Color(0.25f, 0.45f, 0.95f), new Vector2(120f, 64f));
            Place(home.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-80f, 24f), new Vector2(140f, 64f));
            var close = Pill(canvas.transform, "Close", "C", new Color(1f, 0.35f, 0.6f), new Vector2(120f, 64f));
            Place(close.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 24f), new Vector2(140f, 64f));
            EditorSceneManager.SaveScene(scene, "Assets/FluMix/Scenes/Leaderboard.unity");
        }

        static void Title(Transform parent, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var row = Ui(parent, "Title");
            row.AddComponent<TitleBob>();
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 1f;
            Place(row, anchor, anchor, new Vector2(0.5f, 1f), pos, box);
            foreach (char c in text)
            {
                if (c == ' ')
                {
                    var space = Ui(row.transform, "sp");
                    var spaceLayout = space.AddComponent<LayoutElement>();
                    spaceLayout.preferredWidth = size * 0.35f;
                    spaceLayout.preferredHeight = size;
                    continue;
                }
                var glyph = Tmp(row.transform, "g", c.ToString(), size, Color.white, TextAlignmentOptions.Center);
                glyph.fontStyle = FontStyles.Bold;
                var outline = glyph.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.15f, 0.55f, 1f, 0.9f);
                outline.effectDistance = new Vector2(2f, -2f);
                var glyphLayout = glyph.gameObject.AddComponent<LayoutElement>();
                glyphLayout.preferredWidth = size * 0.72f;
                glyphLayout.preferredHeight = size * 1.15f;
            }
        }

        static Button Pill(Transform parent, string name, string label, Color color, Vector2 size)
        {
            var go = MakePillObject(name);
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponentInChildren<TMP_Text>().text = label;
            Place(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return go.GetComponent<Button>();
        }

        static GameObject MakePillObject(string name)
        {
            var go = Ui(null, name, typeof(UnityEngine.UI.Image), typeof(Button), typeof(PillButton));
            var image = go.GetComponent<Image>();
            image.sprite = _pill != null ? _pill : null;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = new Color(1f, 0.35f, 0.65f);
            var glow = Image(go.transform, "Glow", _pill, new Color(1f, 1f, 1f, 0.35f));
            if (glow.GetComponent<Image>().sprite != null) glow.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(glow);
            var ring = Image(go.transform, "Ring", _ring, new Color(1f, 1f, 1f, 0f));
            Place(ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
            ring.SetActive(false);
            var text = Tmp(go.transform, "Label", name, 28f, Color.white, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            Stretch(text.gameObject);
            var pill = go.GetComponent<PillButton>();
            pill.Button = go.GetComponent<Button>();
            pill.Glow = glow.GetComponent<Image>();
            pill.Ring = ring.GetComponent<Image>();
            return go;
        }

        static void InputField(Transform parent, string name, string placeholder, bool password, Vector2 pos)
        {
            var go = Ui(parent, name, typeof(UnityEngine.UI.Image), typeof(TMP_InputField));
            Place(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(520f, 64f));
            var image = go.GetComponent<Image>();
            image.sprite = _pill;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white;
            var area = Ui(go.transform, "Text Area", typeof(RectMask2D));
            var art = area.GetComponent<RectTransform>();
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(16f, 8f);
            art.offsetMax = new Vector2(-16f, -8f);
            var ph = Tmp(area.transform, "Placeholder", placeholder, 26f, new Color(0.55f, 0.65f, 0.75f), TextAlignmentOptions.Left);
            ph.fontStyle = FontStyles.Italic;
            Stretch(ph.gameObject);
            var text = Tmp(area.transform, "Text", "", 26f, new Color(0.1f, 0.2f, 0.35f), TextAlignmentOptions.Left);
            Stretch(text.gameObject);
            var field = go.GetComponent<TMP_InputField>();
            field.textViewport = art;
            field.textComponent = text;
            field.placeholder = ph;
            field.lineType = TMP_InputField.LineType.SingleLine;
            if (password) field.contentType = TMP_InputField.ContentType.Password;
        }

        static void FullBleed(Transform parent, string name, Sprite sprite)
        {
            var go = Image(parent, name, sprite, Color.white);
            Stretch(go);
        }

        static GameObject Image(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = Ui(parent, name, typeof(UnityEngine.UI.Image));
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = sprite == _hero || sprite == _virus || sprite == _castle;
            return go;
        }

        static GameObject Ui(Transform parent, string name, params Type[] extras)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            for (int i = 0; i < extras.Length; i++) go.AddComponent(extras[i]);
            return go;
        }

        static TextMeshProUGUI Tmp(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = Ui(parent, name);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            if (_font != null) tmp.font = _font;
            return tmp;
        }

        static void Place(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Sprite SaveSprite(string path, Texture2D tex, float ppu, Vector4 border)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ppu;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Texture2D PaintCircle(int size, bool ring)
        {
            var tex = NewTex(size, size);
            float r = size * 0.5f - 1f;
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - r * 0.78f) * 0.45f) : Mathf.Clamp01((r - d) / 3f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return tex;
        }

        static Texture2D PaintRoundRect(int w, int h, int radius, bool fill)
        {
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = 0f;
                float dy = 0f;
                if (x < radius) dx = radius - x;
                else if (x > w - radius) dx = x - (w - radius);
                if (y < radius) dy = radius - y;
                else if (y > h - radius) dy = y - (h - radius);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                bool inside = d <= radius;
                if (!fill)
                {
                    float edge = radius - d;
                    float a = inside && (x < 10 || y < 10 || x > w - 11 || y > h - 11 || edge < 8f) ? 1f : 0f;
                    if (d > radius) a = 0f;
                    tex.SetPixel(x, y, new Color(0.4f, 0.9f, 1f, a));
                }
                else
                {
                    float a = d <= radius ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        static Texture2D PaintRibbon(int w, int h)
        {
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float k = x / (float)w;
                Color col = Color.HSVToRGB(k, 0.65f, 1f);
                float a = Mathf.Clamp01(1f - Mathf.Abs(y - h * 0.5f) / (h * 0.5f));
                col.a = a;
                tex.SetPixel(x, y, col);
            }
            tex.Apply();
            return tex;
        }

        static Texture2D PaintHero(int w, int h)
        {
            var tex = NewTex(w, h);
            Fill(tex, Color.clear);
            Ellipse(tex, w * 0.5f, h * 0.28f, w * 0.28f, h * 0.22f, new Color(0.2f, 0.85f, 1f, 0.0f));
            // cape
            for (int y = (int)(h * 0.15f); y < (int)(h * 0.62f); y++)
            {
                float k = (y - h * 0.15f) / (h * 0.47f);
                int half = (int)Mathf.Lerp(w * 0.34f, w * 0.12f, k);
                for (int x = w / 2 - half; x < w / 2 + half; x++)
                {
                    if (x < 0 || x >= w) continue;
                    tex.SetPixel(x, y, Color.HSVToRGB((x / (float)w), 0.7f, 1f));
                }
            }
            Ellipse(tex, w * 0.5f, h * 0.42f, w * 0.22f, h * 0.28f, Color.white);
            Ellipse(tex, w * 0.5f, h * 0.72f, w * 0.2f, h * 0.16f, Color.white);
            Ellipse(tex, w * 0.5f, h * 0.8f, w * 0.22f, h * 0.1f, new Color(1f, 0.35f, 0.65f));
            Ellipse(tex, w * 0.42f, h * 0.74f, w * 0.045f, h * 0.05f, new Color(0.1f, 0.15f, 0.25f));
            Ellipse(tex, w * 0.58f, h * 0.74f, w * 0.045f, h * 0.05f, new Color(0.1f, 0.15f, 0.25f));
            Ellipse(tex, w * 0.72f, h * 0.4f, w * 0.08f, h * 0.06f, new Color(0.2f, 0.7f, 1f));
            tex.Apply();
            return tex;
        }

        static Texture2D PaintCastle(int w, int h)
        {
            var tex = NewTex(w, h);
            Fill(tex, Color.clear);
            var wall = new Color(0.35f, 0.75f, 1f);
            Rect(tex, (int)(w * 0.18f), (int)(h * 0.08f), (int)(w * 0.64f), (int)(h * 0.55f), wall);
            Rect(tex, (int)(w * 0.08f), (int)(h * 0.28f), (int)(w * 0.22f), (int)(h * 0.42f), new Color(0.25f, 0.65f, 1f));
            Rect(tex, (int)(w * 0.7f), (int)(h * 0.28f), (int)(w * 0.22f), (int)(h * 0.48f), new Color(0.45f, 0.82f, 1f));
            for (int i = 0; i < 5; i++)
                Rect(tex, (int)(w * 0.16f + i * w * 0.13f), (int)(h * 0.6f), (int)(w * 0.08f), (int)(h * 0.1f), Color.white);
            Ellipse(tex, w * 0.5f, h * 0.78f, w * 0.08f, h * 0.08f, new Color(1f, 0.4f, 0.7f));
            tex.Apply();
            return tex;
        }

        static Texture2D PaintVirus(int size)
        {
            var tex = NewTex(size, size);
            float r = size * 0.32f;
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                Ellipse(tex, c.x + Mathf.Cos(a) * r * 1.15f, c.y + Mathf.Sin(a) * r * 1.15f, size * 0.08f, size * 0.08f, Color.white);
            }
            Ellipse(tex, c.x, c.y, r, r, Color.white);
            Ellipse(tex, c.x - r * 0.28f, c.y + r * 0.15f, r * 0.16f, r * 0.2f, new Color(0.05f, 0.08f, 0.12f));
            Ellipse(tex, c.x + r * 0.28f, c.y + r * 0.15f, r * 0.16f, r * 0.2f, new Color(0.05f, 0.08f, 0.12f));
            tex.Apply();
            return tex;
        }

        static Texture2D PaintLoginBg(int w, int h)
        {
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            {
                float k = y / (float)h;
                Color sky = Color.Lerp(new Color(0.35f, 0.7f, 1f), new Color(0.65f, 0.88f, 1f), k);
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, sky);
            }
            Ellipse(tex, w * 0.2f, h * 0.72f, w * 0.12f, h * 0.05f, new Color(1f, 1f, 1f, 0.85f));
            Ellipse(tex, w * 0.55f, h * 0.8f, w * 0.16f, h * 0.05f, new Color(1f, 1f, 1f, 0.8f));
            Ellipse(tex, w * 0.35f, h * 0.22f, w * 0.5f, h * 0.18f, new Color(0.35f, 0.75f, 0.4f));
            Ellipse(tex, w * 0.75f, h * 0.18f, w * 0.4f, h * 0.16f, new Color(0.4f, 0.8f, 0.45f));
            Rect(tex, (int)(w * 0.08f), (int)(h * 0.28f), (int)(w * 0.16f), (int)(h * 0.28f), new Color(0.95f, 0.9f, 0.8f));
            Rect(tex, (int)(w * 0.18f), (int)(h * 0.36f), (int)(w * 0.08f), (int)(h * 0.22f), new Color(0.85f, 0.75f, 0.9f));
            Ellipse(tex, w * 0.62f, h * 0.16f, w * 0.28f, h * 0.05f, new Color(0.25f, 0.65f, 0.95f, 0.9f));
            tex.Apply();
            return tex;
        }

        static Texture2D PaintHall(int w, int h)
        {
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            {
                Color col = Color.Lerp(new Color(0.15f, 0.2f, 0.45f), new Color(0.55f, 0.75f, 1f), y / (float)h);
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, col);
            }
            for (int i = 0; i < 5; i++)
            {
                float cx = w * (0.12f + i * 0.19f);
                Ellipse(tex, cx, h * 0.62f, w * 0.07f, h * 0.22f, new Color(0.6f, 0.85f, 1f, 0.55f));
            }
            Rect(tex, 0, 0, w, (int)(h * 0.28f), new Color(0.45f, 0.7f, 0.95f, 0.85f));
            for (int y = 0; y < h * 0.28f; y++)
            for (int x = 0; x < w; x += 40)
                if ((x / 40 + y / 20) % 2 == 0) tex.SetPixel(Mathf.Min(x, w - 1), y, new Color(0.7f, 0.9f, 1f, 0.35f));
            tex.Apply();
            return tex;
        }

        static Texture2D PaintBattleBg(int w, int h)
        {
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            {
                Color sky = Color.Lerp(new Color(0.4f, 0.75f, 1f), new Color(0.7f, 0.9f, 1f), y / (float)h);
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, sky);
            }
            Ellipse(tex, w * 0.3f, h * 0.78f, w * 0.1f, h * 0.04f, new Color(1f, 1f, 1f, 0.9f));
            Ellipse(tex, w * 0.6f, h * 0.84f, w * 0.14f, h * 0.04f, new Color(1f, 1f, 1f, 0.85f));
            Ellipse(tex, w * 0.2f, h * 0.42f, w * 0.35f, h * 0.16f, new Color(0.45f, 0.8f, 0.4f));
            Ellipse(tex, w * 0.55f, h * 0.38f, w * 0.4f, h * 0.14f, new Color(0.4f, 0.75f, 0.38f));
            Ellipse(tex, w * 0.85f, h * 0.4f, w * 0.3f, h * 0.12f, new Color(0.5f, 0.82f, 0.42f));
            Rect(tex, 0, (int)(h * 0.18f), w, (int)(h * 0.12f), new Color(0.72f, 0.55f, 0.32f));
            Rect(tex, 0, 0, w, (int)(h * 0.2f), new Color(0.35f, 0.75f, 0.35f));
            tex.Apply();
            return tex;
        }

        static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        static void Fill(Texture2D tex, Color color)
        {
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
        }

        static void Rect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
            {
                if (xx < 0 || yy < 0 || xx >= tex.width || yy >= tex.height) continue;
                tex.SetPixel(xx, yy, color.a >= 0.99f ? color : Color.Lerp(tex.GetPixel(xx, yy), color, color.a));
            }
        }

        static void Ellipse(Texture2D tex, float cx, float cy, float rx, float ry, Color color)
        {
            int x0 = Mathf.Max(0, (int)(cx - rx));
            int x1 = Mathf.Min(tex.width - 1, (int)(cx + rx));
            int y0 = Mathf.Max(0, (int)(cy - ry));
            int y1 = Mathf.Min(tex.height - 1, (int)(cy + ry));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float nx = (x - cx) / rx;
                float ny = (y - cy) / ry;
                if (nx * nx + ny * ny <= 1f)
                {
                    var dst = color.a >= 0.99f ? color : Color.Lerp(tex.GetPixel(x, y), new Color(color.r, color.g, color.b, 1f), color.a);
                    tex.SetPixel(x, y, dst);
                }
            }
        }
    }
}
