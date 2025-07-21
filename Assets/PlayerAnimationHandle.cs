//#define GUIDebug // Uncomment to enable on-screen debug

using UnityEngine;
using Unity.Netcode;

public class PlayerAnimationHandle : NetworkBehaviour
{
    private Animator animator;
    private Rigidbody rb;
    private bool previousIsRunning = false;
    [SerializeField] private bool isMultiplayer = true;
    [SerializeField] private Transform groundCheckRaycastOriginPoint;
    [SerializeField] private float rayDistance = 0.3f;
    [SerializeField] private float minSpeedThreshold = 0.2f;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    // For singleplayer testing onl
    private void Start()
    {
        if (!isMultiplayer)
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (isMultiplayer)
        {
            if (!IsOwner) return;
        }

        RunCheck();

        GroundCheckAndDebug();
    }

    private void RunCheck()
    {
        bool isRunning = rb.velocity.magnitude > minSpeedThreshold;

        if (isRunning != previousIsRunning)
        {
            animator.SetBool("isRunning", isRunning);
            if (isMultiplayer) SubmitIsRunningServerRpc(isRunning);
            previousIsRunning = isRunning;
        }
    }

    private void GroundCheckAndDebug()
    {
        // Ground check
        bool isGrounded = Physics.Raycast(
            groundCheckRaycastOriginPoint.position,
            Vector3.down,
            out RaycastHit hit,
            rayDistance
        );

        animator.SetBool("isInAir", !isGrounded);

        // Debug ray (yellow if grounded, red if in air)
        Debug.DrawRay(
            groundCheckRaycastOriginPoint.position,
            Vector3.down * rayDistance,
            isGrounded ? Color.yellow : Color.red
        );
    }

    [ServerRpc]
    private void SubmitIsRunningServerRpc(bool isRunning)
    {
        if (animator == null) return;
        animator.SetBool("isRunning", isRunning);
    }
}
