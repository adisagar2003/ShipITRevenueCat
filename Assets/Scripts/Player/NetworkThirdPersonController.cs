using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

/// <summary>
/// Network-friendly Fall Guys-style third person controller with rigidbody physics
/// Combines features from ThirdPersonController with multiplayer compatibility
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(InputManager))]
[RequireComponent(typeof(InputHandler))]
public class NetworkThirdPersonController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Base movement speed. Fall Guys uses moderate speeds for better control.")]
    [Range(1f, 10f)]
    public float moveSpeed = 5f;

    [Tooltip("Maximum velocity to prevent teleporting and maintain Fall Guys feel.")]
    [Range(5f, 15f)]
    public float maxSpeed = 8f;

    [Header("Jump Settings")]
    [Tooltip("Jump force applied when jumping. Fall Guys has strong but controlled jumps.")]
    [Range(5f, 2500f)]
    public float jumpForce = 18f;

    [Tooltip("How long the character stays in air before gravity takes full effect.")]
    [Range(0.1f, 2f)]
    public float jumpTime = 0.85f;

    [Header("Physics Settings")]
    [Tooltip("Gravity force. Fall Guys has slightly lighter gravity for floaty feel.")]
    [Range(5f, 15f)]
    public float gravity = 9.8f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheckRaycastOriginPoint;
    [SerializeField] private float rayDistance = 0.46f;
    [SerializeField] private LayerMask groundMask;


    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool autoFindCamera = true;

    [Header("Movement Control")]
    [Tooltip("Controls whether player can move. Set to false to disable movement until race starts.")]
    [SerializeField] private bool canMove = false;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool enableMovementLogs = true;
    [SerializeField] private bool enableInputLogs = true;
    [SerializeField] private bool enableNetworkLogs = true;
    [SerializeField] private bool enableJumpLogs = true;

    // Components
    private Rigidbody rb;

    // State tracking
    private bool isJumping = false;
    private float jumpElapsedTime = 0;
    private bool isGrounded = false;
    
    // Simple dash state tracking
    private bool isDashing = false;
    
    // Simple super jump state tracking
    private bool isSuperJumping = false;

    // Input cache
    private Vector2 inputValue;
    private bool jumpInput;

    // Network variables for animation synchronization - owner can write
    private NetworkVariable<bool> networkIsJumping = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void OnEnable()
    {
        StartRaceCountdown.OnPlayerPossessionEvent += EnableMovement;
#if debug
        if (enableDebugLogs) Debug.Log($"<color=cyan>[NetworkThirdPersonController]</color> <color=white>Subscribed to OnPlayerPossessionEvent on {gameObject.name}</color>");
#endif
    }

    private void OnDisable()
    {
        StartRaceCountdown.OnPlayerPossessionEvent -= EnableMovement;
#if debug
        if (enableDebugLogs) Debug.Log($"<color=cyan>[NetworkThirdPersonController]</color> <color=white>Unsubscribed from OnPlayerPossessionEvent on {gameObject.name}</color>");
#endif
    }

    private void Start()
    {
#if debug
        if (enableDebugLogs) Debug.Log($"<color=cyan>[NetworkThirdPersonController]</color> <color=white>Starting initialization on {gameObject.name}</color>");
#endif

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
#if debug
            Debug.LogError($"<color=red>[NetworkThirdPersonController]</color> <color=white>CRITICAL: Rigidbody component is required on {gameObject.name}</color>");
#endif
            enabled = false;
            return;
        }

#if debug
        if (enableDebugLogs) Debug.Log($"<color=cyan>[NetworkThirdPersonController]</color> <color=white>Rigidbody found: {rb.name}</color>");
#endif


        // Find camera reference if needed
        if (autoFindCamera && cameraTransform == null)
        {
            FindCameraReference();
        }

        // Validate ground check setup
        if (groundCheckRaycastOriginPoint == null)
        {
#if debug
            if (enableDebugLogs) Debug.LogWarning($"<color=yellow>[NetworkThirdPersonController]</color> <color=white>Ground check raycast origin point not assigned on {gameObject.name}</color>");
#endif
        }

#if debug
        if (enableNetworkLogs) Debug.Log($"<color=magenta>[NetworkThirdPersonController]</color> <color=white>IsOwner: {IsOwner}, IsServer: {IsServer}, IsClient: {IsClient}</color>");

        if (enableDebugLogs) Debug.Log($"<color=green>[NetworkThirdPersonController]</color> <color=white>Initialization complete on {gameObject.name}</color>");
