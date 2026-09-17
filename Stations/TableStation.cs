using Kitchen;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Stations
{
    public class TableStation : BaseStation
    {
        private bool isChopping = false;
        private float chopTimer = 0f;
        private float chopDuration = 2.0f; // Exactly 2 seconds as specified

        // Top-of-Table Timer UI
        private GameObject timerCanvasObj;
        private TMP_Text titleText;
        private TMP_Text countdownText;
        private RectTransform fillBarRT;
        private const float MaxBarWidth = 190f;
        private Camera mainCam;

        protected override void Awake()
        {
            base.Awake();
            mainCam = Camera.main;
            CreateTimerUI();
        }

        private void CreateTimerUI()
        {
            timerCanvasObj = new GameObject("TableTimerUI");
            timerCanvasObj.transform.SetParent(transform);

            // Position right above the cutting table surface
            Vector3 targetPos = holdPoint != null
                ? holdPoint.localPosition + new Vector3(0f, 1.4f, 0f)
                : new Vector3(0f, 2.3f, 0f);
            timerCanvasObj.transform.localPosition = targetPos;

            Canvas canvas = timerCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            timerCanvasObj.AddComponent<CanvasScaler>();

            RectTransform canvasRT = timerCanvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(240f, 100f);
            timerCanvasObj.transform.localScale = Vector3.one * 0.011f;

            // Background Card
            GameObject bgObj = new GameObject("CardBg");
            bgObj.transform.SetParent(timerCanvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.10f, 0.13f, 0.18f, 0.94f);

            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Subtle border
            Outline outline = bgObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.85f, 0.45f, 0.7f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Header Title Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(bgObj.transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "<b><color=#55FF88>CHOPPING VEGETABLES</color></b>";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 15;
            titleText.raycastTarget = false;

            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.62f);
            titleRT.anchorMax = new Vector2(1f, 0.95f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            // Countdown Timer Text
            GameObject cdObj = new GameObject("CountdownText");
            cdObj.transform.SetParent(bgObj.transform, false);
            countdownText = cdObj.AddComponent<TextMeshProUGUI>();
            countdownText.text = "<b>2.0s</b> <size=13><color=#AAAAAA>remaining</color></size>";
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.fontSize = 17;
            countdownText.color = Color.white;
            countdownText.raycastTarget = false;

            RectTransform cdRT = cdObj.GetComponent<RectTransform>();
            cdRT.anchorMin = new Vector2(0f, 0.28f);
            cdRT.anchorMax = new Vector2(1f, 0.62f);
            cdRT.offsetMin = Vector2.zero;
            cdRT.offsetMax = Vector2.zero;

            // Progress Bar Track
            GameObject trackObj = new GameObject("ProgressTrack");
            trackObj.transform.SetParent(bgObj.transform, false);
            Image trackImg = trackObj.AddComponent<Image>();
            trackImg.color = new Color(0.2f, 0.24f, 0.3f, 0.9f);

            RectTransform trackRT = trackObj.GetComponent<RectTransform>();
            trackRT.anchorMin = new Vector2(0.5f, 0.12f);
            trackRT.anchorMax = new Vector2(0.5f, 0.12f);
            trackRT.sizeDelta = new Vector2(MaxBarWidth, 10f);

            // Progress Bar Fill
            GameObject fillObj = new GameObject("ProgressFill");
            fillObj.transform.SetParent(trackObj.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.35f, 0.95f, 0.45f, 1f);

            fillBarRT = fillObj.GetComponent<RectTransform>();
            fillBarRT.anchorMin = new Vector2(0f, 0f);
            fillBarRT.anchorMax = new Vector2(0f, 1f);
            fillBarRT.pivot = new Vector2(0f, 0.5f);
            fillBarRT.sizeDelta = new Vector2(0f, 0f);

            timerCanvasObj.SetActive(false);
        }

        private void LateUpdate()
        {
            if (timerCanvasObj != null && timerCanvasObj.activeSelf)
            {
                if (mainCam == null) mainCam = Camera.main;
                if (mainCam != null)
                {
                    timerCanvasObj.transform.rotation = mainCam.transform.rotation;
                }
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (currentItem == null)
            {
                if (isChopping)
                {
                    isChopping = false;
                    if (timerCanvasObj != null) timerCanvasObj.SetActive(false);
                }
                return;
            }

            if (isChopping)
            {
                chopTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(chopTimer / chopDuration);
                float remaining = Mathf.Max(0f, chopDuration - chopTimer);

                // Update countdown display and progress bar
                if (countdownText != null)
                {
                    countdownText.text = $"<b><color=#FFD700>{remaining:0.0}s</color></b> <size=13><color=#CCCCCC>remaining</color></size>";
                }

                if (fillBarRT != null)
                {
                    fillBarRT.sizeDelta = new Vector2(MaxBarWidth * progress, 0f);
                }

                // Chopping complete!
                if (chopTimer >= chopDuration)
                {
                    IngredientType result = IngredientDatabase.GetPreparedResult(currentItem.IngredientType);
                    if (result == IngredientType.None)
                    {
                        result = IngredientType.SlicedVegetables;
                    }
                    currentItem.SetIngredientType(result);

                    isChopping = false;

                    if (titleText != null)
                    {
                        titleText.text = "<b><color=#00FF66>CHOPPED! (READY)</color></b>";
                    }

                    if (countdownText != null)
                    {
                        countdownText.text = "<size=14><color=#FFFFFF>Pick up [E] to fulfill order</color></size>";
                    }

                    if (fillBarRT != null)
                    {
                        fillBarRT.sizeDelta = new Vector2(MaxBarWidth, 0f);
                    }
                }
            }
        }

        public override bool CanInteract(PlayerInteractor interactor)
        {
            if (interactor == null) return false;

            // Only 1 vegetable can be chopped at a time - cannot interact while chopping is in progress
            if (isChopping)
            {
                return false;
            }

            // Table has an item: empty-handed player can pick it up
            if (HasItem && !interactor.IsHoldingItem)
            {
                return true;
            }

            // Table is empty: player holding vegetables can place it to chop
            if (!HasItem && interactor.IsHoldingItem)
            {
                IngredientType held = interactor.HeldItem.IngredientType;
                return held == IngredientType.Vegetables || held == IngredientType.Tomato ||
                       IngredientDatabase.CanPrepareAt(held, StationType.Table);
            }

            return false;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor)) return;

            // Case 1: Pick up chopped vegetable (or prepared item) from table
            if (HasItem && !interactor.IsHoldingItem)
            {
                KitchenObject item = TakeItem();
                interactor.HoldItem(item);

                isChopping = false;
                chopTimer = 0f;
                if (timerCanvasObj != null)
                {
                    timerCanvasObj.SetActive(false);
                }
                return;
            }

            // Case 2: Place raw vegetable onto table to start chopping (Only 1 item at a time)
            if (!HasItem && interactor.IsHoldingItem)
            {
                IngredientType held = interactor.HeldItem.IngredientType;
                if (held == IngredientType.Vegetables || held == IngredientType.Tomato ||
                    IngredientDatabase.CanPrepareAt(held, StationType.Table))
                {
                    KitchenObject item = interactor.DropItem();
                    PlaceItem(item);

                    chopDuration = 2.0f; // Exactly 2 seconds
                    chopTimer = 0f;
                    isChopping = true;

                    if (titleText != null)
                    {
                        titleText.text = "<b><color=#55FF88>CHOPPING VEGETABLES</color></b>";
                    }

                    if (timerCanvasObj != null)
                    {
                        timerCanvasObj.SetActive(true);
                    }
                }
            }
        }

        public override string GetInteractionPrompt(PlayerInteractor interactor)
        {
            if (interactor == null) return "Chopping Table";

            if (isChopping)
            {
                float remaining = Mathf.Max(0f, chopDuration - chopTimer);
                return $"Chopping Vegetables... ({remaining:0.0}s remaining)";
            }

            if (HasItem)
            {
                if (!interactor.IsHoldingItem)
                {
                    var info = IngredientDatabase.GetInfo(currentItem.IngredientType);
                    return $"Pick up {info.displayName} [E]";
                }

                // Table already has an item and player is holding one
                return "<color=#FFAA55>Table occupied! (Only 1 vegetable at a time)</color>";
            }

            if (!HasItem)
            {
                if (interactor.IsHoldingItem)
                {
                    IngredientType held = interactor.HeldItem.IngredientType;
                    if (held == IngredientType.Vegetables || held == IngredientType.Tomato ||
                        IngredientDatabase.CanPrepareAt(held, StationType.Table))
                    {
                        return "Chop Vegetables (2.0s) [E]";
                    }

                    return "<color=#FFAA55>Cannot chop this item (Vegetables only)</color>";
                }

                return "Chopping Table (Place Vegetables to chop)";
            }

            return "";
        }
    }
}
