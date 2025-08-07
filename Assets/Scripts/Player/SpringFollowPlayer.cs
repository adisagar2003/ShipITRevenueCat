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
                controller = FindLocalOwnedController();

            if (controller != null && (controller.IsOwner || GetComponentInParent<NetworkThirdPersonController>() != null))
            {
                playerTarget = controller.transform;
#if debug
                Debug.Log($"SpringFollowPlayer: Auto-found player target: {playerTarget.name} (IsOwner: {controller.IsOwner})");
#endif
            }
            else
            {
#if debug
                Debug.LogWarning("SpringFollowPlayer: No locally owned NetworkThirdPersonController found for auto-target");
#endif
            }
        }
    }

    /// <summary>
    /// Finds the locally owned NetworkThirdPersonController - multiplayer safe
    /// </summary>
    private NetworkThirdPersonController FindLocalOwnedController()
    {
        // Find all controllers and select the locally owned one
        NetworkThirdPersonController[] allControllers = FindObjectsByType<NetworkThirdPersonController>(FindObjectsSortMode.None);
        foreach (var controller in allControllers)
        {
            if (controller.IsOwner)
            {
                return controller;
            }
        }
        
#if debug
        Debug.LogWarning($"SpringFollowPlayer: No locally owned controller found among {allControllers.Length} controllers");
        foreach (var ctrl in allControllers)
        {
            Debug.Log($"  - Controller: {ctrl.name}, IsOwner: {ctrl.IsOwner}, NetworkObjectId: {ctrl.NetworkObjectId}");
        }
#endif
        return null;
    }

    private void LateUpdate()
    {
        // Retry finding target if we don't have one (handles late network spawning)
        if (playerTarget == null && autoFindPlayer)
        {
            NetworkThirdPersonController controller = GetComponentInParent<NetworkThirdPersonController>();
            if (controller == null)
                controller = FindLocalOwnedController();
                
            if (controller != null && (controller.IsOwner || GetComponentInParent<NetworkThirdPersonController>() != null))
            {
                playerTarget = controller.transform;
#if debug
                Debug.Log($"SpringFollowPlayer: Late-found player target: {playerTarget.name} (IsOwner: {controller.IsOwner})");
#endif
            }
        }
        
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