#endif
    }


    private void Update()
    {
        if (!IsOwner) return;
        GroundCheck();
        // Jump input is handled through command pattern via InputHandler
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        
        // Only handle movement if race has started
        if (canMove)
        {
            HandleMovement();
            HandleJump();
        }

        networkIsJumping.Value = isJumping;
        networkVelocity.Value = rb.linearVelocity;
    }

    public void Move(Vector2 input)
    {
        if (!IsOwner)
        {
#if debug
            if (enableInputLogs) Debug.Log($"<color=orange>[NetworkThirdPersonController]</color> <color=white>Move() called but not owner - ignoring input: {input}</color>");
#endif
            return;
        }

        if (!canMove)
        {
#if debug
            if (enableInputLogs && input.sqrMagnitude > 0.01f) Debug.Log($"<color=yellow>[NetworkThirdPersonController]</color> <color=white>Move() called but movement disabled - race hasn't started yet</color>");
#endif
            return;
        }

        inputValue = input;

#if debug
        if (enableInputLogs && input.sqrMagnitude > 0.01f)
        {
            Debug.Log($"<color=lime>[NetworkThirdPersonController]</color> <color=white>Move() input received: {input} (magnitude: {input.magnitude:F3})</color>");
        }
#endif
    }

    private void HandleMovement()
    {
        // Only log if there's actual input check.
        bool hasInput = inputValue.sqrMagnitude > 0.01f;

    #if debug
        if (enableMovementLogs && hasInput)
        {
            Debug.Log($"<color=lightblue>[NetworkThirdPersonController]</color> <color=white>HandleMovement() - Input: {inputValue}</color>");
        }
    #endif

        // Get camera reference for camera-relative movement
        Transform cameraRef = GetCameraReference();
        if (cameraRef == null)
        {
#if debug
            if (enableMovementLogs) Debug.LogWarning($"<color=red>[NetworkThirdPersonController]</color> <color=white>No camera reference found - cannot calculate camera-relative movement</color>");
#endif
            return;
        }

        // Get camera-relative directions (flatten Y to avoid flying)
        Vector3 cameraForward = cameraRef.forward;
        Vector3 cameraRight = cameraRef.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate camera-relative movement direction
        Vector3 moveDirection;
        
        // If dashing, only allow left/right movement (no forward/backward)
        if (isDashing)
        {
            moveDirection = cameraRight * inputValue.x; // Only left/right movement
#if debug
            if (enableMovementLogs && inputValue.y != 0)
            {
                Debug.Log($"<color=orange>[NetworkThirdPersonController]</color> <color=white>Dash mode: blocking forward/backward input ({inputValue.y}), allowing left/right ({inputValue.x})</color>");
            }
#endif
        }
        // If super jumping, allow reduced air control
        else if (isSuperJumping)
        {
            moveDirection = (cameraForward * inputValue.y + cameraRight * inputValue.x) * 0.3f; // Reduced air control
#if debug
            if (enableMovementLogs && hasInput)
            {
                Debug.Log($"<color=purple>[NetworkThirdPersonController]</color> <color=white>Super jump mode: reduced air control ({inputValue})</color>");
            }
#endif
        }
        else
        {
            moveDirection = cameraForward * inputValue.y + cameraRight * inputValue.x; // Normal movement
        }

#if debug
        if (enableMovementLogs && hasInput)
        {
            Debug.Log($"<color=lightblue>[NetworkThirdPersonController]</color> <color=white>Camera-relative movement - Forward: {cameraForward}, Right: {cameraRight}, Direction: {moveDirection}</color>");
        }
#endif

        // Apply movement with Fall Guys-style physics
        Vector3 beforeVelocity = rb.linearVelocity;
        
        if (isDashing)
        {
            // During dash, only apply lateral (left/right) movement adjustments
            Vector3 lateralVelocity = moveDirection * moveSpeed;
            Vector3 currentVelocity = rb.linearVelocity;
            
            // Keep forward dash speed, only modify left/right component
            Vector3 forwardComponent = Vector3.Project(currentVelocity, transform.forward);
            Vector3 newVelocity = forwardComponent + lateralVelocity;
            newVelocity.y = currentVelocity.y; // Preserve vertical velocity
            
            rb.linearVelocity = newVelocity;
            
#if debug
            if (enableMovementLogs && hasInput)
            {
                Debug.Log($"<color=orange>[NetworkThirdPersonController]</color> <color=white>Dash movement - Lateral: {lateralVelocity}, Forward: {forwardComponent}, Final: {newVelocity}</color>");
            }
#endif
        }
        else
        {
            // Normal movement
            Vector3 desiredVelocity = moveDirection * moveSpeed;
            desiredVelocity.y = rb.linearVelocity.y; // Preserve vertical velocity
            
            // Clamp to max speed to prevent teleporting
            rb.linearVelocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);
        }

#if debug
        if (enableMovementLogs && hasInput)
        {
            Debug.Log($"<color=lightblue>[NetworkThirdPersonController]</color> <color=white>Velocity applied - Before: {beforeVelocity}, Desired: {desiredVelocity}, Final: {rb.linearVelocity}</color>");
        }
