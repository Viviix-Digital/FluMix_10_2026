using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace FluMix
{
    public enum FlowState
    {
        Boot,
        Login,
        Lobby,
        HowToPlay,
        Battle,
        Result,
        Leaderboard,
        WaterIntro
    }

    public enum GuardianId
    {
        G = 0,
        A = 1,
        T = 2
    }

    public static class FluMixLog
    {
        public static readonly List<string> Tail = new List<string>();
        public static event Action<string> LineAdded;

        public static void Info(string category, string message) => Write("I", category, message);
        public static void Warn(string category, string message) => Write("W", category, message);

        static void Write(string level, string category, string message)
        {
            string line = level + " [" + category + "] " + message;
            Tail.Add(line);
            while (Tail.Count > 8) Tail.RemoveAt(0);
            Debug.Log("[FluMix] " + line);
            LineAdded?.Invoke(line);
        }
    }

    public static class GameSession
    {
        public static string Account = "";
        public static int Score;
        public static int Leaks;
        public static float TimeLeft = 120f;
        public static int QuestionIndex;
        public static int ResolvedQuestions;
        public static int SelectedGuardian = -1;
        public static int SelectedOption = -1;
        public static string LastCue = "";
        public static int LastScoreDelta;
        public static FlowState Previous = FlowState.Lobby;
        public static bool RoundActive;
        public static int SpawnCount;
        public static bool SuppressNextWarning;
        public static float SuppressUntil;
        public static int NextWarningIndex;

        public static void ResetRound(float seconds)
        {
            Score = 0;
            Leaks = 0;
            TimeLeft = seconds;
            QuestionIndex = 0;
            ResolvedQuestions = 0;
            SelectedGuardian = -1;
            SelectedOption = -1;
            LastScoreDelta = 0;
            RoundActive = true;
            SpawnCount = 0;
            SuppressNextWarning = false;
            SuppressUntil = 0f;
            NextWarningIndex = 0;
        }

        public static string FormatScore(int value)
        {
            return value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
        }
    }

    [Serializable]
    public class StringEntry
    {
        public string id;
        public string text;
    }

    [CreateAssetMenu(fileName = "StringTable", menuName = "FluMix/String Table")]
    public class StringTable : ScriptableObject
    {
        public List<StringEntry> entries = new List<StringEntry>();

        public string Get(string id)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].id == id) return entries[i].text;
            }
            return id;
        }

        public void Set(string id, string text)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].id == id)
                {
                    entries[i].text = text;
                    return;
                }
            }
            entries.Add(new StringEntry { id = id, text = text });
        }
    }

    [Serializable]
    public class QuestionDef
    {
        public string prompt;
        public string[] options = new string[4];
        public int correctIndex;
        public string affirmation;
        public string body;
        public string citation;
    }

    [CreateAssetMenu(fileName = "QuestionBank", menuName = "FluMix/Question Bank")]
    public class QuestionBank : ScriptableObject
    {
        public List<QuestionDef> questions = new List<QuestionDef>();

        public QuestionDef Get(int index)
        {
            if (questions.Count == 0) return null;
            int i = Mathf.Clamp(index, 0, questions.Count - 1);
            return questions[i];
        }
    }

    [CreateAssetMenu(fileName = "VirusArchetype", menuName = "FluMix/Virus Archetype")]
    public class VirusArchetype : ScriptableObject
    {
        public GuardianId guardian;
        public Color color = Color.green;
        public string displayNameId;
        public float baseSpeed = 1.4f;
    }

    [CreateAssetMenu(fileName = "RoundRules", menuName = "FluMix/Round Rules")]
    public class RoundRules : ScriptableObject
    {
        public float spawnIntervalStart = 2.2f;
        public float spawnIntervalMin = 0.9f;
        public float spawnIntervalStep = 0.1f;
        public int quizEveryNthSpawn = 4;
        public float roundSeconds = 120f;
        public int maxQuestions = 10;
        public int scoreHit = 100;
        public int scoreMiss = -50;
        public int scoreQuiz = 200;
        public int scoreLeak = -50;
        public int[] warningThresholds = { 1000, 2000, 5000, 10000 };
        public float warningDelaySeconds = 20f;
        public float explainAutoContinue = 2.5f;
        public float tier1Speed = 1.25f;
        public float tier2Speed = 1.5f;
        public float beamDuration = 0.35f;
    }

    [CreateAssetMenu(fileName = "UiMotionConfig", menuName = "FluMix/UI Motion")]
    public class UiMotionConfig : ScriptableObject
    {
        public float enterDuration = 0.28f;
        public float enterSlide = 24f;
        public float exitDuration = 0.18f;
        public float exitSlide = 16f;
        public float modalInDuration = 0.35f;
        public float modalOutDuration = 0.2f;
        public float modalDim = 0.45f;
        public float modalStartScale = 0.86f;
        public float titleStagger = 0.04f;
        public float titleBob = 3f;
        public float titleBobPeriod = 2.4f;
        public float pillPulse = 1.1f;
        public float hoverScale = 1.05f;
        public float hoverDuration = 0.12f;
        public float pressScale = 0.96f;
        public float pressDuration = 0.06f;
        public float clickRing = 0.25f;
        public float ribbonSway = 4f;
        public float ribbonPeriod = 3f;
        public float listStagger = 0.05f;
        public float scorePunch = 0.22f;
        public float wrongShake = 0.28f;
        public bool reducedMotion;
    }

    [Serializable]
    public class CueClip
    {
        public string id;
        public AudioClip clip;
    }

    [CreateAssetMenu(fileName = "AudioBank", menuName = "FluMix/Audio Bank")]
    public class AudioBank : ScriptableObject
    {
        public List<CueClip> cues = new List<CueClip>();

        public AudioClip Get(string id)
        {
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].id == id) return cues[i].clip;
            }
            return null;
        }
    }

    [Serializable]
    public class BoardEntry
    {
        public string name;
        public int score;
    }

    [Serializable]
    public class BoardFile
    {
        public List<BoardEntry> entries = new List<BoardEntry>();
    }

    public static class LeaderboardStore
    {
        const string FileName = "flumix-leaderboard.json";

        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static BoardFile Load()
        {
            try
            {
                if (File.Exists(Path))
                {
                    var file = JsonUtility.FromJson<BoardFile>(File.ReadAllText(Path));
                    if (file != null && file.entries != null && file.entries.Count > 0) return file;
                }
            }
            catch (Exception e)
            {
                FluMixLog.Warn("Score", "Leaderboard load failed: " + e.Message);
            }
            return Seed();
        }

        public static BoardFile Seed()
        {
            var file = new BoardFile();
            file.entries.Add(new BoardEntry { name = "NGƯỜI CHƠI 1", score = 10000 });
            file.entries.Add(new BoardEntry { name = "NGƯỜI CHƠI 2", score = 9500 });
            file.entries.Add(new BoardEntry { name = "NGƯỜI CHƠI 3", score = 9000 });
            Save(file);
            return file;
        }

        public static void Save(BoardFile file)
        {
            File.WriteAllText(Path, JsonUtility.ToJson(file));
        }

        public static BoardFile Submit(string name, int score)
        {
            var file = Load();
            if (string.IsNullOrEmpty(name)) name = "NGƯỜI CHƠI";
            file.entries.Add(new BoardEntry { name = name, score = score });
            file.entries.Sort((a, b) => b.score.CompareTo(a.score));
            if (file.entries.Count > 20) file.entries.RemoveRange(20, file.entries.Count - 20);
            Save(file);
            FluMixLog.Info("Score", "Persisted score " + score + " for " + name);
            return file;
        }
    }

    public static class FluMixContent
    {
        public static void ApplyStrings(StringTable table)
        {
            table.Set("login.title", "ĐĂNG NHẬP");
            table.Set("login.account", "Tài khoản");
            table.Set("login.password", "Mật khẩu");
            table.Set("login.forgot", "Quên mật khẩu?");
            table.Set("login.submit", "ĐĂNG NHẬP");
            table.Set("login.forgot_body", "Nếu bạn quên mật khẩu, hãy liên hệ giáo viên hoặc quản trị viên để được cấp lại.");
            table.Set("login.forgot_ok", "ĐÃ HIỂU");
            table.Set("howto.title", "LUẬT CHƠI");
            table.Set("howto.s1", "01. NHIỆM VỤ");
            table.Set("howto.s1b", "Ngăn virus tiến vào thành bằng cách chọn đúng chiến binh.");
            table.Set("howto.green", "Virus xanh lá");
            table.Set("howto.blue", "Virus xanh dương");
            table.Set("howto.pink", "Virus hồng");
            table.Set("howto.s2", "02. CÁCH CHƠI");
            table.Set("howto.s2b", "Quan sát virus xuất hiện → Chọn G, A hoặc T đúng màu → Bắn chặn trước khi virus vào thành.");
            table.Set("howto.s3", "03. TÍNH ĐIỂM");
            table.Set("howto.s3a", "Chọn đúng: +100 điểm");
            table.Set("howto.s3b", "Chọn sai: −50 điểm");
            table.Set("howto.s3c", "Trả lời câu hỏi: +200 điểm");
            table.Set("howto.s4", "04. CẢNH BÁO");
            table.Set("howto.s4a", "Đạt mốc 1.000 / 2.000 / 5.000 / 10.000 điểm xuất hiện cảnh báo dịch cúm.");
            table.Set("howto.s4b", "Trả lời 1 câu hỏi đúng ngăn cảnh báo xuất hiện sớm trong 20 giây hoặc đến mốc kế tiếp.");
            table.Set("howto.s5", "05. THỜI GIAN");
            table.Set("howto.s5a", "Mỗi lượt tối đa 2 phút.");
            table.Set("howto.s5b", "Trò chơi kết thúc khi hết 2 phút hoặc hoàn thành 10 câu hỏi, tùy điều kiện nào đến trước.");
            table.Set("howto.s6", "06. XẾP HẠNG");
            table.Set("howto.s6b", "Tích điểm để vào TOP 20");
            table.Set("howto.top1", "Top 1: KIM CƯƠNG");
            table.Set("howto.top2", "Top 2: VÀNG");
            table.Set("howto.top3", "Top 3: BẠC");
            table.Set("howto.ready", "SẴN SÀNG CHIẾN ĐẤU");
            table.Set("lobby.howto", "Luật chơi");
            table.Set("lobby.fight", "Chiến đấu");
            table.Set("lobby.board", "Bảng xếp hạng");
            table.Set("lobby.sos", "SOS");
            table.Set("hud.time", "TIME");
            table.Set("hud.score", "SCORE");
            table.Set("battle.ready", "SẴN SÀNG");
            table.Set("quiz.tab", "CÂU {0}");
            table.Set("warn.title", "CẢNH BÁO DỊCH");
            table.Set("explain.correct", "Bạn trả lời đúng!");
            table.Set("explain.wrong", "Chưa đúng!");
            table.Set("result.title", "KẾT QUẢ");
            table.Set("result.score", "Điểm: {0}");
            table.Set("result.leaks", "Virus lọt: {0}");
            table.Set("result.board", "BẢNG XẾP HẠNG");
            table.Set("board.title", "BẢNG XẾP HẠNG");
            table.Set("board.sub", "TOP 10 CHIẾN BINH");
            table.Set("board.points", "{0} điểm");
            table.Set("board.page", "{0}/4");
            table.Set("common.close", "ĐÓNG");
        }

        public static void ApplyQuestions(QuestionBank bank)
        {
            bank.questions.Clear();
            bank.questions.Add(Q(
                "Cúm mùa là bệnh do nguyên nhân nào gây ra?",
                new[] { "Vi khuẩn", "Virus cúm", "Thay đổi thời tiết", "Dị ứng" },
                1,
                "Cúm mùa do virus cúm gây ra",
                "Không phải do thời tiết lạnh hay thay đổi nhiệt độ. Virus cúm A và B là các tác nhân gây cúm mùa ở người.",
                "Tài liệu tham khảo: Krammer F, et al. Nat Rev Dis Primers. 2018;4:3"));
            bank.questions.Add(Q(
                "Virus cúm mùa ở người chủ yếu thuộc nhóm nào?",
                new[] { "Chỉ virus cúm C", "Virus cúm A và B", "Vi khuẩn tụ cầu", "Nấm men" },
                1,
                "Chủ yếu là virus cúm A và B",
                "Virus cúm A và B gây cúm mùa ở người. Virus cúm C thường nhẹ hơn. Vi khuẩn và nấm không gây bệnh cúm mùa.",
                ""));
            bank.questions.Add(Q(
                "Cúm mùa lây chủ yếu bằng cách nào?",
                new[] { "Muỗi đốt", "Giọt bắn khi ho, hắt hơi", "Ăn phải thức ăn ôi", "Tắm nước lạnh" },
                1,
                "Cúm mùa lây qua giọt bắn hô hấp",
                "Virus theo giọt bắn khi người bệnh ho, hắt hơi hoặc nói chuyện gần. Muỗi và thức ăn ôi không phải đường lây chính.",
                ""));
            bank.questions.Add(Q(
                "Triệu chứng nào thường gặp khi mắc cúm mùa?",
                new[] { "Sốt, đau họng, đau mình", "Chỉ ngứa da", "Chỉ đau răng", "Chỉ sưng khớp" },
                0,
                "Cúm mùa thường có sốt, đau họng và đau mình",
                "Bệnh thường khởi phát đột ngột với sốt, đau đầu, đau cơ và đau họng. Ngứa da đơn độc không phải dấu hiệu điển hình.",
                ""));
            bank.questions.Add(Q(
                "Cách nào giúp hạn chế lây cúm mùa?",
                new[] { "Che miệng khi ho và rửa tay", "Dùng chung khăn mặt", "Đến nơi đông người khi đang sốt", "Tập luyện dưới mưa" },
                0,
                "Che miệng khi ho và rửa tay giúp hạn chế lây",
                "Rửa tay, che miệng khi ho và ở nhà khi sốt làm giảm phát tán virus. Dùng chung khăn và đến chỗ đông người làm tăng lây lan.",
                ""));
            bank.questions.Add(Q(
                "Kháng sinh có chữa khỏi cúm mùa do virus không?",
                new[] { "Có, vì kháng sinh diệt virus", "Không, kháng sinh không diệt virus", "Chỉ khi trời lạnh", "Chỉ với người lớn" },
                1,
                "Kháng sinh không diệt virus cúm",
                "Cúm mùa do virus. Kháng sinh chỉ dùng khi thầy thuốc xác định có bội nhiễm vi khuẩn, không phải thuốc trị virus cúm.",
                ""));
            bank.questions.Add(Q(
                "Cúm mùa khác cảm lạnh ở điểm nào?",
                new[] { "Cúm thường sốt cao và khởi phát đột ngột", "Hai bệnh do cùng một vi khuẩn", "Cảm lạnh luôn sốt rất cao", "Cúm không lây sang người khác" },
                0,
                "Cúm thường sốt cao hơn và đến đột ngột",
                "Cảm lạnh thường nhẹ, diễn tiến từ từ. Cúm mùa hay bắt đầu đột ngột với sốt cao và mệt nhiều. Cả hai đều có thể lây.",
                ""));
            bank.questions.Add(Q(
                "Ai nên tiêm vắc xin cúm hằng năm?",
                new[] { "Không ai cần tiêm", "Người có nguy cơ cao và người chăm sóc họ", "Chỉ vận động viên", "Chỉ trẻ dưới một tháng tuổi" },
                1,
                "Người có nguy cơ cao và người chăm sóc nên tiêm hằng năm",
                "Vắc xin cúm được nhắc lại mỗi năm vì virus thay đổi. Người bệnh mạn tính, người lớn tuổi và người chăm sóc họ thuộc nhóm nên tiêm.",
                ""));
            bank.questions.Add(Q(
                "Khi bị cúm mùa, việc nên làm trước là gì?",
                new[] { "Nghỉ ngơi, uống đủ nước, hạn chế tiếp xúc", "Đến chỗ đông người", "Dùng chung đồ ăn", "Tập nặng ngay" },
                0,
                "Nên nghỉ, uống đủ nước và hạn chế tiếp xúc",
                "Nghỉ ngơi và uống đủ nước giúp cơ thể hồi phục. Hạn chế tiếp xúc để bớt lây cho người xung quanh.",
                ""));
            bank.questions.Add(Q(
                "Dấu hiệu nào cần đi khám sớm khi nghi cúm?",
                new[] { "Khó thở, sốt cao kéo dài, lừ đừ", "Hắt hơi một cái rồi khỏi", "Ngủ thêm một giấc", "Chỉ khát nước nhẹ" },
                0,
                "Khó thở, sốt cao kéo dài hoặc lừ đừ cần đi khám",
                "Khó thở, sốt cao không hạ, li bì hoặc tím môi là dấu hiệu cần được khám sớm, nhất là với trẻ nhỏ và người có bệnh nền.",
                ""));
        }

        static QuestionDef Q(string prompt, string[] options, int correct, string affirmation, string body, string citation)
        {
            return new QuestionDef
            {
                prompt = prompt,
                options = options,
                correctIndex = correct,
                affirmation = affirmation,
                body = body,
                citation = citation
            };
        }

        public static void ApplyRules(RoundRules rules)
        {
            rules.spawnIntervalStart = 2.2f;
            rules.spawnIntervalMin = 0.9f;
            rules.spawnIntervalStep = 0.1f;
            rules.quizEveryNthSpawn = 4;
            rules.roundSeconds = 120f;
            rules.maxQuestions = 10;
            rules.scoreHit = 100;
            rules.scoreMiss = -50;
            rules.scoreQuiz = 200;
            rules.scoreLeak = -50;
            rules.warningThresholds = new[] { 1000, 2000, 5000, 10000 };
            rules.warningDelaySeconds = 20f;
            rules.explainAutoContinue = 2.5f;
            rules.tier1Speed = 1.25f;
            rules.tier2Speed = 1.5f;
            rules.beamDuration = 0.35f;
        }
    }
}
