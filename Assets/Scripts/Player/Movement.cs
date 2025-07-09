
#define DEBUGGING

using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    private Rigidbody rb;
    private Transform cameraTransform;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxSpeed = 8f;

    private void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        if (IsOwner)
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }
    }

    public void Move(Vector2 input)
    {
        #region DEBUGGING
            if (cameraTransform == null)
            {
                Debug.LogError("CameraTransform is null on " + gameObject.name);
            }
            else
            {
                Debug.Log("CameraTransform is " + cameraTransform.name);
            }
        #endregion
        if (!IsOwner) return;

        // Camera-relative movement
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * input.y + camRight * input.x;
            Debug.Log("Move  direction " + moveDirection);
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
