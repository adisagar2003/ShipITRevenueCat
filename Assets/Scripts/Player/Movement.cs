
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

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        SetOwnedCameraOnly();
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

    /// Ensures that only the local player's camera is active in a multiplayer context.
    private void SetOwnedCameraOnly()
    {
        if (IsOwner)
        {
            Camera cam = GetComponentInChildren<Camera>();
            cameraTransform = cam.transform;
            if (cam != null)
            {
                cam.gameObject.SetActive(true);
            }
        }
        else
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.gameObject.SetActive(false);
            }
        }
    }

    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
    }

    public void Move(Vector2 input)
    {
        if (!canMove) return;
        #if DEBUGGING
            if (cameraTransform == null)
            {
                Debug.LogError("CameraTransform is null on " + gameObject.name);
            }
            else
            {
                Debug.Log("CameraTransform is " + cameraTransform.name);
            }
        #endif
        if (!IsOwner) return;

        // Camera-relative movement
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * input.y + camRight * input.x;
        #if DEBUGGING
                Debug.Log("Move  direction " + moveDirection);
        #endif
        Vector3 desiredVelocity = moveDirection * moveSpeed;

        // Maintain Y velocity
        desiredVelocity.y = rb.velocity.y;

        rb.velocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);


        if (moveDirection.sqrMagnitude > 0.1f)
        {
            Quaternion directionToFace = Quaternion.LookRotation(moveDirection);
            rb.rotation = directionToFace;
        }


    }
}
