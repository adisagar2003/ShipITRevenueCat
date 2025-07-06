using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Transform cameraTransform;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxSpeed = 8f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Move(new Vector2(horizontal, vertical));
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

        // Calculate desired velocity
        Vector3 desiredVelocity = moveDirection * moveSpeed;

        // Maintain current Y velocity (gravity)
        desiredVelocity.y = rb.velocity.y;

        // Apply
        rb.velocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);
    }
}
