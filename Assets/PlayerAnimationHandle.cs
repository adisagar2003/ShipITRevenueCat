#define multiplayer

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerAnimationHandle : NetworkBehaviour
{
    private Rigidbody rb;
    private Animator animator;

    [SerializeField] private float minSpeedThreshold = 0.2f;
    [SerializeField, TextArea] private string debugString; // Shows in Inspector

    public override void OnNetworkSpawn()
    {
        debugString = $"[{name}] IsOwner: {IsOwner}\n";

        // Get Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            debugString += $"Rigidbody found on same GameObject.\n";
        }
        else
        {
            debugString += $"Rigidbody not found on same GameObject, searching in parent...\n";
            rb = GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                debugString += $"Rigidbody found in parent: {rb.name}\n";
            }
            else
            {
                debugString += $"ERROR: Rigidbody not found on player or its parent.\n";
            }
        }

        // Get Animator
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            debugString += $"Animator found: {animator.name}\n";
        }
        else
        {
            debugString += $"ERROR: Animator not found on player or its children.\n";
        }
    }

    private void Update()
    {
        #if multiplayer
                if (!IsOwner)
                {
                    debugString = $"[{name}] Not owner, skipping Update.\n";
                    return;
                }
        #endif
        debugString = $"[{name}] IsOwner: {IsOwner}\n";
        if (animator != null)
        {
            debugString += $"Animator: {animator.name}\n";
        }
        HandleSprintAnimation();
    }

    private void HandleSprintAnimation()
    {
        if (rb == null || animator == null)
        {
            debugString += $"Cannot handle sprint animation, missing references.\n";
            return;
        }

        bool isRunning = rb.velocity.magnitude > minSpeedThreshold;
        debugString += $"Calculated isRunning: {isRunning}\n";
        animator.SetBool("isRunning", isRunning);
        SubmitIsRunningServerRpc(isRunning);
    }

    [ServerRpc]
    private void SubmitIsRunningServerRpc(bool isRunning)
    {
        debugString += $"[Server] Setting isRunning to: {isRunning}\n";
        animator.SetBool("isRunning", isRunning);

    }

    private void OnGUI()
    {
#if multiplayer
        if (!IsOwner) return;
#endif

        GUI.Label(new Rect(20, 10, 600, 200), debugString);
    }
}
