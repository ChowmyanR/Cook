using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private void Awake()
    {
        if (scoreText == null)
        {
            scoreText = GetComponent<TMP_Text>();
        }

        if (scoreText != null)
        {
            scoreText.raycastTarget = false;
            RectTransform rt = scoreText.GetComponent<RectTransform>();
            if (rt != null && rt.sizeDelta.x < 260f)
            {
                rt.sizeDelta = new Vector2(280f, rt.sizeDelta.y);
            }
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += HandleScoreChanged;
            HandleScoreChanged(GameManager.Instance.CurrentScore);
        }
        else
        {
            HandleScoreChanged(0);
        }
    }

    private void HandleScoreChanged(int currentScore)
    {
        if (scoreText != null)
        {
            int high = GameManager.Instance != null ? GameManager.Instance.HighScore : PlayerPrefs.GetInt("HighScore", 0);
            if (currentScore > high)
            {
                high = currentScore;
            }

            scoreText.text = $"Score: <b>{currentScore}</b>   |   <color=#FFD700>High: <b>{high}</b></color>";
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= HandleScoreChanged;
        }
    }
}
