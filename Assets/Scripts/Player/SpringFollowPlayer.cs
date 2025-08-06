using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Spring object that follows the player's position but maintains its own rotation.
/// Used as a stable camera target for Cinemachine to prevent jarring camera spins
/// when the player rotates quickly (e.g., changing movement direction).
/// Only copies X and Z position, preserving Y offset for proper camera positioning.
/// </summary>
public class SpringFollowPlayer : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Position Settings")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private bool followY = false; // Usually false for camera stability

    private void Start()
    {
        if (autoFindPlayer && playerTarget == null)
        {
            // Try to find NetworkThirdPersonController on parent or this GameObject
            NetworkThirdPersonController controller = GetComponentInParent<NetworkThirdPersonController>();
            if (controller == null)
                controller = FindFirstObjectByType<NetworkThirdPersonController>();

            if (controller != null)
            {
                playerTarget = controller.transform;
                Debug.Log($"SpringFollowPlayer: Auto-found player target: {playerTarget.name}");
            }
            else
            {
                Debug.LogWarning("SpringFollowPlayer: No NetworkThirdPersonController found for auto-target");
            }
        }
    }

    private void LateUpdate()
    {
        if (playerTarget == null) return;

        Vector3 newPosition = transform.position;

        // Follow X and Z position with offset
        newPosition.x = playerTarget.position.x + positionOffset.x;
        newPosition.z = playerTarget.position.z + positionOffset.z;

        // Optionally follow Y position
        if (followY)
        {
            newPosition.y = playerTarget.position.y + positionOffset.y;
        }

        transform.position = newPosition;
        // Note: Rotation is intentionally NOT copied to prevent camera spinning
    }
}
