using System.Collections.Generic;
using Kitchen;
using Stations;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactDistance = 2.4f;
    [SerializeField] private float interactRadius = 1.2f;

    [Header("Carry Point")]
    [SerializeField] private Transform carryPoint;

    private KitchenObject heldItem;
    private IInteractable currentTarget;
    private BaseStation selectedStation;

    private TMP_Text promptText;
    private GameObject promptRoot;
    private Camera mainCam;

    // Zero-allocation preallocated buffers for per-frame physics queries
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    private readonly Collider[] overlapBuffer = new Collider[16];
    private readonly List<IInteractable> candidateList = new List<IInteractable>(16);

    public bool IsHoldingItem => heldItem != null;
    public KitchenObject HeldItem => heldItem;

    private void Awake()
    {
        mainCam = Camera.main;
        CreateCarryPoint();
        CreatePromptUI();
    }

    private void CreateCarryPoint()
    {
        if (carryPoint == null)
        {
            GameObject cp = new GameObject("CarryPoint");
            cp.transform.SetParent(transform);
            // Height at chest level, slightly in front of player
            cp.transform.localPosition = new Vector3(0f, 0.9f, 1.1f);
            carryPoint = cp.transform;
        }
    }

    private void CreatePromptUI()
    {
        promptRoot = new GameObject("PlayerPromptUI");
        promptRoot.transform.SetParent(transform);
        promptRoot.transform.localPosition = new Vector3(0f, 2.6f, 0f);

        promptText = promptRoot.AddComponent<TextMeshPro>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 3.8f;
        promptText.color = Color.white;
        promptText.outlineColor = Color.black;
        promptText.outlineWidth = 0.35f;
        promptRoot.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            if (promptRoot != null) promptRoot.SetActive(false);
            return;
        }

        DetectInteractables();
        HandleInteractionInput();
    }

    private void LateUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (promptRoot != null && promptRoot.activeSelf && mainCam != null)
        {
            promptRoot.transform.rotation = mainCam.transform.rotation;
        }
    }

    private void DetectInteractables()
    {
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 forward = transform.forward;

        candidateList.Clear();

        // 1. Forward SphereCast (NonAlloc)
        int hitCount = Physics.SphereCastNonAlloc(origin, interactRadius, forward, hitBuffer, interactDistance);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hitBuffer[i];
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            IInteractable station = hit.collider.GetComponentInParent<IInteractable>();
            if (station != null && !candidateList.Contains(station)) candidateList.Add(station);
        }

        // 2. Proximity OverlapSphere (NonAlloc, guarantees detection when standing right next to counter)
        int overlapCount = Physics.OverlapSphereNonAlloc(origin + forward * 0.8f, 1.4f, overlapBuffer);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null || col.transform == transform || col.transform.IsChildOf(transform)) continue;
            IInteractable station = col.GetComponentInParent<IInteractable>();
            if (station != null && !candidateList.Contains(station)) candidateList.Add(station);
        }

        // Pick closest candidate in front of player
        IInteractable closest = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < candidateList.Count; i++)
        {
            IInteractable cand = candidateList[i];
            MonoBehaviour mb = cand as MonoBehaviour;
            if (mb == null) continue;

            Vector3 dirToStation = (mb.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(forward, dirToStation);

            // Favor stations in front of player
            if (dot > -0.3f)
            {
                float dist = Vector3.Distance(transform.position, mb.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = cand;
                }
            }
        }

        // Update target
        if (closest != currentTarget)
        {
            if (selectedStation != null) selectedStation.SetSelected(false);
            currentTarget = closest;
            selectedStation = currentTarget as BaseStation;
            if (selectedStation != null) selectedStation.SetSelected(true);
        }

        // Display prompt
        if (currentTarget != null)
        {
            string prompt = currentTarget.GetInteractionPrompt(this);
            if (!string.IsNullOrEmpty(prompt))
            {
                promptText.text = prompt;
                promptRoot.SetActive(true);
            }
            else
            {
                promptRoot.SetActive(false);
            }
        }
        else
        {
            promptRoot.SetActive(false);
        }
    }

    private void HandleInteractionInput()
    {
        bool interactPressed = false;

        // New Input System Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) interactPressed = true;
        }

        // Mouse Left Click (guarded against UI clicks)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null ||
                !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                interactPressed = true;
            }
        }

        // Gamepad
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame) interactPressed = true;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
            (Input.GetMouseButtonDown(0) && (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())))
        {
            interactPressed = true;
        }
#endif

        // Interact
        if (interactPressed && currentTarget != null)
        {
            currentTarget.Interact(this);
        }
    }

    public void HoldItem(KitchenObject item)
    {
        if (item == null) return;

        heldItem = item;
        heldItem.transform.SetParent(carryPoint);
        heldItem.transform.localPosition = Vector3.zero;
        heldItem.transform.localRotation = Quaternion.identity;

        // Compensate for player root scaling so the held food is never stretched
        Vector3 parentLossy = carryPoint.lossyScale;
        heldItem.transform.localScale = new Vector3(
            1f / Mathf.Max(parentLossy.x, 0.01f),
            1f / Mathf.Max(parentLossy.y, 0.01f),
            1f / Mathf.Max(parentLossy.z, 0.01f)
        );
    }

    public KitchenObject DropItem()
    {
        if (heldItem == null) return null;

        KitchenObject item = heldItem;
        heldItem = null;
        item.transform.SetParent(null);
        item.transform.localScale = Vector3.one; // restore clean world scale
        return item;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && currentTarget != null)
        {
            currentTarget.Interact(this);
        }
    }
}
