using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControlsWindowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private Button beginButton;

    private void Awake()
    {
        EnsureReferences();
    }

    private void Start()
    {
        EnsureReferences();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            if (GameManager.Instance.CurrentState == GameState.WaitingToStart)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
        else
        {
            Show();
        }
    }

    public void EnsureReferences()
    {
        if (controlsPanel == null)
        {
            Transform panel = transform.Find("ControlsPanel");
            if (panel == null)
            {
                GameObject ui = GameObject.Find("UI");
                if (ui != null)
                {
                    panel = ui.transform.Find("ControlsPanel");
                }
            }

            if (panel != null)
            {
                controlsPanel = panel.gameObject;
            }
            else
            {
                CreateControlsPanel();
            }
        }

        if (controlsPanel != null && beginButton == null)
        {
            Button[] buttons = controlsPanel.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                TMP_Text[] texts = b.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in texts)
                {
                    t.raycastTarget = false;
                }

                if (b.name.IndexOf("Begin", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    b.name.IndexOf("Start", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    b.name.IndexOf("Play", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    beginButton = b;
                    break;
                }
            }

            if (beginButton == null && buttons.Length > 0)
            {
                beginButton = buttons[0];
            }
        }

        if (beginButton != null)
        {
            beginButton.onClick.RemoveListener(BeginGame);
            beginButton.onClick.AddListener(BeginGame);
        }
    }

    private void CreateControlsPanel()
    {
        GameObject uiObj = GameObject.Find("UI");
        Transform parent = uiObj != null ? uiObj.transform : transform;

        controlsPanel = new GameObject("ControlsPanel");
        controlsPanel.transform.SetParent(parent, false);

        RectTransform rootRT = controlsPanel.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;

        // Dark dim backdrop
        Image rootDim = controlsPanel.AddComponent<Image>();
        rootDim.color = new Color(0f, 0f, 0f, 0.78f);

        // Center Card Dialog
        GameObject cardObj = new GameObject("Card");
        cardObj.transform.SetParent(controlsPanel.transform, false);

        RectTransform cardRT = cardObj.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(620f, 440f);

        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.11f, 0.14f, 0.20f, 0.98f);

        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.85f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // Title Header
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.84f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "<b><size=25><color=#FFD700>HOW TO PLAY & CONTROLS</color></size></b>";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;

        // Content Text
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(cardObj.transform, false);
        RectTransform contentRT = contentObj.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.05f, 0.22f);
        contentRT.anchorMax = new Vector2(0.95f, 0.84f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;

        TextMeshProUGUI contentTMP = contentObj.AddComponent<TextMeshProUGUI>();
        contentTMP.fontSize = 14;
        contentTMP.lineSpacing = -5f;
        contentTMP.raycastTarget = false;
        contentTMP.text =
            "<b><color=#00FFFF>CONTROLS:</color></b>\n" +
            "• <b>Move:</b> <color=#FFD700>W, A, S, D</color> or <color=#FFD700>Arrow Keys</color>\n" +
            "• <b>Interact / Pick Up / Place:</b> <color=#FFD700>E</color> or <color=#FFD700>Left Mouse Click</color>\n" +
            "• <b>Pause / Resume:</b> <color=#FFD700>ESC</color> or <color=#FFD700>P</color> | <b>Restart:</b> <color=#FFD700>R</color>\n" +
            "• <b>Refrigerator:</b> Approach fridge & press <color=#FFD700>1 (Meat)</color>, <color=#FFD700>2 (Cheese)</color>, <color=#FFD700>3 (Veggies)</color>\n\n" +
            "<b><color=#55FF88>RECIPES & KITCHEN STATIONS:</color></b>\n" +
            "• <b>Meat (30 pts):</b> Cook on <b>Stove</b> (takes 6s) before serving.\n" +
            "• <b>Vegetables (20 pts):</b> Chop on <b>Table</b> (takes 2s) before serving.\n" +
            "• <b>Cheese (10 pts):</b> Ready to serve directly to Customer Windows!\n" +
            "• <b>Customer Windows:</b> 4 windows. Fulfill fast before time docks score!\n" +
            "• <b>Trash:</b> Discard any mistake or unwanted ingredient.";

        // Begin Game Button
        GameObject btnObj = new GameObject("BeginButton");
        btnObj.transform.SetParent(cardObj.transform, false);
        RectTransform btnRT = btnObj.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.2f, 0.04f);
        btnRT.anchorMax = new Vector2(0.8f, 0.18f);
        btnRT.offsetMin = Vector2.zero;
        btnRT.offsetMax = Vector2.zero;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.72f, 0.28f, 1f);

        beginButton = btnObj.AddComponent<Button>();
        ColorBlock cb = beginButton.colors;
        cb.highlightedColor = new Color(0.25f, 0.88f, 0.38f);
        cb.pressedColor = new Color(0.12f, 0.52f, 0.20f);
        beginButton.colors = cb;

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRT = btnTextObj.AddComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTMP = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "<b><size=18>BEGIN GAME [SPACE / ENTER]</size></b>";
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = Color.white;
        btnTMP.raycastTarget = false;
    }

    private void Update()
    {
        if (controlsPanel != null && controlsPanel.activeInHierarchy)
        {
            bool startPressed = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame ||
                    Keyboard.current.eKey.wasPressedThisFrame)
                {
                    startPressed = true;
                }
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
            {
                startPressed = true;
            }
#endif

            if (startPressed)
            {
                BeginGame();
            }
        }
    }

    private void HandleStateChanged(GameState newState)
    {
        if (newState == GameState.WaitingToStart)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    public void Show()
    {
        EnsureReferences();
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Hide()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
    }

    public void BeginGame()
    {
        Hide();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
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
