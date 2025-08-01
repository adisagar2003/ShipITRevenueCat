using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerArrow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private string targetTag = "FinishLine";
    
    [Header("Offset Settings")]
    [SerializeField] private Vector3 offset = Vector3.zero;
    
    [Header("Rotation Settings")]
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLine = false;
    
    private GameObject target;
    private Transform targetTransform;

    void Start()
    {
        FindTarget();
    }

    void Update()
    {
        // If target is null or destroyed, try to find it again
        if (target == null)
        {
            FindTarget();
            return;
        }

        PointTowardsTarget();
    }

    /// <summary>
    /// Finds the GameObject with the specified tag
    /// </summary>
    private void FindTarget()
    {
        target = GameObject.FindGameObjectWithTag(targetTag);
        
        if (target != null)
        {
            targetTransform = target.transform;
            Debug.Log($"PlayerArrow found target: {target.name}");
        }
        else
        {
            Debug.LogWarning($"PlayerArrow could not find GameObject with tag '{targetTag}'");
        }
    }

    /// <summary>
    /// Rotates the arrow to point towards the target
    /// </summary>
    private void PointTowardsTarget()
    {
        if (targetTransform == null) return;

        // Calculate direction to target with offset
        Vector3 targetPosition = targetTransform.position + offset;
        Vector3 direction = (targetPosition - transform.position).normalized;

        // Calculate the rotation needed to look at the target
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Apply rotation (smooth or instant)
        if (smoothRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Manually set a new target
    /// </summary>
    /// <param name="newTarget">The new target to point towards</param>
    public void SetTarget(GameObject newTarget)
    {
        target = newTarget;
        targetTransform = newTarget != null ? newTarget.transform : null;
    }

    /// <summary>
    /// Get the current target
    /// </summary>
    /// <returns>Current target GameObject</returns>
    public GameObject GetTarget()
    {
        return target;
    }

    /// <summary>
    /// Get the distance to the target
    /// </summary>
    /// <returns>Distance to target, or -1 if no target</returns>
    public float GetDistanceToTarget()
    {
        if (targetTransform == null) return -1f;
        
        Vector3 targetPosition = targetTransform.position + offset;
        return Vector3.Distance(transform.position, targetPosition);
    }

    void OnDrawGizmos()
    {
        if (!showDebugLine || targetTransform == null) return;

        // Draw a line from arrow to target
        Vector3 targetPosition = targetTransform.position + offset;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, targetPosition);
        
        // Draw a sphere at the target position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
    }
}