#endif

        // Rotate player to face movement direction (only if moving)
        if (moveDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, 0.15f);

#if debug
            if (enableMovementLogs)
            {
                Debug.Log($"<color=lightblue>[NetworkThirdPersonController]</color> <color=white>Rotation applied - Target: {targetRotation.eulerAngles}, Current: {rb.rotation.eulerAngles}</color>");
            }
#endif
        }
    }

    private void HandleJump()
    {
        if (!isJumping) return;

        // Apply smooth upward force during jump time (Fall Guys style)
        float jumpProgress = jumpElapsedTime / jumpTime;
        float currentJumpForce = Mathf.SmoothStep(jumpForce, jumpForce * 0.3f, jumpProgress);

#if debug
        if (enableJumpLogs)
        {
            Debug.Log($"<color=orange>[NetworkThirdPersonController]</color> <color=white>HandleJump() - Progress: {jumpProgress:F3}, Current Force: {currentJumpForce:F2}, Elapsed: {jumpElapsedTime:F3}/{jumpTime:F2}</color>");
        }
#endif

        // Apply upward force using AddForce for smooth acceleration
        Vector3 beforeVelocity = rb.linearVelocity;
        rb.AddForce(Vector3.up * currentJumpForce, ForceMode.Acceleration);

#if debug
        if (enableJumpLogs)
        {
            Debug.Log($"<color=orange>[NetworkThirdPersonController]</color> <color=white>Jump force applied - Before velocity: {beforeVelocity}, Force: {currentJumpForce}, After velocity: {rb.linearVelocity}</color>");
        }
#endif

        // Update jump timer
        jumpElapsedTime += Time.fixedDeltaTime;

        // End jump when time elapsed
        if (jumpElapsedTime >= jumpTime)
        {
            isJumping = false;
            jumpElapsedTime = 0;

#if debug
            if (enableJumpLogs)
            {
                Debug.Log($"<color=orange>[NetworkThirdPersonController]</color> <color=white>Jump completed - Final velocity: {rb.linearVelocity}</color>");
            }
#endif
        }

        // Apply custom gravity for Fall Guys feel
        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);

#if debug
        if (enableJumpLogs)
        {
            Debug.Log($"<color=orange>[NetworkThirdPersonController]</color> <color=white>Applied gravity force: {Vector3.down * gravity}</color>");
        }
