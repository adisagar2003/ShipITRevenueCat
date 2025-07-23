using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerMovement : NetworkBehaviour
{
    private Rigidbody rb;
    private Transform cameraTransform;
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
        rb = GetComponent<Rigidbody>();
        SetOwnedCameraOnly();

        if (testMode)
        {
            canMove = true;
        }
    }

    private void Update()
    {
        GroundCheckAndDebug();
    }

    private void OnEnable()
    {
        RaceLevelManager.OnAllPlayersReady += EnableMovement; // 3, 2 ,1 all players have control.
    }

    private void OnDisable()
    {
        RaceLevelManager.OnAllPlayersReady -= EnableMovement;
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
        if (!canMove) return;
        if (!IsOwner && !testMode) return;
        if (cameraTransform == null)
        {
            Debug.LogError("CameraTransform is null on " + gameObject.name);
            return;
        }

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * input.y + camRight * input.x;
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
        isGrounded = Physics.Raycast(
            groundCheckRaycastOriginPoint.position,
            Vector3.down,
            out RaycastHit hit,
            rayDistance,
            groundMask
        );

        Debug.DrawRay(
            groundCheckRaycastOriginPoint.position,
            Vector3.down * rayDistance,
            isGrounded ? Color.yellow : Color.red
        );
    }

    [ContextMenu("Jump")]
    public void Jump()
    {
        if (!isGrounded) return;
        rb.AddForce(Vector3.up * jumpEnergy, ForceMode.Impulse);
    }
}
