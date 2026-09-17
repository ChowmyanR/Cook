using Kitchen;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Stations
{
    public class RefrigeratorUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button meatButton;
        [SerializeField] private Button cheeseButton;
        [SerializeField] private Button vegetablesButton;
        [SerializeField] private Button closeButton;

        private PlayerInteractor currentInteractor;
        private RefrigeratorStation station;
        private Canvas canvas;
        private Camera mainCam;

        public bool IsOpen => (panelRoot != null && panelRoot.activeSelf) || gameObject.activeSelf;

        private void Awake()
        {
            mainCam = Camera.main;
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            BindButtons();
            Close();
        }

        private void Start()
        {
            EnsureWorldCamera();
        }

        public void BindButtons()
        {
            if (meatButton != null)
            {
                meatButton.onClick.RemoveAllListeners();
                meatButton.onClick.AddListener(OnClickMeat);
            }

            if (cheeseButton != null)
            {
                cheeseButton.onClick.RemoveAllListeners();
                cheeseButton.onClick.AddListener(OnClickCheese);
            }

            if (vegetablesButton != null)
            {
                vegetablesButton.onClick.RemoveAllListeners();
                vegetablesButton.onClick.AddListener(OnClickVegetables);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnClickClose);
            }
        }

        public void SetReferences(GameObject root, Button meat, Button cheese, Button veg, Button close)
        {
            panelRoot = root;
            meatButton = meat;
            cheeseButton = cheese;
            vegetablesButton = veg;
            closeButton = close;
            BindButtons();
        }

        private void EnsureWorldCamera()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (mainCam == null) mainCam = Camera.main;

            if (canvas != null && (canvas.worldCamera == null || !canvas.worldCamera.gameObject.activeInHierarchy))
            {
                canvas.worldCamera = mainCam;
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            // Keep facing the active camera
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null)
            {
                transform.rotation = mainCam.transform.rotation;
            }

            // Auto-close if player walks away
            if (currentInteractor != null)
            {
                float distance = Vector3.Distance(transform.position, currentInteractor.transform.position);
                if (distance > 4.5f)
                {
                    Close();
                    return;
                }
            }

            // Handle keyboard shortcuts
            HandleKeyboardInput();
        }

        private void HandleKeyboardInput()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                SelectIngredient(IngredientType.Meat);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            {
                SelectIngredient(IngredientType.Cheese);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            {
                SelectIngredient(IngredientType.Vegetables);
            }
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open(PlayerInteractor interactor, RefrigeratorStation refStation)
        {
            currentInteractor = interactor;
            station = refStation;

            EnsureWorldCamera();

            // Activate both gameObject and panelRoot
            gameObject.SetActive(true);
            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(true);
            }

            // Unlock mouse cursor for UI clicking
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void Close()
        {
            // Guaranteed closure: deactivate root, panelRoot, and clear state
            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(false);
            }

            gameObject.SetActive(false);
            currentInteractor = null;
        }

        public void Toggle(PlayerInteractor interactor, RefrigeratorStation refStation)
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open(interactor, refStation);
            }
        }

        public void OnClickMeat()
        {
            SelectIngredient(IngredientType.Meat);
        }

        public void OnClickCheese()
        {
            SelectIngredient(IngredientType.Cheese);
        }

        public void OnClickVegetables()
        {
            SelectIngredient(IngredientType.Vegetables);
        }

        public void OnClickClose()
        {
            Close();
        }

        public void SelectIngredient(IngredientType type)
        {
            // 1. Ensure we have the player interactor reference
            if (currentInteractor == null)
            {
                currentInteractor = FindAnyObjectByType<PlayerInteractor>();
            }

            // 2. If player can hold an item, give it to them
            if (currentInteractor != null && !currentInteractor.IsHoldingItem)
            {
                KitchenObject newItem = KitchenObject.Spawn(type, currentInteractor.transform.position, Quaternion.identity);
                currentInteractor.HoldItem(newItem);
            }

            // 3. ALWAYS close the panel immediately upon selection!
            Close();
        }
    }
}
