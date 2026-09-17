using System.Collections;
using System.Collections.Generic;
using System.Text;
using Kitchen;
using Orders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Stations
{
    public class CustomerWindowStation : BaseStation
    {
        [Header("Window Configuration")]
        [SerializeField] private int windowIndex = 0;
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private Vector3 customerSpawnOffset = new Vector3(2.5f, 0f, 0f);
        [SerializeField] private float respawnDelay = 5f;

        private Order currentOrder;
        private GameObject spawnedCustomer;
        private Coroutine respawnCoroutine;

        // Plate & Placed Items
        private GameObject plateObj;
        private Transform plateItemHolder;
        private List<KitchenObject> itemsOnPlate = new List<KitchenObject>();

        // Small Order Panel & Structured Rows
        private GameObject orderPanelObj;
        private TMP_Text headerTMP;
        private GameObject rowsContainerObj;
        private GameObject[] ingredientRowObjs = new GameObject[4];
        private Image[] ingredientBoxImages = new Image[4];
        private Outline[] ingredientBoxOutlines = new Outline[4];
        private GameObject[] ingredientCheckmarkObjs = new GameObject[4];
        private TMP_Text[] ingredientRowTMPs = new TMP_Text[4];
        private TMP_Text footerTMP;
        private TMP_Text orderTMP;

        private PlayerInteractor cachedPlayer;
        private KitchenObject lastKnownHeldItem;

        // Floating Score Popup (+17 or -6)
        private GameObject scorePopupObj;
        private TMP_Text scorePopupTMP;
        private Coroutine scorePopupCoroutine;
        private int lastDisplayedSecond = -1;

        private Camera mainCam;

        // Procedural UI Sprites (Guaranteed to render on every machine without missing font glyphs)
        private static Sprite s_CheckmarkSprite;
        private static Sprite s_UncheckedSprite;
        private static Sprite s_InHandSprite;

        public static Sprite GetCheckmarkSprite()
        {
            if (s_CheckmarkSprite != null) return s_CheckmarkSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 31.5f;
                    float dy = y - 31.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Stroke distance to checkmark line segments: (18, 30) -> (28, 18) -> (48, 44)
                    float d1 = DistToSegment(x, y, 18f, 30f, 28f, 18f);
                    float d2 = DistToSegment(x, y, 28f, 18f, 48f, 44f);
                    float strokeDist = Mathf.Min(d1, d2);

                    Color c = Color.clear;
                    if (dist <= 28f)
                    {
                        if (strokeDist <= 3.4f)
                        {
                            c = Color.white;
                        }
                        else if (dist > 25f)
                        {
                            c = new Color(0.04f, 0.55f, 0.18f, 0.98f);
                        }
                        else
                        {
                            c = new Color(0.08f, 0.85f, 0.28f, 0.98f);
                        }
                    }

                    cols[y * size + x] = c;
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            s_CheckmarkSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return s_CheckmarkSprite;
        }

        public static Sprite GetUncheckedSprite()
        {
            if (s_UncheckedSprite != null) return s_UncheckedSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool insideSquare = (x >= 12 && x <= 52 && y >= 12 && y <= 52);
                    bool onBorder = insideSquare && (x <= 16 || x >= 48 || y <= 16 || y >= 48);

                    Color c = Color.clear;
                    if (onBorder)
                    {
                        c = new Color(0.55f, 0.65f, 0.78f, 0.95f);
                    }
                    else if (insideSquare)
                    {
                        c = new Color(0.10f, 0.13f, 0.18f, 0.75f);
                    }

                    cols[y * size + x] = c;
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            s_UncheckedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return s_UncheckedSprite;
        }

        public static Sprite GetInHandSprite()
        {
            if (s_InHandSprite != null) return s_InHandSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 31.5f;
                    float dy = y - 31.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    Color c = Color.clear;
                    if (dist <= 28f)
                    {
                        bool isArrow = (x >= 20 && x <= 36 && Mathf.Abs(y - 31.5f) <= 3.2f) ||
                                       (x >= 30 && Mathf.Abs(y - 31.5f) <= (44 - x) * 1.2f && x <= 44);

                        if (isArrow)
                        {
                            c = new Color(0.1f, 0.1f, 0.1f, 1f);
                        }
                        else if (dist > 25f)
                        {
                            c = new Color(0.85f, 0.65f, 0.05f, 1f);
                        }
                        else
                        {
                            c = new Color(1.0f, 0.85f, 0.15f, 1f);
                        }
                    }

                    cols[y * size + x] = c;
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            s_InHandSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return s_InHandSprite;
        }

        private static float DistToSegment(float px, float py, float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            float l2 = dx * dx + dy * dy;
            if (l2 <= 0.0001f)
            {
                float ex = px - x1;
                float ey = py - y1;
                return Mathf.Sqrt(ex * ex + ey * ey);
            }
            float t = Mathf.Clamp01(((px - x1) * dx + (py - y1) * dy) / l2);
            float projx = x1 + t * dx;
            float projy = y1 + t * dy;
            float rx = px - projx;
            float ry = py - projy;
            return Mathf.Sqrt(rx * rx + ry * ry);
        }

        public int WindowIndex => windowIndex;
        public Order CurrentOrder => currentOrder;
        public bool HasActiveOrder => currentOrder != null && !currentOrder.IsComplete;

        protected override void Awake()
        {
            base.Awake();
            mainCam = Camera.main;
            CreatePlate();
            CreateSmallOrderPanel();
            CreateScorePopup();
        }

        public void SetWindowIndex(int index)
        {
            windowIndex = index;
            if (orderPanelObj != null)
            {
                orderPanelObj.name = $"OrderPanel_Win_{windowIndex}";
            }
            if (plateObj != null)
            {
                plateObj.name = $"Plate_Window_{windowIndex}";
            }
            UpdateDisplay();
        }

        public void SetCustomerPrefab(GameObject prefab)
        {
            customerPrefab = prefab;
        }

        public static GameObject CreateVectorCheckmark(GameObject boxObj)
        {
            if (boxObj == null) return null;

            Transform existing = boxObj.transform.Find("Checkmark");
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject checkmarkObj = new GameObject("Checkmark");
            checkmarkObj.transform.SetParent(boxObj.transform, false);
            RectTransform cmRT = checkmarkObj.AddComponent<RectTransform>();
            cmRT.anchorMin = new Vector2(0.5f, 0.5f);
            cmRT.anchorMax = new Vector2(0.5f, 0.5f);
            cmRT.sizeDelta = new Vector2(22f, 22f);
            cmRT.localPosition = Vector3.zero;

            // Short stroke
            GameObject strokeShort = new GameObject("StrokeShort");
            strokeShort.transform.SetParent(checkmarkObj.transform, false);
            Image ssImg = strokeShort.AddComponent<Image>();
            ssImg.color = Color.white;
            ssImg.raycastTarget = false;
            RectTransform ssRT = strokeShort.GetComponent<RectTransform>();
            ssRT.anchorMin = new Vector2(0.5f, 0.5f);
            ssRT.anchorMax = new Vector2(0.5f, 0.5f);
            ssRT.pivot = new Vector2(0.5f, 0.5f);
            ssRT.sizeDelta = new Vector2(3f, 8f);
            ssRT.localPosition = new Vector3(-3.5f, -1.5f, 0f);
            ssRT.localEulerAngles = new Vector3(0f, 0f, -42f);

            // Long stroke
            GameObject strokeLong = new GameObject("StrokeLong");
            strokeLong.transform.SetParent(checkmarkObj.transform, false);
            Image slImg = strokeLong.AddComponent<Image>();
            slImg.color = Color.white;
            slImg.raycastTarget = false;
            RectTransform slRT = strokeLong.GetComponent<RectTransform>();
            slRT.anchorMin = new Vector2(0.5f, 0.5f);
            slRT.anchorMax = new Vector2(0.5f, 0.5f);
            slRT.pivot = new Vector2(0.5f, 0.5f);
            slRT.sizeDelta = new Vector2(3f, 15f);
            slRT.localPosition = new Vector3(2.5f, 1.5f, 0f);
            slRT.localEulerAngles = new Vector3(0f, 0f, 42f);

            checkmarkObj.SetActive(false);
            return checkmarkObj;
        }

        private void CreatePlate()
        {
            if (plateObj == null)
            {
                Transform existing = transform.Find($"Plate_Window_{windowIndex}") ?? transform.Find("Plate");
                if (existing != null)
                {
                    plateObj = existing.gameObject;
                }
                else
                {
                    // Plate sits on the kitchen counter in front of the window
                    plateObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    plateObj.name = $"Plate_Window_{windowIndex}";
                    plateObj.transform.SetParent(transform, false);

                    plateObj.transform.localPosition = new Vector3(-0.9f, 0.45f, 0f);
                    plateObj.transform.localScale = new Vector3(1.3f, 0.06f, 1.3f);

                    Collider col = plateObj.GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    Material plateMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    plateMat.color = new Color(0.96f, 0.96f, 0.98f);
                    var rend = plateObj.GetComponent<MeshRenderer>();
                    if (rend != null) rend.material = plateMat;
                }
            }

            if (plateItemHolder == null && plateObj != null)
            {
                // Create plateItemHolder as unparented root object so items retain pure (1,1,1) world scale
                GameObject holder = new GameObject($"PlateItems_Win_{windowIndex}");
                holder.transform.SetParent(null);
                holder.transform.position = plateObj.transform.position + new Vector3(0f, 0.08f, 0f);
                holder.transform.rotation = Quaternion.identity;
                holder.transform.localScale = Vector3.one;
                plateItemHolder = holder.transform;
            }
        }

        private static TMP_FontAsset osFallbackFont;

        public static void EnsureFontSupportsCheckmark(TMP_Text tmp)
        {
            if (tmp == null || tmp.font == null) return;

            if (osFallbackFont == null)
            {
                string[] fallbackFamilies = new string[] { "Segoe UI Symbol", "Segoe UI", "Arial", "Calibri" };
                Font font = Font.CreateDynamicFontFromOSFont(fallbackFamilies, 36);
                if (font != null)
                {
                    osFallbackFont = TMP_FontAsset.CreateFontAsset(font);
                }
            }

            if (osFallbackFont != null)
            {
                if (tmp.font.fallbackFontAssetTable == null)
                {
                    tmp.font.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }
                if (!tmp.font.fallbackFontAssetTable.Contains(osFallbackFont))
                {
                    tmp.font.fallbackFontAssetTable.Add(osFallbackFont);
                }
            }
        }

        private void CreateSmallOrderPanel()
        {
            if (orderPanelObj != null) return;

            Transform existing = transform.Find("OrderPanel") ?? transform.Find($"OrderPanel_Win_{windowIndex}");
            if (existing != null)
            {
                orderPanelObj = existing.gameObject;
                SetupOrBindOrderPanel(orderPanelObj);
                orderPanelObj.SetActive(true);
                return;
            }

            orderPanelObj = new GameObject($"OrderPanel_Win_{windowIndex}");
            orderPanelObj.transform.SetParent(transform, false);

            Vector3 parentScale = transform.lossyScale;
            orderPanelObj.transform.localPosition = new Vector3(
                -2.2f / Mathf.Max(parentScale.x, 0.01f),
                3.2f / Mathf.Max(parentScale.y, 0.01f),
                0f
            );
            orderPanelObj.transform.localEulerAngles = new Vector3(90f, 0f, 180f);

            float targetScale = 0.020f;
            orderPanelObj.transform.localScale = new Vector3(
                targetScale / Mathf.Max(parentScale.x, 0.01f),
                targetScale / Mathf.Max(parentScale.z, 0.01f),
                targetScale / Mathf.Max(parentScale.y, 0.01f)
            );

            // World Space Canvas for the panel
            Canvas canvas = orderPanelObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCam != null ? mainCam : Camera.main;
            orderPanelObj.AddComponent<CanvasScaler>();

            RectTransform canvasRT = orderPanelObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(300f, 220f);

            BuildPanelContents(orderPanelObj);
            orderPanelObj.SetActive(true);
        }

        private void SetupOrBindOrderPanel(GameObject panelObj)
        {
            Transform cardBg = panelObj.transform.Find("CardBackground") ?? panelObj.transform.Find("Background");
            Transform rowsT = cardBg != null ? cardBg.Find("RowsContainer") : null;

            if (cardBg != null && rowsT != null)
            {
                Transform headerT = cardBg.Find("Header");
                if (headerT != null)
                {
                    headerTMP = headerT.GetComponent<TextMeshProUGUI>();
                    orderTMP = headerTMP;
                }

                rowsContainerObj = rowsT.gameObject;
                for (int i = 0; i < 4; i++)
                {
                    Transform rowT = rowsT.Find($"IngredientRow_{i}");
                    if (rowT != null)
                    {
                        ingredientRowObjs[i] = rowT.gameObject;

                        Transform boxT = rowT.Find("Checkbox") ?? rowT.Find("CheckIcon");
                        if (boxT != null)
                        {
                            ingredientBoxImages[i] = boxT.GetComponent<Image>();
                            ingredientBoxOutlines[i] = boxT.GetComponent<Outline>();
                            if (ingredientBoxOutlines[i] == null)
                            {
                                ingredientBoxOutlines[i] = boxT.gameObject.AddComponent<Outline>();
                                ingredientBoxOutlines[i].effectDistance = new Vector2(1.5f, -1.5f);
                            }

                            Transform cmT = boxT.Find("Checkmark");
                            if (cmT != null)
                            {
                                ingredientCheckmarkObjs[i] = cmT.gameObject;
                            }
                            else
                            {
                                ingredientCheckmarkObjs[i] = CreateVectorCheckmark(boxT.gameObject);
                            }
                        }

                        ingredientRowTMPs[i] = rowT.Find("Text")?.GetComponent<TextMeshProUGUI>();
                    }
                }

                Transform footerT = cardBg.Find("Footer");
                if (footerT != null)
                {
                    footerTMP = footerT.GetComponent<TextMeshProUGUI>();
                }
            }
            else
            {
                // Rebuild clean content inside existing panelObj
                for (int i = panelObj.transform.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(panelObj.transform.GetChild(i).gameObject);
                }
                BuildPanelContents(panelObj);
            }
        }

        private void BuildPanelContents(GameObject rootObj)
        {
            // Card Background Image
            GameObject cardBg = new GameObject("CardBackground");
            cardBg.transform.SetParent(rootObj.transform, false);
            Image bgImg = cardBg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.11f, 0.16f, 0.96f);

            RectTransform bgRT = cardBg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            Outline outline = cardBg.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.82f, 0.18f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            // Header Text (Order # and Timer / Score)
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(cardBg.transform, false);
            headerTMP = headerObj.AddComponent<TextMeshProUGUI>();
            headerTMP.alignment = TextAlignmentOptions.Center;
            headerTMP.fontSize = 18;
            headerTMP.lineSpacing = -2f;
            headerTMP.raycastTarget = false;
            EnsureFontSupportsCheckmark(headerTMP);

            RectTransform headerRT = headerObj.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0f, 0.65f);
            headerRT.anchorMax = new Vector2(1f, 1f);
            headerRT.offsetMin = new Vector2(8f, 0f);
            headerRT.offsetMax = new Vector2(-8f, -6f);

            orderTMP = headerTMP;

            // Rows Container
            rowsContainerObj = new GameObject("RowsContainer");
            rowsContainerObj.transform.SetParent(cardBg.transform, false);
            VerticalLayoutGroup vlg = rowsContainerObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(12, 12, 0, 0);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            RectTransform rowsRT = rowsContainerObj.GetComponent<RectTransform>();
            rowsRT.anchorMin = new Vector2(0f, 0.18f);
            rowsRT.anchorMax = new Vector2(1f, 0.65f);
            rowsRT.offsetMin = Vector2.zero;
            rowsRT.offsetMax = Vector2.zero;

            // Create 4 Ingredient Rows with Vector Checkboxes
            for (int i = 0; i < 4; i++)
            {
                GameObject rowObj = new GameObject($"IngredientRow_{i}");
                rowObj.transform.SetParent(rowsContainerObj.transform, false);

                HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10f;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                RectTransform rowRT = rowObj.GetComponent<RectTransform>();
                rowRT.sizeDelta = new Vector2(280f, 26f);

                // 1. Checkbox Box
                GameObject boxObj = new GameObject("Checkbox");
                boxObj.transform.SetParent(rowObj.transform, false);
                Image boxImg = boxObj.AddComponent<Image>();
                boxImg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
                boxImg.raycastTarget = false;

                Outline boxOutline = boxObj.AddComponent<Outline>();
                boxOutline.effectColor = new Color(0.55f, 0.65f, 0.78f, 0.85f);
                boxOutline.effectDistance = new Vector2(1.5f, -1.5f);

                RectTransform boxRT = boxObj.GetComponent<RectTransform>();
                boxRT.sizeDelta = new Vector2(22f, 22f);

                // Checkmark vector inside the box
                GameObject checkmarkObj = CreateVectorCheckmark(boxObj);

                // 2. Ingredient Text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(rowObj.transform, false);
                TextMeshProUGUI rowTMP = textObj.AddComponent<TextMeshProUGUI>();
                rowTMP.alignment = TextAlignmentOptions.MidlineLeft;
                rowTMP.fontSize = 16;
                rowTMP.raycastTarget = false;
                EnsureFontSupportsCheckmark(rowTMP);

                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.sizeDelta = new Vector2(245f, 24f);

                ingredientRowObjs[i] = rowObj;
                ingredientBoxImages[i] = boxImg;
                ingredientBoxOutlines[i] = boxOutline;
                ingredientCheckmarkObjs[i] = checkmarkObj;
                ingredientRowTMPs[i] = rowTMP;
            }

            // Footer Text
            GameObject footerObj = new GameObject("Footer");
            footerObj.transform.SetParent(cardBg.transform, false);
            footerTMP = footerObj.AddComponent<TextMeshProUGUI>();
            footerTMP.alignment = TextAlignmentOptions.Center;
            footerTMP.fontSize = 14;
            footerTMP.raycastTarget = false;
            EnsureFontSupportsCheckmark(footerTMP);

            RectTransform footerRT = footerObj.GetComponent<RectTransform>();
            footerRT.anchorMin = new Vector2(0f, 0f);
            footerRT.anchorMax = new Vector2(1f, 0.18f);
            footerRT.offsetMin = new Vector2(8f, 4f);
            footerRT.offsetMax = new Vector2(-8f, 0f);
        }

        private void CreateScorePopup()
        {
            if (scorePopupObj != null) return;

            scorePopupObj = new GameObject($"ScorePopup_Win_{windowIndex}");
            scorePopupObj.transform.SetParent(null);
            scorePopupObj.transform.position = transform.position + new Vector3(-2.2f, 3.8f, 0f);

            scorePopupTMP = scorePopupObj.AddComponent<TextMeshPro>();
            scorePopupTMP.alignment = TextAlignmentOptions.Center;
            scorePopupTMP.fontSize = 11f;
            scorePopupTMP.outlineColor = Color.black;
            scorePopupTMP.outlineWidth = 0.35f;
            EnsureFontSupportsCheckmark(scorePopupTMP);
            scorePopupObj.SetActive(false);
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (HasActiveOrder)
            {
                currentOrder.timeElapsed += Time.deltaTime;
                int curSec = Mathf.FloorToInt(currentOrder.timeElapsed);

                if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerInteractor>();
                KitchenObject currentHeld = (cachedPlayer != null && cachedPlayer.IsHoldingItem) ? cachedPlayer.HeldItem : null;

                if (curSec != lastDisplayedSecond || currentHeld != lastKnownHeldItem)
                {
                    lastDisplayedSecond = curSec;
                    lastKnownHeldItem = currentHeld;
                    UpdateDisplay();
                }
            }
        }

        private void LateUpdate()
        {
            if (orderPanelObj != null && orderPanelObj.activeSelf)
            {
                orderPanelObj.transform.localEulerAngles = new Vector3(90f, 0f, 180f);
            }
            if (scorePopupObj != null && scorePopupObj.activeSelf)
            {
                if (mainCam == null) mainCam = Camera.main;
                if (mainCam != null)
                {
                    scorePopupObj.transform.rotation = mainCam.transform.rotation;
                }
            }
        }

        private void OnDestroy()
        {
            if (orderPanelObj != null) Destroy(orderPanelObj);
            if (scorePopupObj != null) Destroy(scorePopupObj);
            if (plateItemHolder != null) Destroy(plateItemHolder.gameObject);
        }

        public void AssignNewOrder(Order order)
        {
            currentOrder = order;
            lastDisplayedSecond = -1;
            ClearItemsOnPlate();
            if (plateObj != null) plateObj.SetActive(true);
            if (plateItemHolder != null) plateItemHolder.gameObject.SetActive(true);

            SpawnCustomer();
            UpdateDisplay();
        }

        private void SpawnCustomer()
        {
            if (spawnedCustomer != null)
            {
                Destroy(spawnedCustomer);
            }

            Vector3 spawnPos = transform.position + customerSpawnOffset;

            if (customerPrefab != null)
            {
                spawnedCustomer = Instantiate(customerPrefab, spawnPos, Quaternion.Euler(0f, -90f, 0f));
            }
            else
            {
                spawnedCustomer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spawnedCustomer.name = $"Customer_Window_{windowIndex}";
                spawnedCustomer.transform.position = spawnPos;
                spawnedCustomer.transform.localScale = new Vector3(0.8f, 1.8f, 0.8f);
                var rend = spawnedCustomer.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.2f, 0.6f, 0.9f);
                }
            }
        }

        private void RemoveCustomer()
        {
            if (spawnedCustomer != null)
            {
                Destroy(spawnedCustomer);
                spawnedCustomer = null;
            }
        }

        public void UpdateDisplay()
        {
            if (headerTMP == null && orderTMP == null) return;

            if (currentOrder == null)
            {
                if (headerTMP != null) headerTMP.text = "<size=21><b><color=#888888>Waiting for\nCustomer...</color></b></size>";
                if (rowsContainerObj != null) rowsContainerObj.SetActive(false);
                if (footerTMP != null) footerTMP.text = "";
                return;
            }

            if (rowsContainerObj != null) rowsContainerObj.SetActive(true);

            int seconds = Mathf.FloorToInt(currentOrder.timeElapsed);
            int currentPotentialScore = currentOrder.GetFinalScore();
            string timerColor = seconds < 15 ? "#00FF66" : (seconds < 30 ? "#FFAA00" : "#FF3333");
            string scorePrefix = currentPotentialScore >= 0 ? "+" : "";

            if (headerTMP != null)
            {
                headerTMP.text = $"<b><size=20><color=#FFD700>ORDER #{currentOrder.id}</color></size></b>\n<size=15>Timer: <color={timerColor}><b>{seconds}s</b></color>  |  Score: <b>{scorePrefix}{currentPotentialScore}</b></size>\n<size=11><color=#556677>────────────────────────</color></size>";
            }

            // Check if player currently holds a matching ingredient in hand
            if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerInteractor>();
            KitchenObject held = (cachedPlayer != null && cachedPlayer.IsHoldingItem) ? cachedPlayer.HeldItem : null;
            IngredientType heldType = held != null ? held.IngredientType : IngredientType.None;

            int reqCount = currentOrder.requiredIngredients != null ? currentOrder.requiredIngredients.Count : 0;

            for (int i = 0; i < ingredientRowObjs.Length; i++)
            {
                if (ingredientRowObjs[i] == null) continue;

                if (i < reqCount)
                {
                    ingredientRowObjs[i].SetActive(true);
                    var req = currentOrder.requiredIngredients[i];
                    bool fulfilled = currentOrder.IsFulfilled(i);
                    string dispName = Order.GetOrderDisplayName(req);
                    int scoreVal = Order.GetIngredientScoreValue(req);

                    if (fulfilled)
                    {
                        // 1. DELIVERED / COLLECTED: Vibrant green box + bright green outline + vector checkmark!
                        if (ingredientBoxImages[i] != null)
                        {
                            ingredientBoxImages[i].sprite = null;
                            ingredientBoxImages[i].color = new Color(0.06f, 0.72f, 0.22f, 1f);
                        }
                        if (ingredientBoxOutlines[i] != null)
                        {
                            ingredientBoxOutlines[i].effectColor = new Color(0.2f, 1.0f, 0.45f, 1f);
                            ingredientBoxOutlines[i].effectDistance = new Vector2(2f, -2f);
                        }
                        if (ingredientCheckmarkObjs[i] != null)
                        {
                            ingredientCheckmarkObjs[i].SetActive(true);
                        }
                        if (ingredientRowTMPs[i] != null)
                        {
                            ingredientRowTMPs[i].text = $"<b><color=#00FF66>{dispName}</color></b> <size=13><color=#88FFB0>(+{scoreVal})</color></size> <size=11><color=#00FFAA><b>COLLECTED</b></color></size>";
                        }
                    }
                    else if (heldType != IngredientType.None && Order.MatchesRequirement(req, heldType))
                    {
                        // 2. IN HAND: Ingredient in player's hand! Vibrant green box + checkmark + prompt [E]
                        if (ingredientBoxImages[i] != null)
                        {
                            ingredientBoxImages[i].sprite = null;
                            ingredientBoxImages[i].color = new Color(0.10f, 0.65f, 0.25f, 1f);
                        }
                        if (ingredientBoxOutlines[i] != null)
                        {
                            ingredientBoxOutlines[i].effectColor = new Color(0.2f, 1.0f, 0.45f, 1f);
                            ingredientBoxOutlines[i].effectDistance = new Vector2(2f, -2f);
                        }
                        if (ingredientCheckmarkObjs[i] != null)
                        {
                            ingredientCheckmarkObjs[i].SetActive(true);
                        }
                        if (ingredientRowTMPs[i] != null)
                        {
                            ingredientRowTMPs[i].text = $"<b><color=#FFE066>{dispName}</color></b> <size=13><color=#FFE066>(+{scoreVal})</color></size> <size=11><color=#FFD700><b>IN HAND [E]</b></color></size>";
                        }
                    }
                    else
                    {
                        // 3. PENDING: Dark slate box + silver outline + checkmark hidden
                        if (ingredientBoxImages[i] != null)
                        {
                            ingredientBoxImages[i].sprite = null;
                            ingredientBoxImages[i].color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
                        }
                        if (ingredientBoxOutlines[i] != null)
                        {
                            ingredientBoxOutlines[i].effectColor = new Color(0.55f, 0.65f, 0.78f, 0.85f);
                            ingredientBoxOutlines[i].effectDistance = new Vector2(1.5f, -1.5f);
                        }
                        if (ingredientCheckmarkObjs[i] != null)
                        {
                            ingredientCheckmarkObjs[i].SetActive(false);
                        }
                        if (ingredientRowTMPs[i] != null)
                        {
                            ingredientRowTMPs[i].text = $"<b><color=#DDE5EE>{dispName}</color></b> <size=13><color=#8899AA>(+{scoreVal})</color></size>";
                        }
                    }
                }
                else
                {
                    ingredientRowObjs[i].SetActive(false);
                }
            }

            if (footerTMP != null)
            {
                if (currentOrder.IsComplete)
                {
                    footerTMP.text = $"<size=15><color=#00FF66><b>[ ALL COLLECTED! (+{currentPotentialScore}) ]</b></color></size>";
                }
                else
                {
                    footerTMP.text = "";
                }
            }
        }

        public override bool CanInteract(PlayerInteractor interactor)
        {
            if (!HasActiveOrder) return false;
            if (!interactor.IsHoldingItem) return false;
            if (interactor.HeldItem.IngredientType == IngredientType.Overcooked) return false;

            return currentOrder.NeedsIngredient(interactor.HeldItem.IngredientType);
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor)) return;

            IngredientType heldType = interactor.HeldItem.IngredientType;

            if (currentOrder.TryDeliverIngredient(heldType))
            {
                // Take item from player and place it onto the Plate!
                KitchenObject deliveredItem = interactor.DropItem();
                if (deliveredItem != null)
                {
                    PlaceItemOnPlate(deliveredItem);
                }

                UpdateDisplay();

                // Check if order is complete
                if (currentOrder.IsComplete)
                {
                    CompleteOrder();
                }
            }
        }

        private void PlaceItemOnPlate(KitchenObject item)
        {
            if (plateItemHolder == null)
            {
                CreatePlate();
            }

            item.transform.SetParent(plateItemHolder);

            int index = itemsOnPlate.Count;
            Vector3 offset;
            switch (index)
            {
                case 0:
                    offset = new Vector3(-0.20f, 0.08f, 0f);
                    break;
                case 1:
                    offset = new Vector3(0.20f, 0.08f, 0f);
                    break;
                case 2:
                default:
                    offset = new Vector3(0f, 0.08f, 0.20f);
                    break;
            }

            item.transform.localPosition = offset;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

            itemsOnPlate.Add(item);
        }

        private void ClearItemsOnPlate()
        {
            foreach (var item in itemsOnPlate)
            {
                if (item != null)
                {
                    item.DestroySelf();
                }
            }
            itemsOnPlate.Clear();
        }

        private void CompleteOrder()
        {
            int earnedScore = currentOrder.GetFinalScore();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(earnedScore);
            }

            ShowScorePopup(earnedScore);

            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
            }

            respawnCoroutine = StartCoroutine(RespawnTimerRoutine(earnedScore));
        }

        private void ShowScorePopup(int earnedScore)
        {
            if (scorePopupCoroutine != null)
            {
                StopCoroutine(scorePopupCoroutine);
            }

            scorePopupCoroutine = StartCoroutine(ScorePopupRoutine(earnedScore));
        }

        private IEnumerator ScorePopupRoutine(int earnedScore)
        {
            if (scorePopupObj == null || scorePopupTMP == null) yield break;

            scorePopupObj.SetActive(true);
            scorePopupObj.transform.localPosition = new Vector3(-0.9f, 2.7f, 0f);

            if (earnedScore >= 0)
            {
                scorePopupTMP.text = $"<b><color=#00FF66>+{earnedScore} PTS</color></b>";
            }
            else
            {
                scorePopupTMP.text = $"<b><color=#FF3333>{earnedScore}</color></b>";
            }

            float elapsed = 0f;
            float duration = 2.5f;
            Vector3 startPos = new Vector3(-0.9f, 2.7f, 0f);
            Vector3 endPos = new Vector3(-0.9f, 3.8f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                scorePopupObj.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                Color c = scorePopupTMP.color;
                c.a = Mathf.Lerp(1f, 0f, t * t);
                scorePopupTMP.color = c;

                yield return null;
            }

            scorePopupObj.SetActive(false);
        }

        private IEnumerator RespawnTimerRoutine(int earnedScore)
        {
            // 1. Keep the complete ticket with all ticks clearly visible for 1.4 seconds
            yield return new WaitForSeconds(1.4f);

            // 2. Prominent celebration text when customer receives the order
            if (orderTMP != null)
            {
                string scoreText = earnedScore >= 0 ? $"+{earnedScore}" : $"{earnedScore}";
                string scoreCol = earnedScore >= 0 ? "#FFD700" : "#FF3333";
                orderTMP.text = $"<b><size=24><color=#00FF66>[ ORDER COMPLETE! ]</color></size></b>\n<size=18><color={scoreCol}><b>{scoreText} Points Earned!</b></color></size>";
            }

            // Customer receives food on counter for 1.6 seconds
            yield return new WaitForSeconds(1.6f);

            // Customer takes plate and departs
            RemoveCustomer();
            ClearItemsOnPlate();
            if (plateObj != null)
            {
                plateObj.SetActive(false);
            }
            if (plateItemHolder != null)
            {
                plateItemHolder.gameObject.SetActive(false);
            }

            // 3. Countdown to next customer (1.4s + 1.6s + 2.0s = exactly 5.0s)
            float remainingDelay = Mathf.Max(1f, respawnDelay - 3.0f);
            float timer = remainingDelay;

            while (timer > 0f)
            {
                if (orderTMP != null)
                {
                    orderTMP.text = $"<size=16><color=#00FFAA><b>[ ORDER SERVED ]</b></color></size>\n<size=14><color=#AAAAAA>Next Customer in <color=#FFD700><b>{Mathf.CeilToInt(timer)}s</b></color>...</color></size>";
                }

                yield return new WaitForSeconds(1f);
                timer -= 1f;
            }

            // Spawn next order
            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.SpawnOrderForWindow(this);
            }
        }

        public override string GetInteractionPrompt(PlayerInteractor interactor)
        {
            if (!HasActiveOrder)
            {
                return "Window: Waiting for customer (Ingredient remains in hand)";
            }

            if (!interactor.IsHoldingItem)
            {
                return $"Window #{windowIndex + 1}: Check order ticket";
            }

            IngredientType heldType = interactor.HeldItem.IngredientType;

            if (heldType == IngredientType.Overcooked)
            {
                return "<color=#FF4444>Overcooked food cannot be served! Move to Trash</color>";
            }

            if (currentOrder.NeedsIngredient(heldType))
            {
                string disp = Order.GetOrderDisplayName(heldType);
                int pts = Order.GetIngredientScoreValue(heldType);
                return $"Place {disp} on Plate (+{pts} pts) [E]";
            }

            return $"<color=#FFAA55>Not on ticket! Remains in hand (Window #{windowIndex + 1})</color>";
        }
    }
}
