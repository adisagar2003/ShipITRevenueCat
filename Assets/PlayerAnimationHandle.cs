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

        #if multiplayer
                if (!IsOwner)
                {
                    debugString += $"[{name}] Not owner, skipping Start initialization.\n";
                    return;
                }
        #endif

        // Get Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            debugString += $"Rigidbody found on same GameObject.\n";
        }
        else
        {
            debugString += $"Rigidbody not found on same GameObject, searching in children...\n";
            rb = GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                debugString += $"Rigidbody found in children: {rb.name}\n";
            }
            else
            {
                debugString += $"ERROR: Rigidbody not found on player or its children.\n";
            }
        }

        // Get Animator
        debugString += $"Animator not found on same GameObject, searching in children...\n";
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            debugString += $"Animator found in children: {animator.name}\n";
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

        HandleSprintAnimation();
        if (rb != null)
        {
            debugString += $"Velocity: {rb.velocity} (Magnitude: {rb.velocity.magnitude:F2})\n";
        }
        else
        {
            debugString += $"Rigidbody is null.\n";
        }

        if (animator != null)
        {
            debugString += $"Animator 'isRunning': {animator.GetBool("isRunning")}\n";
        }
        else
        {
            debugString += $"Animator is null.\n";
        }
    }

    private void HandleSprintAnimation()
    {
        if (rb == null || animator == null)
        {
            debugString += $"Cannot handle sprint animation, missing references.\n";
            return;
        }
        bool isRunning = rb.velocity.magnitude > minSpeedThreshold;
        debugString += $" handle sprint animation, {isRunning}.\n";
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
