using FluMix.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public class ResultView : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text timeText;
        [SerializeField] TMP_Text questionText;
        [SerializeField] TMP_Text rankText;
        [SerializeField] Button playAgainButton;
        [SerializeField] Button leaderboardButton;

        public void Bind(GameFlow gameFlow, TMP_Text score, TMP_Text time, TMP_Text questions, TMP_Text rank, Button again, Button board)
        {
            flow = gameFlow;
            scoreText = score;
            timeText = time;
            questionText = questions;
            rankText = rank;
            playAgainButton = again;
            leaderboardButton = board;
            Wire();
        }

        void Awake()
        {
            Wire();
        }

        void Wire()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveListener(PlayAgain);
                playAgainButton.onClick.AddListener(PlayAgain);
            }

            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.RemoveListener(OpenBoard);
                leaderboardButton.onClick.AddListener(OpenBoard);
            }
        }

        public void Show(int score, float elapsed, int correct, int answered, int rank)
        {
            if (scoreText != null)
                scoreText.text = ScoreKeeper.FormatScore(score);
            if (timeText != null)
                timeText.text = ScoreKeeper.FormatElapsed(elapsed);
            if (questionText != null)
                questionText.text = correct + "/" + answered;
            if (rankText != null)
                rankText.text = rank.ToString();
        }

        public void PlayAgain()
        {
            if (flow != null)
                flow.PlayAgain();
        }

        public void OpenBoard()
        {
            if (flow != null)
                flow.OpenLeaderboard();
        }
    }
}
