using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text highScoreText;

    [Header("Optional Notice")]
    [SerializeField] private TMP_Text newHighScoreBadge;

    public TMP_Text NewHighScoreBadge
    {
        get => newHighScoreBadge;
        set => newHighScoreBadge = value;
    }

    public void SetBadgeReference(TMP_Text badge)
    {
        newHighScoreBadge = badge;
    }

    private void Awake()
    {
        EnsureReferences();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    public void EnsureReferences()
    {
        if (gameOverPanel == null)
        {
            Transform panel = transform.Find("GameOverPanel") ?? transform.Find("Panel");
            if (panel != null)
            {
                gameOverPanel = panel.gameObject;
            }
            else
            {
                GameObject p = GameObject.Find("GameOverPanel");
                if (p != null) gameOverPanel = p;
            }
        }

        if (currentScoreText == null)
        {
            GameObject cs = GameObject.Find("CurrentScore");
            if (cs != null) currentScoreText = cs.GetComponent<TMP_Text>();
        }

        if (highScoreText == null)
        {
            GameObject hs = GameObject.Find("HighScore");
            if (hs != null) highScoreText = hs.GetComponent<TMP_Text>();
        }

        if (newHighScoreBadge == null && gameOverPanel != null)
        {
            Transform badge = gameOverPanel.transform.Find("NewHighScoreText") ?? gameOverPanel.transform.Find("NewHighScore");
            if (badge != null)
            {
                newHighScoreBadge = badge.GetComponent<TMP_Text>();
            }
            else
            {
                GameObject badgeObj = new GameObject("NewHighScoreText");
                badgeObj.transform.SetParent(gameOverPanel.transform, false);

                RectTransform rt = badgeObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -22f);
                rt.sizeDelta = new Vector2(260f, 35f);

                TextMeshProUGUI tmp = badgeObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "New High Score!";
                tmp.fontSize = 22;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(1f, 0.85f, 0.2f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;

                if (highScoreText != null)
                {
                    tmp.font = highScoreText.font;
                    badgeObj.transform.SetSiblingIndex(highScoreText.transform.GetSiblingIndex());
                }

                badgeObj.SetActive(false);
                newHighScoreBadge = tmp;
            }
        }

        if (newHighScoreBadge != null)
        {
            newHighScoreBadge.gameObject.SetActive(false);
        }

        // Auto-wire buttons and ensure child TMP texts never block clicks
        Button[] buttons = gameOverPanel != null
            ? gameOverPanel.GetComponentsInChildren<Button>(true)
            : GetComponentsInChildren<Button>(true);

        foreach (var b in buttons)
        {
            TMP_Text[] childTexts = b.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in childTexts)
            {
                t.raycastTarget = false;
            }

            if (b.name.IndexOf("Restart", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                b.onClick.RemoveListener(Restart);
                b.onClick.AddListener(Restart);
            }
            else if (b.name.IndexOf("Menu", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                b.onClick.RemoveListener(MainMenu);
                b.onClick.AddListener(MainMenu);
            }
        }
    }

    private void Start()
    {
        EnsureReferences();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOver;
        }
    }

    private void Update()
    {
        if (gameOverPanel != null && gameOverPanel.activeInHierarchy)
        {
            bool restartPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                restartPressed = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                restartPressed = true;
            }
#endif
            if (restartPressed)
            {
                Restart();
            }
        }
    }

    private void HandleGameOver(int finalScore, int highScore, bool isNewHighScore)
    {
        EnsureReferences();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (currentScoreText != null)
        {
            currentScoreText.text = $"Score: {finalScore}";
        }

        if (highScoreText != null)
        {
            highScoreText.text = $"High Score: {highScore}";
            highScoreText.color = isNewHighScore ? new Color(1f, 0.85f, 0.2f) : Color.white;
        }

        if (newHighScoreBadge != null)
        {
            newHighScoreBadge.gameObject.SetActive(isNewHighScore);
            if (isNewHighScore)
            {
                newHighScoreBadge.text = "New High Score!";
            }
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "GameScene";
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
    }
}
