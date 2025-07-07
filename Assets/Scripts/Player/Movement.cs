using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Transform cameraTransform;
    private JoystickDetection detection;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxSpeed = 8f;

    private void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    private void FixedUpdate()
    {


    }

    public void Move(Vector2 input)
    {
        // Camera-relative movement
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        
        
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * input.y + camRight * input.x;
        Vector3 desiredVelocity = moveDirection * moveSpeed;

        // Maintain current Y velocity (gravity)
        desiredVelocity.y = rb.velocity.y;

        rb.velocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);
    }
}
