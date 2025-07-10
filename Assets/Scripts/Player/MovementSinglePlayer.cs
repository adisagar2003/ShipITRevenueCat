
using UnityEngine;
using Unity.Netcode;

public class PlayerMovementSinglePlayer : MonoBehaviour
{
    private Rigidbody rb;
    private Transform cameraTransform;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxSpeed = 8f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    public void Move(Vector2 input)
    {
        #if DEBUGGING
            if (cameraTransform == null)
            {
                Debug.LogError("CameraTransform is null on " + gameObject.name);
            }
            else
            {
            }
        #endif

        // Camera-relative movement
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * input.y + camRight * input.x;
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
