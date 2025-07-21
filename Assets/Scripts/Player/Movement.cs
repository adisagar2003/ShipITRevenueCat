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
    [SerializeField] private bool testMode = false; // Enable for local test

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        SetOwnedCameraOnly();

        if (testMode)
        {
            canMove = true;
        }
    }
    private void OnEnable()
    {
        RaceLevelManager.OnPlayerPossesionEvent += EnableMovement;
    }

    private void OnDisable()
    {
        RaceLevelManager.OnPlayerPossesionEvent -= EnableMovement;
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

    [ContextMenu("Jump")]
    private void Jump()
    {
        rb.AddForce(Vector3.up * jumpEnergy, ForceMode.Impulse);
    }
}
