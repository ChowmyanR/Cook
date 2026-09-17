using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    private void Awake()
    {
        EnsureReferences();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void EnsureReferences()
    {
        if (pausePanel == null)
        {
            Transform panel = transform.Find("PausePanel") ?? transform.Find("Panel");
            if (panel != null)
            {
                pausePanel = panel.gameObject;
            }
            else
            {
                GameObject p = GameObject.Find("PausePanel");
                if (p != null) pausePanel = p;
            }
        }

        if (pausePanel != null)
        {
            Button[] buttons = pausePanel.GetComponentsInChildren<Button>(true);
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
                else if (b.name.IndexOf("Resume", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    b.onClick.RemoveListener(Resume);
                    b.onClick.AddListener(Resume);
                }
                else if (b.name.IndexOf("Menu", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    b.onClick.RemoveListener(MainMenu);
                    b.onClick.AddListener(MainMenu);
                }
                else if (b.name.IndexOf("Quit", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         b.name.IndexOf("Exit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    b.onClick.RemoveListener(QuitGame);
                    b.onClick.AddListener(QuitGame);
                }
            }

            // If no Quit button in pause panel, create one below MainMenu
            Transform existingQuit = pausePanel.transform.Find("Quit") ?? pausePanel.transform.Find("QuitButton");
            if (existingQuit == null)
            {
                GameObject quitObj = new GameObject("Quit");
                quitObj.transform.SetParent(pausePanel.transform, false);

                RectTransform rt = quitObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -98f);
                rt.sizeDelta = new Vector2(125f, 45f);

                Image img = quitObj.AddComponent<Image>();
                img.color = new Color(0.85f, 0.22f, 0.22f, 1f);

                Button btn = quitObj.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(0.85f, 0.22f, 0.22f, 1f);
                cb.highlightedColor = new Color(0.95f, 0.35f, 0.35f, 1f);
                cb.pressedColor = new Color(0.65f, 0.15f, 0.15f, 1f);
                btn.colors = cb;
                btn.onClick.AddListener(QuitGame);

                GameObject textObj = new GameObject("Text (TMP)");
                textObj.transform.SetParent(quitObj.transform, false);
                RectTransform textRT = textObj.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.sizeDelta = Vector2.zero;

                TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "Quit";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 20;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
            }
        }
    }

    private void Start()
    {
        EnsureReferences();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    private void Update()
    {
        // Support Escape key or P key for fast keyboard pausing
        bool escapePressed = false;
        bool restartPressed = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)
            {
                escapePressed = true;
            }

            if (pausePanel != null && pausePanel.activeInHierarchy && Keyboard.current.rKey.wasPressedThisFrame)
            {
                restartPressed = true;
            }
        }
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            escapePressed = true;
        }

        if (pausePanel != null && pausePanel.activeInHierarchy && Input.GetKeyDown(KeyCode.R))
        {
            restartPressed = true;
        }
#endif

        if (escapePressed)
        {
            TogglePause();
        }
        else if (restartPressed)
        {
            Restart();
        }
    }

    private void HandleStateChanged(GameState newState)
    {
        bool isPaused = (newState == GameState.Paused);

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }

        if (isPaused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            EnsureReferences();
        }
    }

    public void TogglePause()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }

    public void Pause()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
    }

    public void Resume()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
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
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "GameScene";
            SceneManager.LoadScene(sceneName);
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
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }
}
