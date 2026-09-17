using System.Collections.Generic;
using Orders;
using Stations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
public class GameSetup : MonoBehaviour
{
    private static bool s_RegisteredCallback = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitSceneLoadedCallback()
    {
        if (!s_RegisteredCallback)
        {
            s_RegisteredCallback = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            GameSetup setup = FindAnyObjectByType<GameSetup>();
            if (setup == null)
            {
                GameObject bootstrap = new GameObject("AutoGameSetup");
                setup = bootstrap.AddComponent<GameSetup>();
            }
            else
            {
                setup.SetupSceneConnectivity();
            }
        }
    }

    private void Awake()
    {
        SetupSceneConnectivity();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrapScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name == "GameScene")
        {
            if (FindAnyObjectByType<GameSetup>() == null)
            {
                GameObject bootstrap = new GameObject("AutoGameSetup");
                bootstrap.AddComponent<GameSetup>();
            }
        }
    }

    public void SetupSceneConnectivity()
    {
        GameObject customerPrefab = Resources.Load<GameObject>("Customers");

        // 1. Setup GameManager & OrderManager
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj != null)
        {
            if (gmObj.GetComponent<GameManager>() == null)
            {
                gmObj.AddComponent<GameManager>();
            }

            if (gmObj.GetComponent<OrderManager>() == null)
            {
                gmObj.AddComponent<OrderManager>();
            }
        }

        // 2. Setup Player Interactor
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            if (playerObj.GetComponent<PlayerInteractor>() == null)
            {
                playerObj.AddComponent<PlayerInteractor>();
            }
        }

        // 3. Setup Refrigerator & Child RefrigeratorPanel
        GameObject fridgeObj = GameObject.Find("Refrigirator");
        if (fridgeObj != null)
        {
            EnsureCollider(fridgeObj);
            RefrigeratorStation fridgeStation = fridgeObj.GetComponent<RefrigeratorStation>();
            if (fridgeStation == null)
            {
                fridgeStation = fridgeObj.AddComponent<RefrigeratorStation>();
            }

            SetupRefrigeratorPanel(fridgeObj, fridgeStation);
        }

        // 4. Setup Stove
        GameObject stoveObj = GameObject.Find("Stove");
        if (stoveObj != null)
        {
            EnsureCollider(stoveObj);
            if (stoveObj.GetComponent<StoveStation>() == null)
            {
                stoveObj.AddComponent<StoveStation>();
            }
        }

        // 5. Setup Table
        GameObject tableObj = GameObject.Find("Table");
        if (tableObj != null)
        {
            EnsureCollider(tableObj);
            if (tableObj.GetComponent<TableStation>() == null)
            {
                tableObj.AddComponent<TableStation>();
            }
        }

        // 6. Setup Trash
        GameObject trashObj = GameObject.Find("Trash");
        if (trashObj != null)
        {
            EnsureCollider(trashObj);
            if (trashObj.GetComponent<TrashStation>() == null)
            {
                trashObj.AddComponent<TrashStation>();
            }
        }

        // 7. Setup Customer Windows
        List<CustomerWindowStation> windowStations = new List<CustomerWindowStation>();
        string[] windowNames = new string[] { "Window", "Window (1)", "Window (2)", "Window (3)" };

        for (int i = 0; i < windowNames.Length; i++)
        {
            GameObject winObj = GameObject.Find(windowNames[i]);
            if (winObj != null)
            {
                EnsureCollider(winObj);
                CustomerWindowStation station = winObj.GetComponent<CustomerWindowStation>();
                if (station == null)
                {
                    station = winObj.AddComponent<CustomerWindowStation>();
                }
                station.SetWindowIndex(i);
                if (customerPrefab != null)
                {
                    station.SetCustomerPrefab(customerPrefab);
                }
                windowStations.Add(station);
            }
        }

        // 8. Register windows with OrderManager
        OrderManager orderMgr = OrderManager.Instance != null ? OrderManager.Instance : FindAnyObjectByType<OrderManager>();
        if (orderMgr != null && windowStations.Count > 0)
        {
            orderMgr.RegisterWindows(windowStations.ToArray(), customerPrefab);
        }

        // 9. Setup Score UI
        GameObject scoreObj = GameObject.Find("Score");
        if (scoreObj != null && scoreObj.GetComponent<ScoreUI>() == null)
        {
            scoreObj.AddComponent<ScoreUI>();
        }

        // 10. Setup Controls Window UI & HUD Buttons (Pause & Quit)
        GameObject uiObj = GameObject.Find("UI");
        if (uiObj != null)
        {
            if (uiObj.GetComponent<ControlsWindowUI>() == null)
            {
                uiObj.AddComponent<ControlsWindowUI>();
            }

            SetupHUDButtons(uiObj);
        }
    }

    private void SetupHUDButtons(GameObject uiObj)
    {
        Transform hudPanel = uiObj.transform.Find("Panel");
        if (hudPanel == null) return;

        // Ensure Pause button has text raycast off and clicks Pause
        Transform pauseBtnT = hudPanel.Find("PauseButton");
        if (pauseBtnT != null)
        {
            Button pBtn = pauseBtnT.GetComponent<Button>();
            if (pBtn != null)
            {
                PauseMenu pm = FindAnyObjectByType<PauseMenu>();
                if (pm != null)
                {
                    pBtn.onClick.RemoveListener(pm.Pause);
                    pBtn.onClick.AddListener(pm.Pause);
                }
            }
            TMP_Text[] tmps = pauseBtnT.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps) t.raycastTarget = false;
        }

        // Ensure Quit button on HUD next to Pause button
        Transform quitBtnT = hudPanel.Find("QuitButton");
        if (quitBtnT == null)
        {
            GameObject quitObj = new GameObject("QuitButton");
            quitObj.transform.SetParent(hudPanel, false);

            RectTransform rt = quitObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(-195f, -275.8f);
            rt.sizeDelta = new Vector2(50f, 50f);

            Image img = quitObj.AddComponent<Image>();
            img.color = new Color(0.85f, 0.22f, 0.22f, 1f);

            Button btn = quitObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.85f, 0.22f, 0.22f, 1f);
            cb.highlightedColor = new Color(0.95f, 0.35f, 0.35f, 1f);
            cb.pressedColor = new Color(0.65f, 0.15f, 0.15f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            });

            GameObject textObj = new GameObject("Text (TMP)");
            textObj.transform.SetParent(quitObj.transform, false);
            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "<b>X</b>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }
    }

    private void SetupRefrigeratorPanel(GameObject fridgeObj, RefrigeratorStation fridgeStation)
    {
        Transform existingPanel = fridgeObj.transform.Find("RefrigeratorPanel");
        if (existingPanel != null)
        {
            RefrigeratorUI existingUI = existingPanel.GetComponent<RefrigeratorUI>();
            if (existingUI == null)
            {
                existingUI = existingPanel.gameObject.AddComponent<RefrigeratorUI>();
            }

            // Find child buttons even if reorganized by user
            Button meat = existingPanel.Find("Background/ButtonsRow/MEATButton")?.GetComponent<Button>()
                       ?? existingPanel.Find("ButtonsRow/MEATButton")?.GetComponent<Button>()
                       ?? existingPanel.GetComponentInChildren<Button>();
            Button cheese = existingPanel.Find("Background/ButtonsRow/CHEESEButton")?.GetComponent<Button>()
                         ?? existingPanel.Find("ButtonsRow/CHEESEButton")?.GetComponent<Button>();
            Button veg = existingPanel.Find("Background/ButtonsRow/VEGGIESButton")?.GetComponent<Button>()
                      ?? existingPanel.Find("ButtonsRow/VEGGIESButton")?.GetComponent<Button>();
            Button close = existingPanel.Find("Background/CloseButton")?.GetComponent<Button>()
                        ?? existingPanel.Find("CloseButton")?.GetComponent<Button>();

            if (meat != null && cheese != null && veg != null)
            {
                GameObject root = existingPanel.Find("Background")?.gameObject ?? existingPanel.gameObject;
                existingUI.SetReferences(root, meat, cheese, veg, close);
            }

            fridgeStation.SetRefrigeratorUI(existingUI);
            existingUI.Close();
            return;
        }

        // Create RefrigeratorPanel as child in hierarchy below Refrigirator
        GameObject panelObj = new GameObject("RefrigeratorPanel");
        panelObj.transform.SetParent(fridgeObj.transform);

        // Position above refrigerator
        panelObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);

        // Compensate for refrigerator's scale so UI proportions remain crisp
        Vector3 parentScale = fridgeObj.transform.lossyScale;
        panelObj.transform.localScale = new Vector3(
            0.014f / Mathf.Max(parentScale.x, 0.01f),
            0.014f / Mathf.Max(parentScale.y, 0.01f),
            0.014f / Mathf.Max(parentScale.z, 0.01f)
        );

        Canvas canvas = panelObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        panelObj.AddComponent<CanvasScaler>();
        panelObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = panelObj.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(380f, 240f);

        // Background Box
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(panelObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.10f, 0.12f, 0.18f, 0.95f);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // Content Layout
        VerticalLayoutGroup layout = bgObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(bgObj.transform, false);
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "<b><color=#FFD700>REFRIGERATOR</color></b>";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize = 16;
        titleTMP.raycastTarget = false;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(bgObj.transform, false);
        TextMeshProUGUI subTMP = subObj.AddComponent<TextMeshProUGUI>();
        subTMP.text = "<color=#CCCCCC>Select an ingredient to take:</color>";
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.fontSize = 11;
        subTMP.raycastTarget = false;

        // Buttons Row
        GameObject rowObj = new GameObject("ButtonsRow");
        rowObj.transform.SetParent(bgObj.transform, false);
        HorizontalLayoutGroup rowLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = false;

        RectTransform rowRT = rowObj.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(340f, 75f);
        LayoutElement rowLE = rowObj.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 75f;

        // 1. Meat Button
        Button meatBtn = CreateIngredientButton(rowObj.transform, "MEAT", "[1]", new Color(0.78f, 0.22f, 0.22f));

        // 2. Cheese Button
        Button cheeseBtn = CreateIngredientButton(rowObj.transform, "CHEESE", "[2]", new Color(0.85f, 0.68f, 0.15f));

        // 3. Vegetables Button
        Button vegBtn = CreateIngredientButton(rowObj.transform, "VEGGIES", "[3]", new Color(0.22f, 0.65f, 0.28f));

        // Close Button
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(bgObj.transform, false);
        Image closeImg = closeObj.AddComponent<Image>();
        closeImg.color = new Color(0.25f, 0.28f, 0.35f, 0.9f);
        Button closeBtn = closeObj.AddComponent<Button>();
        LayoutElement closeLE = closeObj.AddComponent<LayoutElement>();
        closeLE.preferredHeight = 30f;

        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeObj.transform, false);
        TextMeshProUGUI closeTMP = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeTMP.text = "Close [E]";
        closeTMP.alignment = TextAlignmentOptions.Center;
        closeTMP.fontSize = 14;
        closeTMP.color = Color.white;
        closeTMP.raycastTarget = false;

        RectTransform closeTextRT = closeTextObj.GetComponent<RectTransform>();
        closeTextRT.anchorMin = Vector2.zero;
        closeTextRT.anchorMax = Vector2.one;
        closeTextRT.sizeDelta = Vector2.zero;

        // Add and wire RefrigeratorUI
        RefrigeratorUI ui = panelObj.AddComponent<RefrigeratorUI>();
        ui.SetReferences(panelObj, meatBtn, cheeseBtn, vegBtn, closeBtn);

        fridgeStation.SetRefrigeratorUI(ui);
        ui.Close();
    }

    private Button CreateIngredientButton(Transform parent, string title, string shortcut, Color bgColor)
    {
        GameObject btnObj = new GameObject($"{title}_Button");
        btnObj.transform.SetParent(parent, false);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = bgColor;
        btnImg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = bgColor * 1.15f;
        colors.pressedColor = bgColor * 0.85f;
        btn.colors = colors;

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = 100f;
        le.preferredHeight = 70f;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"<b>{title}</b>\n<size=9><color=#EEEEEE>{shortcut}</color></size>";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 11;
        tmp.color = Color.white;
        tmp.raycastTarget = false; // Never block click raycasts to button!

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        return btn;
    }

    private void EnsureCollider(GameObject obj)
    {
        if (obj.GetComponent<Collider>() == null && obj.GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = obj.AddComponent<BoxCollider>();
            box.size = new Vector3(2f, 2f, 2f);
        }
    }
}
