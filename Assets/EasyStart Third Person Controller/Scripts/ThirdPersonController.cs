
using UnityEngine;
using UnityEngine.InputSystem;

/*
    This file has a commented version with details about how each line works.
    The commented version contains code that is easier and simpler to read. This file is minified.
*/


/// <summary>
/// Main script for third-person movement of the character in the game.
/// </summary>
public class ThirdPersonController : MonoBehaviour
{

    [Tooltip("Speed ​​at which the character moves. It is not affected by gravity or jumping.")]
    public float velocity = 5f;
    [Tooltip("The higher the value, the higher the character will jump.")]
    public float jumpForce = 18f;
    [Tooltip("Stay in the air. The higher the value, the longer the character floats before falling.")]
    public float jumpTime = 0.85f;
    [Space]
    [Tooltip("Force that pulls the player down. Changing this value causes all movement, jumping and falling to be changed as well.")]
    public float gravity = 9.8f;

    float jumpElapsedTime = 0;

    // Player states
    bool isJumping = false;

    // Movement input cache
    private Vector2 currentInput = Vector2.zero;

    Rigidbody rb;
    private PlayerMovement playerMovement;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    // Input should be handled through command pattern, not directly in Update
    // Remove this method as input handling is done via InputHandler and commands

    // With the inputs and animations defined, FixedUpdate is responsible for applying movements and actions to the player
    private void FixedUpdate()
    {

        // Direction movement using cached input
        float directionX = currentInput.x * velocity * Time.deltaTime;
        float directionZ = currentInput.y * velocity * Time.deltaTime;
        float directionY = 0;

        // Jump handler
        if ( isJumping )
        {

            // Apply inertia and smoothness when climbing the jump
            // It is not necessary when descending, as gravity itself will gradually pulls
            directionY = Mathf.SmoothStep(jumpForce, jumpForce * 0.30f, jumpElapsedTime / jumpTime) * Time.deltaTime;

            // Jump timer
            jumpElapsedTime += Time.deltaTime;
            if (jumpElapsedTime >= jumpTime)
            {
                isJumping = false;
                jumpElapsedTime = 0;
            }
        }

        // Add gravity to Y axis
        directionY = directionY - gravity * Time.deltaTime;


        // --- Character rotation ---

        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        // Relate the front with the Z direction (depth) and right with X (lateral movement)
        forward = forward * directionZ;
        right = right * directionX;

        if (directionX != 0 || directionZ != 0)
        {
            float angle = Mathf.Atan2(forward.x + right.x, forward.z + right.z) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.15f);
        }

        // --- End rotation ---


        Vector3 verticalDirection = Vector3.up * directionY;
        Vector3 horizontalDirection = forward + right;

        Vector3 moviment = verticalDirection + horizontalDirection;

        // Apply the calculated movement to the rigidbody
        rb.linearVelocity = moviment;
    }
    
    // Public methods for command pattern integration
    public void Move(Vector2 input)
    {
        currentInput = input;
    }
    
    public void Jump()
    {
        // Only jump if not already jumping (simple ground check)
        if (!isJumping)
        {
            isJumping = true;
            jumpElapsedTime = 0;
        }
    }



}
