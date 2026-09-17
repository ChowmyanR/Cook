using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units/sec. 9-11 feels snappy and responsive for a kitchen environment.")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 16f;

    private CharacterController characterController;
    private Vector3 verticalVelocity;
    private Vector2 inputActionInput = Vector2.zero;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 45f;
            characterController.minMoveDistance = 0f;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        // Always read input fresh each frame so the player stops instantly upon key release
        Vector2 input = Vector2.zero;

        // 1. Keyboard (New Input System)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        }

        // 2. Gamepad Stick
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.05f)
            {
                input = stick;
            }
        }

        // 3. Fallback to InputAction callback if keyboard was not active
        if (input.sqrMagnitude < 0.001f && inputActionInput.sqrMagnitude > 0.01f)
        {
            input = inputActionInput;
        }

        // Normalize direction vector
        input = Vector2.ClampMagnitude(input, 1f);

        // 4. Align movement to screen-space (Camera View)
        Vector3 camForward = Vector3.forward;
        Vector3 camRight = Vector3.right;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null)
        {
            Transform cam = mainCam.transform;
            if (Mathf.Abs(cam.forward.y) > 0.8f)
            {
                // Top-down camera: camera forward points down into the floor, so camera "up" is screen-up!
                camForward = Vector3.ProjectOnPlane(cam.up, Vector3.up).normalized;
                camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            }
            else
            {
                camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            }
        }

        Vector3 moveDir = (camRight * input.x + camForward * input.y);

        // 5. Smooth rotation towards movement direction
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 6. Gravity to stay firmly grounded
        if (characterController.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -3f;
        }
        else
        {
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
        }

        // 7. Execute movement (Instantly zero when no keys pressed)
        Vector3 finalMovement = (moveDir * moveSpeed) + verticalVelocity;
        characterController.Move(finalMovement * Time.deltaTime);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            inputActionInput = context.ReadValue<Vector2>();
        }
        else if (context.canceled)
        {
            inputActionInput = Vector2.zero;
        }
    }
}
