using UnityEngine;
using Unity.Netcode;
using System;

    public class PlayerMovement : NetworkBehaviour
{
    private Rigidbody rb;
    private Transform cameraTransform;
    private EdgeDetection edgeDetection; // Reference to edge detection component
    
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private bool canMove = false;
    [SerializeField] private float jumpEnergy = 10.0f;
    [SerializeField] private bool testMode = false;

    // Ground check
    [SerializeField] private Transform groundCheckRaycastOriginPoint;
    public bool isGrounded { get; private set; }
    [SerializeField] private float rayDistance = 1.3f;
    [SerializeField] private LayerMask groundMask;

    private void Start()
    {
        // Validate critical components
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"Rigidbody component is required on {gameObject.name} but was not found");
            enabled = false;
            return;
        }
        
        edgeDetection = GetComponent<EdgeDetection>(); // Get reference to edge detection
        SetOwnedCameraOnly();

        if (testMode)
        {
            canMove = true;
        }

        // Warn if EdgeDetection component is missing
        if (edgeDetection == null)
        {
            Debug.LogWarning($"EdgeDetection component not found on {gameObject.name}. Edge detection will be disabled.");
        }
        
        // Validate ground check configuration
        if (groundCheckRaycastOriginPoint == null)
        {
            Debug.LogError($"Ground check raycast origin point is not assigned on {gameObject.name}");
        }
        
        // Validate movement parameters
        if (moveSpeed <= 0)
        {
            Debug.LogWarning($"Move speed is {moveSpeed} on {gameObject.name}, setting to default value 5");
            moveSpeed = 5f;
        }
        
        if (maxSpeed <= 0)
        {
            Debug.LogWarning($"Max speed is {maxSpeed} on {gameObject.name}, setting to default value 8");
            maxSpeed = 8f;
        }
    }

    private void Update()
    {
        GroundCheckAndDebug();
    }

    private void OnEnable()
    {
        StartRaceCountdown.OnPlayerPossessionEvent += EnableMovement;
    }

    private void OnDisable()
    {
        StartRaceCountdown.OnPlayerPossessionEvent -= EnableMovement;
    }

    private void EnableMovement()
    {
        canMove = true;
    }

    private void SetOwnedCameraOnly()
    {
        Camera cam = GetComponentInChildren<Camera>();
        cameraTransform = cam != null ? cam.transform : null;

        if (cam != null)
        {
            cam.gameObject.SetActive(IsOwner || testMode);
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("PlayerMovement: cameraTransform is null after SetOwnedCameraOnly.");
        }
    }

    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
    }

    public void Move(Vector2 input)
    {
        // Early validation checks
        if (!canMove) return;
        if (!IsOwner && !testMode) return;
        
        if (rb == null)
        {
            Debug.LogError($"Rigidbody is null on {gameObject.name}, cannot move");
            return;
        }
        
        if (cameraTransform == null)
        {
            Debug.LogError($"CameraTransform is null on {gameObject.name}, cannot move");
            return;
        }

        // Validate input magnitude to avoid unnecessary calculations
        if (input.sqrMagnitude < 0.01f)
        {
            return; // No significant input
        }

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        
        // Check for degenerate camera vectors
        if (camForward.sqrMagnitude < 0.01f || camRight.sqrMagnitude < 0.01f)
        {
            Debug.LogWarning($"Camera vectors are degenerate on {gameObject.name}");
            return;
        }
        
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * input.y + camRight * input.x;

        // Edge detection check using separate component
        if (edgeDetection != null && moveDirection.sqrMagnitude > 0.1f)
        {
            if (edgeDetection.IsMovementBlocked(moveDirection))
            {
                return; // Block movement
            }
        }
        
        Vector3 desiredVelocity = moveDirection * moveSpeed;
        desiredVelocity.y = rb.velocity.y;
        rb.velocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);

        if (moveDirection.sqrMagnitude > 0.1f)
        {
            Quaternion directionToFace = Quaternion.LookRotation(moveDirection);
            rb.rotation = directionToFace;
        }
    }

    private void GroundCheckAndDebug()
    {
        if (groundCheckRaycastOriginPoint == null)
        {
            isGrounded = false;
            return;
        }
        
        if (rayDistance <= 0)
        {
            Debug.LogWarning($"Ray distance is {rayDistance} on {gameObject.name}, using default value 1.3");
            rayDistance = 1.3f;
        }
        
        isGrounded = Physics.Raycast(
            groundCheckRaycastOriginPoint.position,
            Vector3.down,
            out RaycastHit hit,
            rayDistance,
            groundMask
        );

#if UNITY_EDITOR
        Debug.DrawRay(
            groundCheckRaycastOriginPoint.position,
            Vector3.down * rayDistance,
            isGrounded ? Color.yellow : Color.red
        );
#endif
    }

    [ContextMenu("Jump")]
    public void Jump()
    {
        if (!isGrounded) return;
        
        if (rb == null)
        {
            Debug.LogError($"Rigidbody is null on {gameObject.name}, cannot jump");
            return;
        }
        
        if (jumpEnergy <= 0)
        {
            Debug.LogWarning($"Jump energy is {jumpEnergy} on {gameObject.name}, cannot jump");
            return;
        }
        
        rb.AddForce(Vector3.up * jumpEnergy, ForceMode.Impulse);
    }

    // Simplified debug - edge detection gizmos are now in EdgeDetection component
    private void OnDrawGizmosSelected()
    {
        // Only draw ground check visualization here
        if (groundCheckRaycastOriginPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(groundCheckRaycastOriginPoint.position, Vector3.down * rayDistance);
            Gizmos.DrawWireSphere(groundCheckRaycastOriginPoint.position + Vector3.down * rayDistance, 0.1f);
        }
    }
}
