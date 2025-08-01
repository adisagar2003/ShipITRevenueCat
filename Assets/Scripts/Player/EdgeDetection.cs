#define DEBUG_EDGE_DETECTION
using UnityEngine;

/// <summary>
/// Handles edge detection for player movement using SphereCast for better coverage
/// </summary>
public class EdgeDetection : MonoBehaviour
{
    [Header("Edge Detection Settings")]
    [SerializeField] private float edgeCheckDistance = 0.6f;
    [SerializeField] private float edgeCheckHeight = 0.1f;
    [SerializeField] private LayerMask obstacleMask = -1;
    [SerializeField] private bool enableEdgeDetection = true;

    [Header("Sphere Cast Settings")]
    [SerializeField] private float sphereRadius = 0.6f;
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    // Debug visualization
    private Vector3 lastCheckedDirection;
    private bool wasLastMovementBlocked;

    public bool IsMovementBlocked(Vector3 moveDirection)
    {
        if (!enableEdgeDetection || moveDirection.sqrMagnitude <= 0.1f)
            return false;

        lastCheckedDirection = moveDirection;
        Vector3 castOrigin = transform.position + Vector3.up * edgeCheckHeight + positionOffset;

        bool isBlocked = Physics.SphereCast(
            castOrigin,
            sphereRadius,
            moveDirection.normalized,
            out RaycastHit hit,
            edgeCheckDistance,
            obstacleMask
        );

        wasLastMovementBlocked = isBlocked;

#if DEBUG_EDGE_DETECTION
        if (isBlocked)
            Debug.Log($"<color=orange>[SPHERE CAST - COLLISION]</color> Hit {hit.collider.name} at distance {hit.distance:F2}");
        else
            Debug.Log($"<color=green>[SPHERE CAST - CLEAR]</color> No obstacles in direction: {moveDirection.normalized}");
#endif

        return isBlocked;
    }

    public void SetEdgeDetectionEnabled(bool enabled) => enableEdgeDetection = enabled;
    public bool IsEdgeDetectionEnabled => enableEdgeDetection;

    private void OnDrawGizmos()
    {
        if (!enableEdgeDetection) return;

        Vector3 castOrigin = transform.position + Vector3.up * edgeCheckHeight + positionOffset;
        
        // Draw origin sphere - always visible
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(castOrigin, sphereRadius);

        // Draw sphere at end position if we have a direction
        if (lastCheckedDirection.sqrMagnitude > 0.1f)
        {
            Vector3 endPosition = castOrigin + lastCheckedDirection.normalized * edgeCheckDistance;
            Gizmos.color = wasLastMovementBlocked ? Color.red : Color.green;
            Gizmos.DrawWireSphere(endPosition, sphereRadius);
            
            // Draw line showing cast direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(castOrigin, endPosition);
        }

        // Show offset visualization if offset is not zero
        if (positionOffset != Vector3.zero)
        {
            Vector3 originalPosition = transform.position + Vector3.up * edgeCheckHeight;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(originalPosition, castOrigin);
            Gizmos.DrawWireSphere(originalPosition, 0.05f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!enableEdgeDetection) return;
        
        Vector3 castOrigin = transform.position + Vector3.up * edgeCheckHeight + positionOffset;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(castOrigin, edgeCheckDistance);
    }
}
