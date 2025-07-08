using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SphereMovement : NetworkBehaviour
{
    private Rigidbody rb;
    private Vector3 moveDirection;
    [SerializeField] private float moveForce = 14f;
    [SerializeField] private float maxSpeed = 10.0f;
    [SerializeField] private float stopDrag = 5f; // How quickly it stops

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        // Calculate movement direction
        moveDirection = new Vector3(horizontal, 0, vertical).normalized;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        ApplyMovement();
    }

    void ApplyMovement()
    {
        
        if (moveDirection.magnitude > 0.1f)
        {
            // Calculate force to apply
            Vector3 forceToApply = moveDirection * moveForce;
            // Limit maximum speed
            Vector3 currentVelocity = rb.velocity;
            currentVelocity.y = 0; // Ignore Y velocity for speed check

            if (currentVelocity.magnitude < maxSpeed)
            {
                rb.AddForce(forceToApply, ForceMode.Force);
            }
            else
            {
                // If at max speed, only allow force in new directions
                Vector3 velocityDirection = currentVelocity.normalized;
                Vector3 newDirection = moveDirection;

                // Only apply force if moving in a significantly different direction
                if (Vector3.Dot(velocityDirection, newDirection) < 0.8f)
                {
                    rb.AddForce(forceToApply * 0.5f, ForceMode.Force);
                }
            }
        }
        else
        {
            // No input - apply stopping force
            Vector3 horizontalVelocity = rb.velocity;
            horizontalVelocity.y = 0; // Keep Y velocity for gravity

            if (horizontalVelocity.magnitude > 0.1f)
            {
                Vector3 stopForce = -horizontalVelocity.normalized * stopDrag;
                rb.AddForce(stopForce, ForceMode.Force);
            }
        }
    }
}