#endif
    }

    public void Jump()
    {
#if debug
        if (enableJumpLogs)
        {
            Debug.Log($"<color=yellow>[NetworkThirdPersonController]</color> <color=white>Jump() called - IsOwner: {IsOwner}, IsGrounded: {isGrounded}, IsJumping: {isJumping}</color>");
        }
#endif

        // Check ownership
        if (!IsOwner)
        {
#if debug
            if (enableJumpLogs) Debug.Log($"<color=red>[NetworkThirdPersonController]</color> <color=white>Jump() rejected - Not owner</color>");
#endif
            return;
        }

        // Check if movement is enabled
        if (!canMove)
        {
#if debug
            if (enableJumpLogs) Debug.Log($"<color=yellow>[NetworkThirdPersonController]</color> <color=white>Jump() rejected - Movement disabled, race hasn't started yet</color>");
#endif
            return;
        }

        // Check if grounded
        if (!isGrounded)
        {
#if debug
            if (enableJumpLogs) Debug.Log($"<color=red>[NetworkThirdPersonController]</color> <color=white>Jump() rejected - Not grounded</color>");
#endif
            return;
        }

        // Check if already jumping
        if (isJumping)
        {
#if debug
            if (enableJumpLogs) Debug.Log($"<color=red>[NetworkThirdPersonController]</color> <color=white>Jump() rejected - Already jumping</color>");
#endif
            return;
        }

        Vector3 beforeVelocity = rb.linearVelocity;

        isJumping = true;
        jumpElapsedTime = 0;

        // Strong initial jump impulse for immediate response
        Vector3 impulse = Vector3.up * jumpForce;
        rb.AddForce(impulse, ForceMode.VelocityChange);

#if debug
        if (enableJumpLogs)
        {
            Debug.Log($"<color=green>[NetworkThirdPersonController]</color> <color=white>Jump STARTED! Impulse: {impulse}, Before velocity: {beforeVelocity}, After velocity: {rb.linearVelocity}</color>");
        }
#endif
    }

    private void GroundCheck()
    {
        bool wasGrounded = isGrounded;

        if (groundCheckRaycastOriginPoint == null)
        {
            isGrounded = false;
#if debug
            if (enableJumpLogs && wasGrounded)
            {
                Debug.LogWarning($"<color=yellow>[NetworkThirdPersonController]</color> <color=white>GroundCheck - No raycast origin point assigned!</color>");
            }
#endif
            return;
        }

        isGrounded = Physics.Raycast(
            groundCheckRaycastOriginPoint.position,
            Vector3.down,
            rayDistance,
            groundMask
        );

#if debug
        // Log ground state changes
        if (enableJumpLogs && wasGrounded != isGrounded)
        {
            Debug.Log($"<color=purple>[NetworkThirdPersonController]</color> <color=white>Ground state changed: {wasGrounded} -> {isGrounded} at position {groundCheckRaycastOriginPoint.position}</color>");
        }
#endif

#if UNITY_EDITOR
        Debug.DrawRay(
            groundCheckRaycastOriginPoint.position,
            Vector3.down * rayDistance,
            isGrounded ? Color.green : Color.red
        );
#endif
    }

    // Public getters for animation system
    public bool IsGrounded => isGrounded;
    public bool IsJumping => IsOwner ? isJumping : networkIsJumping.Value;
    public Vector3 Velocity => IsOwner ? rb.linearVelocity : networkVelocity.Value;

    /// <summary>
    /// Finds camera reference automatically - multiplayer safe (only for owned players)
    /// </summary>
    private void FindCameraReference()
    {
        // Only the owner should have an active camera reference
        if (!IsOwner)
        {
#if debug
            if (enableDebugLogs) Debug.Log($"<color=cyan>[NetworkThirdPersonController]</color> <color=white>Not owner - skipping camera reference setup</color>");
#endif
            return;
        }

        // Try to find Cinemachine virtual camera as child first (most reliable for multiplayer)
        var virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        if (virtualCamera != null)
        {
            cameraTransform = virtualCamera.transform;
#if debug
            if (enableDebugLogs) Debug.Log($"<color=cyan>[NetworkThirdPersonController]</color> <color=white>Found child Cinemachine virtual camera: {cameraTransform.name}</color>");
#endif
            return;
        }

        // Try to find camera as child component
        Camera childCamera = GetComponentInChildren<Camera>();
        if (childCamera != null)
        {
            cameraTransform = childCamera.transform;
#if debug
            if (enableDebugLogs) Debug.Log($"<color=cyan>[NetworkThirdPersonController]</color> <color=white>Found child camera: {cameraTransform.name}</color>");
#endif
            return;
        }

        // Last resort: use main camera (only for owner, and only if no other players are using it)
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
#if debug
            if (enableDebugLogs) Debug.Log($"<color=cyan>[NetworkThirdPersonController]</color> <color=white>Using main camera as fallback: {cameraTransform.name}</color>");
#endif
            return;
        }

#if debug
        if (enableDebugLogs) Debug.LogWarning($"<color=yellow>[NetworkThirdPersonController]</color> <color=white>No camera found for owner player</color>");
#endif
    }

    /// <summary>
    /// Gets the current camera reference for movement calculations - multiplayer safe
    /// </summary>
    private Transform GetCameraReference()
    {
        if (!IsOwner) return null;

        if (cameraTransform != null) return cameraTransform;

        // Try main camera as fallback, but only for the owner
        return Camera.main?.transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckRaycastOriginPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(groundCheckRaycastOriginPoint.position, Vector3.down * rayDistance);
            Gizmos.DrawWireSphere(groundCheckRaycastOriginPoint.position + Vector3.down * rayDistance, 0.1f);
        }
    }

    // Simple methods to control dash state
    public void SetDashState(bool dashing)
    {
        isDashing = dashing;
        Debug.Log($"<color=orange>[NetworkThirdPersonController]</color> Dash state set to: {dashing}");
    }

    public bool IsDashing()
    {
        return isDashing;
    }

    // Simple methods to control super jump state
    public void SetSuperJumpState(bool superJumping)
    {
        isSuperJumping = superJumping;
        Debug.Log($"<color=purple>[NetworkThirdPersonController]</color> Super jump state set to: {superJumping}");
    }

    public bool IsSuperJumping()
    {
        return isSuperJumping;
    }

    /// <summary>
    /// Event handler for race start - enables player movement when countdown finishes
    /// </summary>
    private void EnableMovement()
    {
        canMove = true;
#if debug
        if (enableDebugLogs) Debug.Log($"<color=lime>[NetworkThirdPersonController]</color> <color=white>Movement enabled for {gameObject.name} - Race started!</color>");
#endif
    }

    /// <summary>
    /// Public method to disable player movement - useful for race finish, cutscenes, etc.
    /// </summary>
    public void DisableMovement()
    {
        canMove = false;
#if debug
        if (enableDebugLogs) Debug.Log($"<color=red>[NetworkThirdPersonController]</color> <color=white>Movement disabled for {gameObject.name}</color>");
#endif
    }
}
