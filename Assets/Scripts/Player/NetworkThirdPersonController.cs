using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Network-friendly Fall Guys-style third person controller with rigidbody physics
/// Combines features from ThirdPersonController with multiplayer compatibility
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(InputManager))]
[RequireComponent(typeof(InputHandler))]
[RequireComponent(typeof(CameraLook))]
[RequireComponent(typeof(MouseLookWithTouch))]
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
    [Range(5f, 25f)]
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
    [SerializeField] private float rayDistance = 1.3f;
    [SerializeField] private LayerMask groundMask;
    
    [Header("Camera Reference")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool enableMovementLogs = true;
    [SerializeField] private bool enableInputLogs = true;
    [SerializeField] private bool enableNetworkLogs = true;
    
    // Components
    private Rigidbody rb;
    private Transform playerCameraTransform;
    
    // State tracking
    private bool isJumping = false;
    private float jumpElapsedTime = 0;
    private bool isGrounded = false;
    
    // Input cache
    private Vector2 inputValue;
    private bool jumpInput;
    
    // Network variables for animation synchronization
    private NetworkVariable<bool> networkIsJumping = new NetworkVariable<bool>(false);
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>(Vector3.zero);
    
    private void Start()
    {
        if (enableDebugLogs) Debug.Log($"<color=cyan>[NetworkThirdPersonController]</color> <color=white>Starting initialization on {gameObject.name}</color>");
        
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"<color=red>[NetworkThirdPersonController]</color> <color=white>CRITICAL: Rigidbody component is required on {gameObject.name}</color>");
            enabled = false;
            return;
        }
        
        if (enableDebugLogs) Debug.Log($"<color=cyan>[NetworkThirdPersonController]</color> <color=white>Rigidbody found: {rb.name}</color>");
        
        SetOwnedCameraOnly();
        
        // Validate ground check setup
        if (groundCheckRaycastOriginPoint == null)
        {
            Debug.LogWarning($"<color=yellow>[NetworkThirdPersonController]</color> <color=white>Ground check raycast origin point not assigned on {gameObject.name}</color>");
        }
        
        if (enableNetworkLogs) Debug.Log($"<color=magenta>[NetworkThirdPersonController]</color> <color=white>IsOwner: {IsOwner}, IsServer: {IsServer}, IsClient: {IsClient}</color>");
        
        if (enableDebugLogs) Debug.Log($"<color=green>[NetworkThirdPersonController]</color> <color=white>Initialization complete on {gameObject.name}</color>");
    }
    
    private void SetOwnedCameraOnly()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cam.gameObject.SetActive(IsOwner);
            playerCameraTransform = cam.transform;
        }
        
        // If no camera transform assigned, try to find one
        if (cameraTransform == null && playerCameraTransform != null)
        {
            cameraTransform = playerCameraTransform;
        }
    }
    
    private void Update()
    {
        // Only the owner processes ground checks
        if (!IsOwner) return;
        
        GroundCheck();
        // Jump input is handled through command pattern via InputHandler
    }
    
    private void FixedUpdate()
    {
        // Only owner calculates and applies movement
        if (!IsOwner) return;
        
        HandleMovement();
        HandleJump();
        
        // Update network variables for other clients
        networkIsJumping.Value = isJumping;
        networkVelocity.Value = rb.linearVelocity;
    }
    
    public void Move(Vector2 input)
    {
        if (!IsOwner) 
        {
            if (enableInputLogs) Debug.Log($"<color=orange>[NetworkThirdPersonController]</color> <color=white>Move() called but not owner - ignoring input: {input}</color>");
            return;
        }
        
        inputValue = input;
        
        if (enableInputLogs && input.sqrMagnitude > 0.01f) 
        {
            Debug.Log($"<color=lime>[NetworkThirdPersonController]</color> <color=white>Move() input received: {input} (magnitude: {input.magnitude:F3})</color>");
        }
    }
    
    private void HandleMovement()
    {
        // Use cached camera transform or find main camera as fallback
        Transform cameraRef = cameraTransform != null ? cameraTransform : Camera.main?.transform;
        if (cameraRef == null) 
        {
            if (enableMovementLogs) Debug.LogWarning($"<color=red>[NetworkThirdPersonController]</color> <color=white>No camera reference found - cannot calculate movement</color>");
            return;
        }
        
        // Only log if there's actual input
        bool hasInput = inputValue.sqrMagnitude > 0.01f;
        
        if (enableMovementLogs && hasInput)
        {
            Debug.Log($"<color=lightblue>[NetworkThirdPersonController]</color> <color=white>HandleMovement() - Input: {inputValue}, Camera: {cameraRef.name}</color>");
        }
        
        // Get camera-relative directions
        Vector3 camForward = cameraRef.forward;
        Vector3 camRight = cameraRef.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        
        // Calculate movement direction
        Vector3 moveDirection = camForward * inputValue.y + camRight * inputValue.x;
        
        if (enableMovementLogs && hasInput)
        {
            Debug.Log($"<color=lightblue>[NetworkThirdPersonController]</color> <color=white>Movement calc - Forward: {camForward}, Right: {camRight}, Direction: {moveDirection}</color>");
        }
        
        // Apply movement with Fall Guys-style physics
        Vector3 desiredVelocity = moveDirection * moveSpeed;
        desiredVelocity.y = rb.linearVelocity.y; // Preserve vertical velocity
        
        Vector3 beforeVelocity = rb.linearVelocity;
        
        // Clamp to max speed to prevent teleporting
        rb.linearVelocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);
        
        if (enableMovementLogs && hasInput)
        {
            Debug.Log($"<color=lightblue>[NetworkThirdPersonController]</color> <color=white>Velocity applied - Before: {beforeVelocity}, Desired: {desiredVelocity}, Final: {rb.linearVelocity}</color>");
        }
        
        // Rotate player to face movement direction
        if (moveDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, 0.15f);
            
            if (enableMovementLogs)
            {
                Debug.Log($"<color=lightblue>[NetworkThirdPersonController]</color> <color=white>Rotation applied - Target: {targetRotation.eulerAngles}, Current: {rb.rotation.eulerAngles}</color>");
            }
        }
    }
    
    private void HandleJump()
    {
        if (!isJumping) return;
        
        // Apply smooth upward force during jump time (Fall Guys style)
        float jumpProgress = jumpElapsedTime / jumpTime;
        float currentJumpForce = Mathf.SmoothStep(jumpForce, jumpForce * 0.3f, jumpProgress);
        
        // Apply upward force
        Vector3 jumpVelocity = rb.linearVelocity;
        jumpVelocity.y = currentJumpForce * Time.fixedDeltaTime;
        rb.linearVelocity = jumpVelocity;
        
        // Update jump timer
        jumpElapsedTime += Time.fixedDeltaTime;
        
        // End jump when time elapsed
        if (jumpElapsedTime >= jumpTime)
        {
            isJumping = false;
            jumpElapsedTime = 0;
        }
        
        // Apply custom gravity for Fall Guys feel
        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
    }
    
    public void Jump()
    {
        // Only allow jump if grounded and not already jumping
        if (!IsOwner || !isGrounded || isJumping) return;
        
        isJumping = true;
        jumpElapsedTime = 0;
        
        // Initial jump impulse
        rb.AddForce(Vector3.up * (jumpForce * 0.5f), ForceMode.Impulse);
    }
    
    private void GroundCheck()
    {
        if (groundCheckRaycastOriginPoint == null) 
        {
            isGrounded = false;
            return;
        }
        
        isGrounded = Physics.Raycast(
            groundCheckRaycastOriginPoint.position,
            Vector3.down,
            rayDistance,
            groundMask
        );
        
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
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheckRaycastOriginPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(groundCheckRaycastOriginPoint.position, Vector3.down * rayDistance);
            Gizmos.DrawWireSphere(groundCheckRaycastOriginPoint.position + Vector3.down * rayDistance, 0.1f);
        }
    }
}