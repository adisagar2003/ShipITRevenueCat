#define multiplayer
//#define GUIDebug // Uncomment to enable on-screen debug

using UnityEngine;
using Unity.Netcode;

public class PlayerAnimationHandle : NetworkBehaviour
{
    private Animator animator;
    private Rigidbody rb;
    private bool previousIsRunning = false;

    [SerializeField] private float minSpeedThreshold = 0.2f;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        bool isRunning = rb.velocity.magnitude > minSpeedThreshold;

        if (isRunning != previousIsRunning)
        {
            animator.SetBool("isRunning", isRunning);
            SubmitIsRunningServerRpc(isRunning);
            previousIsRunning = isRunning;
        }
    }

    [ServerRpc]
    private void SubmitIsRunningServerRpc(bool isRunning)
    {
        if (animator == null) return;
        animator.SetBool("isRunning", isRunning);
    }
}
