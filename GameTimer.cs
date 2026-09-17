using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimerChanged += HandleTimerChanged;
            HandleTimerChanged(GameManager.Instance.RemainingTime);
        }
    }

    private void HandleTimerChanged(float remainingTime)
    {
        if (timerText == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"Timer: {minutes:00}:{seconds:00}";
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimerChanged -= HandleTimerChanged;
        }
    }
}