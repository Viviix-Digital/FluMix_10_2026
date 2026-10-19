using UnityEngine;
using UnityEngine.Audio;

namespace FluMix
{
    public class FluMixSfx : MonoBehaviour
    {
        public static FluMixSfx Instance { get; private set; }

        [SerializeField] AudioMixer mixer;
        [SerializeField] AudioClip musicMenu;
        [SerializeField] AudioClip musicBattle;
        [SerializeField] AudioClip beamBlue;
        [SerializeField] AudioClip beamGreen;
        [SerializeField] AudioClip beamPink;
        [SerializeField] AudioClip impactCorrect;
        [SerializeField] AudioClip impactWrong;
        [SerializeField] AudioClip uiConfirm;
        [SerializeField] AudioClip uiDeny;
        [SerializeField] AudioClip uiPage;
        [SerializeField] AudioClip uiModalOpen;
        [SerializeField] AudioClip uiModalClose;
        [SerializeField] AudioClip inputKey;
        [SerializeField] AudioClip inputPassword;
        [SerializeField] AudioClip quizSelect;
        [SerializeField] AudioClip quizCorrect;
        [SerializeField] AudioClip quizWrong;
        [SerializeField] AudioClip quizExplain;
        [SerializeField] AudioClip quizUnlock;
        [SerializeField] AudioClip warningAlarm;
        [SerializeField] AudioClip stingerSos;
        [SerializeField] AudioClip timerTick;
        [SerializeField] AudioClip timerEnd;
        [SerializeField] AudioClip virusSpawn;
        [SerializeField] AudioClip virusEvolve;
        [SerializeField] AudioClip virusLeak;
        [SerializeField] AudioClip podiumLand;

        AudioSource music;
        AudioSource voice;
        bool battleMusic;

        void Awake()
        {
            Instance = this;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            Route(music, "Music");
            Route(voice, "SFX");
        }

        public static void Play(string id)
        {
            if (Instance != null)
                Instance.PlayCue(id);
        }

        public static void SetBattleMusic(bool battle)
        {
            if (Instance != null)
                Instance.PlayMusic(battle);
        }

        public void PlayCue(string id)
        {
            AudioClip clip = Clip(id);
            if (clip == null || voice == null)
                return;
            voice.PlayOneShot(clip);
        }

        public void PlayMusic(bool battle)
        {
            if (music == null)
                return;
            if (battle == battleMusic && music.isPlaying)
                return;
            battleMusic = battle;
            AudioClip clip = battle ? musicBattle : musicMenu;
            if (clip == null)
                return;
            music.clip = clip;
            music.Play();
        }

        void Route(AudioSource source, string groupName)
        {
            if (mixer == null || source == null)
                return;
            AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
            if (groups != null && groups.Length > 0)
                source.outputAudioMixerGroup = groups[0];
        }

        AudioClip Clip(string id)
        {
            switch (id)
            {
                case "beam_blue": return beamBlue;
                case "beam_green": return beamGreen;
                case "beam_pink": return beamPink;
                case "impact_correct": return impactCorrect;
                case "impact_wrong": return impactWrong;
                case "ui_confirm": return uiConfirm;
                case "ui_deny": return uiDeny;
                case "ui_page": return uiPage;
                case "ui_modal_open": return uiModalOpen;
                case "ui_modal_close": return uiModalClose;
                case "input_key": return inputKey;
                case "input_password": return inputPassword;
                case "quiz_select": return quizSelect;
                case "quiz_correct": return quizCorrect;
                case "quiz_wrong": return quizWrong;
                case "quiz_explain": return quizExplain;
                case "quiz_unlock": return quizUnlock;
                case "warning_alarm": return warningAlarm;
                case "stinger_sos": return stingerSos;
                case "timer_tick": return timerTick;
                case "timer_end": return timerEnd;
                case "virus_spawn": return virusSpawn;
                case "virus_evolve": return virusEvolve;
                case "virus_leak": return virusLeak;
                case "podium_land": return podiumLand;
                default: return null;
            }
        }
    }
}
