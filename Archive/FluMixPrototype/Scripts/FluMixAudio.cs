using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace FluMix
{
    [DefaultExecutionOrder(-150)]
    public class FluMixAudio : MonoBehaviour
    {
        public static FluMixAudio Instance { get; private set; }

        public AudioMixer Mixer;
        public AudioBank Bank;

        AudioSource _musicA;
        AudioSource _musicB;
        AudioSource _ui;
        AudioSource _water;
        readonly List<AudioSource> _pool = new List<AudioSource>();
        bool _usingA = true;
        Coroutine _fade;

        public static void Ensure(FluMixFlow flow)
        {
            var audio = flow.GetComponent<FluMixAudio>();
            if (audio == null) audio = flow.gameObject.AddComponent<FluMixAudio>();
            audio.Bank = flow.AudioBank;
            audio.Mixer = flow.Mixer;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _musicA = CreateSource("MusicA", true);
            _musicB = CreateSource("MusicB", true);
            _ui = CreateSource("UI", false);
            _ui.ignoreListenerPause = true;
            _water = CreateSource("Water", true);
            for (int i = 0; i < 8; i++) _pool.Add(CreateSource("World" + i, false));
            Route(_musicA, "Music");
            Route(_musicB, "Music");
            Route(_ui, "UI");
            Route(_water, "SFX");
            for (int i = 0; i < _pool.Count; i++) Route(_pool[i], "SFX");
        }

        void Route(AudioSource src, string group)
        {
            if (Mixer == null || src == null) return;
            var groups = Mixer.FindMatchingGroups(group);
            if (groups != null && groups.Length > 0) src.outputAudioMixerGroup = groups[0];
        }

        AudioSource CreateSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            return src;
        }

        public void OnFlow(FlowState state)
        {
            if (state == FlowState.Battle) PlayMusic("music_battle");
            else if (state == FlowState.WaterIntro) Play("stinger_sos");
            else if (state == FlowState.Login || state == FlowState.Lobby || state == FlowState.HowToPlay || state == FlowState.Leaderboard)
                PlayMusic("music_menu");
        }

        public void Play(string id)
        {
            GameSession.LastCue = id;
            FluMixLog.Info("Audio", id);
            var clip = Bank != null ? Bank.Get(id) : null;
            if (clip == null) return;
            if (id == "water_loop")
            {
                _water.clip = clip;
                _water.loop = true;
                if (!_water.isPlaying) _water.Play();
                return;
            }
            if (id.StartsWith("music_") || id == "stinger_sos" || id == "warning_alarm")
            {
                var src = _pool.Count > 0 ? _ui : _musicA;
                src.PlayOneShot(clip);
                return;
            }
            bool uiCue = id.StartsWith("ui_") || id.StartsWith("quiz_") || id.StartsWith("input_") || id.StartsWith("podium_");
            if (uiCue)
            {
                _ui.PlayOneShot(clip);
                return;
            }
            var world = NextWorld();
            world.pitch = id == "beam_blue" ? 1.15f : id == "beam_pink" ? 0.9f : 1f;
            world.PlayOneShot(clip);
        }

        public void PlayMusic(string id)
        {
            GameSession.LastCue = id;
            FluMixLog.Info("Audio", id);
            var clip = Bank != null ? Bank.Get(id) : null;
            if (clip == null) return;
            var incoming = _usingA ? _musicB : _musicA;
            var outgoing = _usingA ? _musicA : _musicB;
            if (outgoing.clip == clip && outgoing.isPlaying) return;
            incoming.clip = clip;
            incoming.volume = 0f;
            incoming.loop = true;
            incoming.Play();
            _usingA = !_usingA;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(Crossfade(outgoing, incoming, 0.6f));
        }

        IEnumerator Crossfade(AudioSource from, AudioSource to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                from.volume = 1f - k;
                to.volume = k;
                yield return null;
            }
            from.Stop();
            from.volume = 1f;
            to.volume = 1f;
        }

        public void StopWater()
        {
            if (_water != null) _water.Stop();
        }

        AudioSource NextWorld()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].isPlaying) return _pool[i];
            }
            return _pool[0];
        }

        public void SetMuted(bool muted)
        {
            AudioListener.volume = muted ? 0f : 1f;
            if (Mixer != null)
            {
                Mixer.SetFloat("MasterVolume", muted ? -80f : 0f);
            }
        }

        public void SetMaster(float linear)
        {
            linear = Mathf.Clamp01(linear);
            AudioListener.volume = linear;
            if (Mixer != null)
            {
                float db = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
                Mixer.SetFloat("MasterVolume", db);
            }
        }
    }
}
