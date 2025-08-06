//#define GUIDebug

using UnityEngine;
using Unity.Netcode;

public class PlayerAnimationHandle : NetworkBehaviour
{
    private Animator animator;
    private Rigidbody rb;
    private NetworkThirdPersonController networkController;
    private bool previousIsRunning = false;

    [SerializeField] private bool isMultiplayer = true;
    [SerializeField] private float minSpeedThreshold = 0.2f;

    public override void OnNetworkSpawn()
    {
        InitializeReferences();
    }

    private void Start()
    {
        if (!isMultiplayer)
        {
            InitializeReferences();
        }
    }

    private void InitializeReferences()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        networkController = GetComponent<NetworkThirdPersonController>();
    }

    private void Update()
    {
        if (isMultiplayer && !IsOwner) return;

        RunCheck();
        GroundCheck();
    }

    private void RunCheck()
    {
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f; // ignore vertical speed

        bool isRunning = horizontalVelocity.magnitude > minSpeedThreshold;

        if (isRunning != previousIsRunning && networkController.IsGrounded)
        {
            animator.SetBool("isRunning", isRunning);
            if (isMultiplayer) SubmitIsRunningRpc(isRunning);
            previousIsRunning = isRunning;
        }
    }

    private void GroundCheck()
    {
        bool isInAir = !networkController.IsGrounded;
        animator.SetBool("isInAir", isInAir);
        if (isMultiplayer) SubmitIsInAirRpc(isInAir);
    }

    [Rpc(SendTo.Server)]
    private void SubmitIsRunningRpc(bool isRunning)
    {
        if (animator == null) return;
        animator.SetBool("isRunning", isRunning);
    }

    [Rpc(SendTo.Server)]
    private void SubmitIsInAirRpc(bool isInAir)
    {
        if (animator == null) return;
        animator.SetBool("isInAir", isInAir);
    }

    public void TriggerJump()
    {
        if (isMultiplayer && !IsOwner) return;
        animator.SetTrigger("Jump");
    }
}
