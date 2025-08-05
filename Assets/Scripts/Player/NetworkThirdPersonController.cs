using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Network-friendly Fall Guys-style third person controller with rigidbody physics
/// Combines features from ThirdPersonController with multiplayer compatibility
/// </summary>
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
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            GameLogger.LogCritical(GameLogger.LogCategory.Gameplay, $"Rigidbody component is required on {gameObject.name}", this);
            enabled = false;
            return;
        }
        
        SetOwnedCameraOnly();
        
        // Validate ground check setup
        if (groundCheckRaycastOriginPoint == null)
        {
            GameLogger.LogError(GameLogger.LogCategory.Gameplay, $"Ground check raycast origin point not assigned on {gameObject.name}", this);
        }
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
        if (!IsOwner) return;
        inputValue = input;
    }
    
    private void HandleMovement()
    {
        // Use cached camera transform or find main camera as fallback
        Transform cameraRef = cameraTransform != null ? cameraTransform : Camera.main?.transform;
        if (cameraRef == null) return;
        
        // Get camera-relative directions
        Vector3 camForward = cameraRef.forward;
        Vector3 camRight = cameraRef.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        
        // Calculate movement direction
        Vector3 moveDirection = camForward * inputValue.y + camRight * inputValue.x;
        
        // Apply movement with Fall Guys-style physics
        Vector3 desiredVelocity = moveDirection * moveSpeed;
        desiredVelocity.y = rb.linearVelocity.y; // Preserve vertical velocity
        
        // Clamp to max speed to prevent teleporting
        rb.linearVelocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);
        
        // Rotate player to face movement direction
        if (moveDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, 0.15f);
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