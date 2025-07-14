#define multiplayer
#define GUIDebug
using UnityEngine;
using Unity.Netcode;

public class PlayerAnimationHandle : NetworkBehaviour
{
    private Rigidbody rb;
    private Animator animator;

    [SerializeField] private float minSpeedThreshold = 0.2f;
    [SerializeField, TextArea] private string debugString;
    private bool previousIsRunning = false;

    private NetworkVariable<bool> isRunningNetVar = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        rb = GetComponentInParent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning("Animator not found on PlayerAnimationHandle!");

        if (rb == null)
            Debug.LogWarning("Rigidbody not found on PlayerAnimationHandle!");

        isRunningNetVar.OnValueChanged += (oldVal, newVal) =>
        {
            if (animator != null)
            {
                animator.SetBool("isRunning", newVal);
                debugString += $"[Sync] isRunning changed to: {newVal}\n";
            }
        };
    }
    private void Update()
    {
#if multiplayer
        if (!IsOwner) return;
#endif
        HandleSprintAnimation();
    }

    private void HandleSprintAnimation()
    {
        if (rb == null) return;

        bool localIsRunning = rb.velocity.magnitude > minSpeedThreshold;
        if (localIsRunning != previousIsRunning)
        {
            SubmitIsRunningServerRpc();
            previousIsRunning = localIsRunning;
        }
    }

    [ServerRpc]
    private void SubmitIsRunningServerRpc()
    {
        if (rb == null) return;

        // Server decides if player is running based on authoritative velocity
        bool serverIsRunning = rb.velocity.magnitude > minSpeedThreshold;
        isRunningNetVar.Value = serverIsRunning;
    }

#if GUIDebug
    private void OnGUI()
    {
#if multiplayer
        if (!IsOwner) return;
#endif
        GUI.Label(new Rect(20, 10, 600, 200), debugString);
    }
#endif
}
