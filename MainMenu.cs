using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    private void Awake()
    {
        EnsureEnvironment();
    }

    private void Start()
    {
        EnsureEnvironment();
    }

    private void EnsureEnvironment()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var b in buttons)
        {
            TMP_Text[] texts = b.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                t.raycastTarget = false;
            }

            if (b.name.IndexOf("Play", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                b.name.IndexOf("Start", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                b.onClick.RemoveListener(PlayGame);
                b.onClick.AddListener(PlayGame);
            }
            else if (b.name.IndexOf("Quit", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     b.name.IndexOf("Exit", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                b.onClick.RemoveListener(QuitGame);
                b.onClick.AddListener(QuitGame);
            }
        }
    }

    private void Update()
    {
        // Support keyboard shortcuts on Main Menu: Enter/Space to Play, Esc to Quit
        bool playPressed = false;
        bool quitPressed = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                playPressed = true;
            }
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                quitPressed = true;
            }
        }
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            playPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            quitPressed = true;
        }
#endif

        if (playPressed)
        {
            PlayGame();
        }
        else if (quitPressed)
        {
            QuitGame();
        }
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        string targetScene = string.IsNullOrEmpty(gameSceneName) ? "GameScene" : gameSceneName;
        SceneManager.LoadScene(targetScene);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
