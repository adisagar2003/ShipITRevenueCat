using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationHandle : MonoBehaviour
{
    private Rigidbody rb;
    private Animator animator;

    [SerializeField] private float minSpeedThreshold = 0.2f;
    private void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
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
