using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Configuration")]
    [SerializeField] private float gameDuration = 180f; // 3 minutes
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("State")]
    private GameState currentState = GameState.WaitingToStart;
    private float remainingTime;
    private int currentScore;
    private int highScore;

    // Events for Decoupled Observers
    public event Action<GameState> OnStateChanged;
    public event Action<float> OnTimerChanged;
    public event Action<int> OnScoreChanged;
    public event Action<int, int, bool> OnGameOver; // (finalScore, highScore, isNewHighScore)

    public GameState CurrentState => currentState;
    public float RemainingTime => remainingTime;
    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public bool IsPlaying => currentState == GameState.Playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadHighScore();
        remainingTime = gameDuration;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        OnScoreChanged?.Invoke(currentScore);
        OnTimerChanged?.Invoke(remainingTime);

        // Display controls window on start; player clicks Begin Game to start 3-minute session
        SetState(GameState.WaitingToStart);
    }

    public void StartGame()
    {
        SetState(GameState.Playing);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime < 0f)
        {
            remainingTime = 0f;
        }

        OnTimerChanged?.Invoke(remainingTime);

        if (remainingTime <= 0f)
        {
            TriggerGameOver();
        }
    }

    public void AddScore(int points)
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
    }

    private void TriggerGameOver()
    {
        SetState(GameState.GameOver);

        bool isNewHighScore = false;
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
            isNewHighScore = true;
        }

        Time.timeScale = 0f;
        OnGameOver?.Invoke(currentScore, highScore, isNewHighScore);
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;
        OnStateChanged?.Invoke(currentState);
    }

    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            PauseGame();
        }
        else if (currentState == GameState.Paused)
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        SetState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused)
        {
            return;
        }

        SetState(GameState.Playing);
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = string.IsNullOrEmpty(gameSceneName) ? "GameScene" : gameSceneName;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }
}
