using UnityEngine;
using Unity.Netcode;

public class PlayerAnimationHandle : NetworkBehaviour
{
    private Rigidbody rb;
    private Animator animator;

    [SerializeField] private float minSpeedThreshold = 0.2f;
    [SerializeField, TextArea] private string debugString;

    private NetworkVariable<bool> isRunningNetVar = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

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
        SubmitIsRunningServerRpc(localIsRunning);
    }

    [ServerRpc]
    private void SubmitIsRunningServerRpc(bool isRunning)
    {
        isRunningNetVar.Value = isRunning;
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
