using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerAnimationHandle : NetworkBehaviour
{
    private Rigidbody rb;
    private Animator animator;

    [SerializeField] private float minSpeedThreshold = 0.2f;
    private void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();

        if (!IsOwner) return;
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleSprintAnimation();
    }

    private void HandleSprintAnimation()
    {
        if (rb.velocity.magnitude > minSpeedThreshold)
        {
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }
    }
}
