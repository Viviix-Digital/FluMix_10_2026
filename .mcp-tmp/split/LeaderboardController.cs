using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public class LeaderboardController : MonoBehaviour
    {
        BoardFile _file;
        int _page;
        Transform _slots;

        void Start()
        {
            _file = LeaderboardStore.Load();
            _slots = transform.Find("Panel/Slots");
            transform.Find("Home").GetComponent<Button>().onClick.AddListener(() => FluMixFlow.Instance.GoLobby());
            transform.Find("Close").GetComponent<Button>().onClick.AddListener(Close);
            transform.Find("Prev").GetComponent<Button>().onClick.AddListener(() => ChangePage(-1));
            transform.Find("Next").GetComponent<Button>().onClick.AddListener(() => ChangePage(1));
            var title = transform.Find("Panel/Title");
            var bob = title.GetComponent<TitleBob>();
            if (bob != null)
            {
                var glyphs = new RectTransform[title.childCount];
                for (int i = 0; i < title.childCount; i++) glyphs[i] = title.GetChild(i) as RectTransform;
                bob.Play(glyphs);
            }
            ShowPage(0);
            StartCoroutine(UiMotion.Enter(transform.Find("Panel").GetComponent<CanvasGroup>(), transform.Find("Panel") as RectTransform, FluMixFlow.Instance.Motion));
        }

        void ChangePage(int dir)
        {
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_page");
            ShowPage(Mathf.Clamp(_page + dir, 0, 3));
        }

        void ShowPage(int page)
        {
            _page = page;
            var label = transform.Find("Page").GetComponent<TMP_Text>();
            label.text = (_page + 1) + "/4";
            StartCoroutine(UiMotion.Punch(label.rectTransform));
            if (_page == 0)
            {
                int[] map = { 1, 0, 2 };
                for (int i = 0; i < _slots.childCount; i++)
                {
                    var slot = _slots.GetChild(i).gameObject;
                    if (i >= map.Length || _file == null || map[i] >= _file.entries.Count)
                    {
                        slot.SetActive(false);
                        continue;
                    }
                    slot.SetActive(true);
                    var entry = _file.entries[map[i]];
                    slot.GetComponentInChildren<TMP_Text>().text = entry.name + "\n" + GameSession.FormatScore(entry.score) + " điểm";
                }
                StartCoroutine(DropPodium());
                return;
            }
            int pageSize = 5;
            for (int i = 0; i < _slots.childCount; i++)
            {
                int index = 3 + (_page - 1) * pageSize + i;
                var slot = _slots.GetChild(i).gameObject;
                if (_file == null || index >= _file.entries.Count)
                {
                    slot.SetActive(false);
                    continue;
                }
                slot.SetActive(true);
                var entry = _file.entries[index];
                slot.GetComponentInChildren<TMP_Text>().text = entry.name + "\n" + GameSession.FormatScore(entry.score) + " điểm";
            }
        }

        IEnumerator DropPodium()
        {
            int[] order = { 1, 0, 2 };
            for (int n = 0; n < order.Length && n < _slots.childCount; n++)
            {
                var rt = _slots.GetChild(order[n]) as RectTransform;
                if (!rt.gameObject.activeSelf) continue;
                Vector2 end = rt.anchoredPosition;
                float t = 0f;
                while (t < 0.45f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = EaseOutBounce(Mathf.Clamp01(t / 0.45f));
                    rt.anchoredPosition = end + new Vector2(0f, Mathf.Lerp(40f, 0f, k));
                    yield return null;
                }
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("podium_land");
                yield return new WaitForSecondsRealtime(0.08f);
            }
        }

        static float EaseOutBounce(float x)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (x < 1f / d1) return n1 * x * x;
            if (x < 2f / d1) return n1 * (x -= 1.5f / d1) * x + 0.75f;
            if (x < 2.5f / d1) return n1 * (x -= 2.25f / d1) * x + 0.9375f;
            return n1 * (x -= 2.625f / d1) * x + 0.984375f;
        }

        void Close()
        {
            var prev = GameSession.Previous;
            if (prev == FlowState.Leaderboard || prev == FlowState.Boot) prev = FlowState.Lobby;
            FluMixFlow.Instance.Go(prev);
        }
    }
}
