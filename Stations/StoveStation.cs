using Kitchen;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Stations
{
    public class StoveStation : BaseStation
    {
        public enum SlotState
        {
            Empty,
            Cooking,
            Cooked,
            BurningWarning,
            Overcooked
        }

        public class StoveSlotData
        {
            public int index;
            public Transform holdPoint;
            public KitchenObject item;
            public SlotState state = SlotState.Empty;
            public float cookingTimer = 0f;
            public float burnTimer = 0f;

            // UI Elements
            public TMP_Text statusText;
            public TMP_Text timerText;
            public RectTransform fillBarRT;
            public Image fillBarImg;
            public Image cardBg;
        }

        private const float CookingDuration = 6.0f; // Exactly 6 seconds to cook as specified
        private const float BurnGraceDuration = 2.5f; // Safe period when cooked
        private const float BurnWarningDuration = 3.5f; // Warning period before turning charcoal/black
        private const float MaxSlotBarWidth = 100f;

        private StoveSlotData[] slots = new StoveSlotData[2];

        // Dual-Slot WorldSpace UI
        private GameObject stoveCanvasObj;
        private Camera mainCam;

        protected override void Awake()
        {
            base.Awake();
            mainCam = Camera.main;
            SetupSlots();
            CreateStoveUI();
        }

        private void SetupSlots()
        {
            Transform stovetop = transform.Find("Stovetop");
            Vector3 center = stovetop != null
                ? stovetop.position + Vector3.up * 0.35f
                : transform.position + Vector3.up * 1.0f;

            Vector3 rightDir = stovetop != null ? stovetop.right : transform.right;

            for (int i = 0; i < 2; i++)
            {
                GameObject hpObj = new GameObject($"StoveHoldPoint_{i + 1}");
                hpObj.transform.SetParent(transform);
                float offset = (i == 0) ? -0.55f : 0.55f;
                hpObj.transform.position = center + rightDir * offset;

                slots[i] = new StoveSlotData
                {
                    index = i,
                    holdPoint = hpObj.transform,
                    state = SlotState.Empty
                };
            }
        }

        private void CreateStoveUI()
        {
            Transform existing = transform.Find("StoveDualSlotUI");
            if (existing != null)
            {
                stoveCanvasObj = existing.gameObject;
                BindExistingStoveUI(existing);
                stoveCanvasObj.SetActive(true);
                return;
            }

            stoveCanvasObj = new GameObject("StoveDualSlotUI");
            stoveCanvasObj.transform.SetParent(transform);

            Transform stovetop = transform.Find("Stovetop");
            Vector3 center = stovetop != null
                ? stovetop.position + Vector3.up * 1.6f
                : transform.position + Vector3.up * 2.3f;
            stoveCanvasObj.transform.position = center;

            Canvas canvas = stoveCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            stoveCanvasObj.AddComponent<CanvasScaler>();

            RectTransform canvasRT = stoveCanvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(280f, 130f);
            stoveCanvasObj.transform.localScale = Vector3.one * 0.010f;

            // Main Background Panel
            GameObject mainBg = new GameObject("MainBg");
            mainBg.transform.SetParent(stoveCanvasObj.transform, false);
            Image mainBgImg = mainBg.AddComponent<Image>();
            mainBgImg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            RectTransform mainBgRT = mainBg.GetComponent<RectTransform>();
            mainBgRT.anchorMin = Vector2.zero;
            mainBgRT.anchorMax = Vector2.one;
            mainBgRT.sizeDelta = Vector2.zero;

            Outline outline = mainBg.AddComponent<Outline>();
            outline.effectColor = new Color(0.9f, 0.45f, 0.15f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Title Header
            GameObject titleObj = new GameObject("Header");
            titleObj.transform.SetParent(mainBg.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "<b><color=#FFA500>STOVE (2 SLOTS)</color></b>";
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontSize = 15;
            titleTMP.raycastTarget = false;

            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.76f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            // Slots Row Container
            GameObject rowObj = new GameObject("SlotsRow");
            rowObj.transform.SetParent(mainBg.transform, false);
            HorizontalLayoutGroup rowHLG = rowObj.AddComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 10f;
            rowHLG.padding = new RectOffset(10, 10, 6, 8);
            rowHLG.childAlignment = TextAnchor.MiddleCenter;
            rowHLG.childControlWidth = true;
            rowHLG.childControlHeight = true;

            RectTransform rowRT = rowObj.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0f, 0f);
            rowRT.anchorMax = new Vector2(1f, 0.78f);
            rowRT.offsetMin = Vector2.zero;
            rowRT.offsetMax = Vector2.zero;

            // Create Slot 1 and Slot 2 Cards
            for (int i = 0; i < 2; i++)
            {
                CreateSlotCard(rowObj.transform, slots[i]);
            }

            stoveCanvasObj.SetActive(true);
        }

        private void BindExistingStoveUI(Transform root)
        {
            for (int i = 0; i < 2; i++)
            {
                Transform card = root.Find($"MainBg/SlotsRow/SlotCard_{i + 1}")
                              ?? root.Find($"SlotsRow/SlotCard_{i + 1}")
                              ?? root.Find($"SlotCard_{i + 1}");

                if (card != null)
                {
                    slots[i].cardBg = card.GetComponent<Image>();
                    slots[i].timerText = card.Find("TimerText")?.GetComponent<TMP_Text>();
                    slots[i].statusText = card.Find("StatusText")?.GetComponent<TMP_Text>() ?? slots[i].timerText;

                    Transform track = card.Find("Track");
                    if (track != null)
                    {
                        Transform fill = track.Find("Fill");
                        if (fill != null)
                        {
                            slots[i].fillBarImg = fill.GetComponent<Image>();
                            slots[i].fillBarRT = fill.GetComponent<RectTransform>();
                        }
                    }
                }
            }
        }

        private void CreateSlotCard(Transform parent, StoveSlotData slot)
        {
            GameObject cardObj = new GameObject($"SlotCard_{slot.index + 1}");
            cardObj.transform.SetParent(parent, false);

            slot.cardBg = cardObj.AddComponent<Image>();
            slot.cardBg.color = new Color(0.16f, 0.19f, 0.25f, 0.9f);

            Outline cardOutline = cardObj.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.6f);
            cardOutline.effectDistance = new Vector2(1f, -1f);

            // Slot Title
            GameObject titleObj = new GameObject("SlotTitle");
            titleObj.transform.SetParent(cardObj.transform, false);
            TMP_Text titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = $"<b>SLOT {slot.index + 1}</b>";
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontSize = 13;
            titleTMP.color = new Color(0.85f, 0.85f, 0.85f);
            titleTMP.raycastTarget = false;

            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.68f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            // Timer & Status Text
            GameObject textObj = new GameObject("TimerText");
            textObj.transform.SetParent(cardObj.transform, false);
            slot.timerText = textObj.AddComponent<TextMeshProUGUI>();
            slot.timerText.text = "<color=#777777>Empty</color>";
            slot.timerText.alignment = TextAlignmentOptions.Center;
            slot.timerText.fontSize = 14;
            slot.timerText.lineSpacing = -10f;
            slot.timerText.raycastTarget = false;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0f, 0.22f);
            textRT.anchorMax = new Vector2(1f, 0.70f);
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            // Progress Track
            GameObject trackObj = new GameObject("Track");
            trackObj.transform.SetParent(cardObj.transform, false);
            Image trackImg = trackObj.AddComponent<Image>();
            trackImg.color = new Color(0.1f, 0.12f, 0.16f, 0.9f);

            RectTransform trackRT = trackObj.GetComponent<RectTransform>();
            trackRT.anchorMin = new Vector2(0.5f, 0.12f);
            trackRT.anchorMax = new Vector2(0.5f, 0.12f);
            trackRT.sizeDelta = new Vector2(MaxSlotBarWidth, 8f);

            // Progress Fill Bar
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(trackObj.transform, false);
            slot.fillBarImg = fillObj.AddComponent<Image>();
            slot.fillBarImg.color = Color.yellow;

            slot.fillBarRT = fillObj.GetComponent<RectTransform>();
            slot.fillBarRT.anchorMin = new Vector2(0f, 0f);
            slot.fillBarRT.anchorMax = new Vector2(0f, 1f);
            slot.fillBarRT.pivot = new Vector2(0f, 0.5f);
            slot.fillBarRT.sizeDelta = new Vector2(0f, 0f);
        }

        private void LateUpdate()
        {
            if (stoveCanvasObj != null)
            {
                if (mainCam == null) mainCam = Camera.main;
                if (mainCam != null)
                {
                    stoveCanvasObj.transform.rotation = mainCam.transform.rotation;
                }
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            // Update cooking and burning logic for both slots autonomously
            for (int i = 0; i < slots.Length; i++)
            {
                UpdateSlot(slots[i]);
            }
        }

        private void UpdateSlot(StoveSlotData slot)
        {
            if (slot.item == null)
            {
                slot.state = SlotState.Empty;
                slot.cookingTimer = 0f;
                slot.burnTimer = 0f;

                if (slot.timerText != null)
                {
                    slot.timerText.text = "<color=#666666>Empty</color>";
                }
                if (slot.fillBarRT != null)
                {
                    slot.fillBarRT.sizeDelta = new Vector2(0f, 0f);
                }
                return;
            }

            // State: Cooking (6 seconds to cook)
            if (slot.state == SlotState.Cooking)
            {
                slot.cookingTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(slot.cookingTimer / CookingDuration);
                float remaining = Mathf.Max(0f, CookingDuration - slot.cookingTimer);

                if (slot.timerText != null)
                {
                    slot.timerText.text = $"<b><color=#FFD700>{remaining:0.0}s</color></b>\n<size=11><color=#CCCCCC>Cooking</color></size>";
                }

                if (slot.fillBarRT != null)
                {
                    slot.fillBarRT.sizeDelta = new Vector2(MaxSlotBarWidth * progress, 0f);
                    slot.fillBarImg.color = Color.Lerp(Color.yellow, new Color(1f, 0.55f, 0f), progress);
                }

                if (slot.cookingTimer >= CookingDuration)
                {
                    // Finished cooking! Transform to Cooked Meat
                    slot.item.SetIngredientType(IngredientType.CookedPatty);
                    slot.state = SlotState.Cooked;
                    slot.burnTimer = 0f;
                }
            }
            // State: Cooked (Grace period)
            else if (slot.state == SlotState.Cooked)
            {
                slot.burnTimer += Time.deltaTime;

                if (slot.timerText != null)
                {
                    slot.timerText.text = "<b><color=#00FF66>READY!</color></b>\n<size=11><color=#FFFFFF>Pick Up [E]</color></size>";
                }

                if (slot.fillBarRT != null)
                {
                    slot.fillBarRT.sizeDelta = new Vector2(MaxSlotBarWidth, 0f);
                    slot.fillBarImg.color = new Color(0.2f, 0.95f, 0.35f);
                }

                if (slot.burnTimer >= BurnGraceDuration)
                {
                    slot.state = SlotState.BurningWarning;
                }
            }
            // State: BurningWarning (Flashing warning before turning black)
            else if (slot.state == SlotState.BurningWarning)
            {
                slot.burnTimer += Time.deltaTime;
                float totalBurnTime = BurnGraceDuration + BurnWarningDuration;
                float remainingBurn = Mathf.Max(0f, totalBurnTime - slot.burnTimer);

                if (slot.timerText != null)
                {
                    bool flash = (Mathf.FloorToInt(Time.time * 4f) % 2) == 0;
                    string colorTag = flash ? "<color=#FF2200>" : "<color=#FFAA00>";
                    slot.timerText.text = $"{colorTag}<b>BURNING!</b></color>\n<size=11>{remainingBurn:0.0}s left!</size>";
                }

                if (slot.fillBarRT != null)
                {
                    slot.fillBarImg.color = Color.red;
                }

                if (slot.burnTimer >= totalBurnTime)
                {
                    // Overcooked / Burned completely!
                    slot.item.SetIngredientType(IngredientType.Overcooked);
                    slot.state = SlotState.Overcooked;
                }
            }
            // State: Overcooked (Charcoal black, must move to trash)
            else if (slot.state == SlotState.Overcooked)
            {
                if (slot.timerText != null)
                {
                    slot.timerText.text = "<b><color=#FF2222>BURNT!</color></b>\n<size=10><color=#FF8888>Move to Trash</color></size>";
                }

                if (slot.fillBarRT != null)
                {
                    slot.fillBarRT.sizeDelta = new Vector2(MaxSlotBarWidth, 0f);
                    slot.fillBarImg.color = new Color(0.4f, 0.1f, 0.1f);
                }
            }
        }

        public override bool CanInteract(PlayerInteractor interactor)
        {
            if (interactor == null) return false;

            // Empty-handed player can pick up cooked or overcooked items
            if (!interactor.IsHoldingItem)
            {
                return HasPickableItem();
            }

            // Player holding raw meat can place it if any slot is empty
            if (interactor.IsHoldingItem)
            {
                if (interactor.HeldItem.IngredientType == IngredientType.Meat)
                {
                    return HasEmptySlot();
                }
            }

            return false;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor)) return;

            // Case 1: Pick up item from stove (empty hands)
            if (!interactor.IsHoldingItem)
            {
                StoveSlotData pickSlot = GetSlotToPick();
                if (pickSlot != null && pickSlot.item != null)
                {
                    KitchenObject itemToTake = pickSlot.item;
                    pickSlot.item = null;
                    pickSlot.state = SlotState.Empty;
                    pickSlot.cookingTimer = 0f;
                    pickSlot.burnTimer = 0f;

                    interactor.HoldItem(itemToTake);
                }
                return;
            }

            // Case 2: Place raw meat into first empty slot (takes 6s to cook)
            if (interactor.IsHoldingItem && interactor.HeldItem.IngredientType == IngredientType.Meat)
            {
                StoveSlotData emptySlot = GetFirstEmptySlot();
                if (emptySlot != null)
                {
                    KitchenObject droppedItem = interactor.DropItem();
                    emptySlot.item = droppedItem;
                    droppedItem.transform.SetParent(emptySlot.holdPoint);
                    droppedItem.transform.localPosition = Vector3.zero;
                    droppedItem.transform.localRotation = Quaternion.identity;

                    emptySlot.state = SlotState.Cooking;
                    emptySlot.cookingTimer = 0f;
                    emptySlot.burnTimer = 0f;
                }
            }
        }

        public override string GetInteractionPrompt(PlayerInteractor interactor)
        {
            if (interactor == null) return "Stove";

            if (interactor.IsHoldingItem)
            {
                if (interactor.HeldItem.IngredientType == IngredientType.Meat)
                {
                    if (HasEmptySlot())
                    {
                        int emptyIndex = GetFirstEmptySlot().index + 1;
                        return $"Cook Meat on Stove (Slot {emptyIndex}) [E]";
                    }
                    return "<color=#FFAA55>Stove is Full! (2/2 slots occupied)</color>";
                }

                if (interactor.HeldItem.IngredientType == IngredientType.Overcooked)
                {
                    return "<color=#FF4444>Overcooked food! Take to Trash</color>";
                }

                return "<color=#FFAA55>Cannot cook this item (Meat only)</color>";
            }

            // Empty hands: evaluate pickable items
            StoveSlotData pickSlot = GetSlotToPick();
            if (pickSlot != null)
            {
                int slotNum = pickSlot.index + 1;
                if (pickSlot.state == SlotState.Overcooked)
                {
                    return $"<color=#FF4444>Pick up Overcooked Meat (Slot {slotNum}) [E]</color>";
                }
                if (pickSlot.state == SlotState.BurningWarning)
                {
                    return $"<color=#FF8800>BURNING! Pick up Cooked Meat (Slot {slotNum}) [E]</color>";
                }
                if (pickSlot.state == SlotState.Cooked)
                {
                    return $"<color=#00FF66>Pick up Cooked Meat (Slot {slotNum}) [E]</color>";
                }
            }

            // Check if any slot is currently cooking
            if (IsAnySlotCooking(out float minRemaining))
            {
                return $"Stove: Meat cooking... ({minRemaining:0.0}s remaining)";
            }

            return "Stove (2 Slots Empty - Place Meat to Cook)";
        }

        private bool HasEmptySlot()
        {
            return slots[0].item == null || slots[1].item == null;
        }

        private StoveSlotData GetFirstEmptySlot()
        {
            if (slots[0].item == null) return slots[0];
            if (slots[1].item == null) return slots[1];
            return null;
        }

        private bool HasPickableItem()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].item != null && (slots[i].state == SlotState.Cooked ||
                                              slots[i].state == SlotState.BurningWarning ||
                                              slots[i].state == SlotState.Overcooked))
                {
                    return true;
                }
            }
            return false;
        }

        private StoveSlotData GetSlotToPick()
        {
            // Priority 1: Overcooked item (clear burnt food)
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].item != null && slots[i].state == SlotState.Overcooked)
                {
                    return slots[i];
                }
            }

            // Priority 2: Burning item (urgent save)
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].item != null && slots[i].state == SlotState.BurningWarning)
                {
                    return slots[i];
                }
            }

            // Priority 3: Cooked item (ready to serve)
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].item != null && slots[i].state == SlotState.Cooked)
                {
                    return slots[i];
                }
            }

            return null;
        }

        private bool IsAnySlotCooking(out float minRemaining)
        {
            minRemaining = float.MaxValue;
            bool anyCooking = false;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].item != null && slots[i].state == SlotState.Cooking)
                {
                    float rem = Mathf.Max(0f, CookingDuration - slots[i].cookingTimer);
                    if (rem < minRemaining)
                    {
                        minRemaining = rem;
                    }
                    anyCooking = true;
                }
            }

            return anyCooking;
        }
    }
}
