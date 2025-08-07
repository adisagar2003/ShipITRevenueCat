using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

/// <summary>
/// Handles isolated Cinemachine setup for multiplayer in Unity 6.0
/// Ensures each player has their own local camera that doesn't interfere with others.
/// Works with Unity 6's Cinemachine 3.x built-in package.
/// </summary>
[RequireComponent(typeof(Camera))]
public class NetworkCameraRig : NetworkBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Camera Settings")]
    [SerializeField] private int ownerCameraPriority = 10;
    [SerializeField] private int nonOwnerCameraPriority = -1;
    [SerializeField] private bool disableNonOwnerCamera = true;

    [Header("Follow Target")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private bool autoFindFollowTarget = true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        InitializeCameraComponents();
        SetupCameraForOwnership();
        ConfigureFollowTarget();
        HandleMainCameraConflict();

#if debug
        Debug.Log($"<color=cyan>[NetworkCameraRig]</color> <color=white>OnNetworkSpawn - IsOwner: {IsOwner}, Player: {gameObject.name}</color>");
#endif
    }

    private void InitializeCameraComponents()
    {
        gameObject.SetActive(true); // enable camera object
        // Auto-find components if not assigned
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();

        if (cinemachineBrain == null)
            cinemachineBrain = GetComponent<CinemachineBrain>();

        if (virtualCamera == null)
            virtualCamera = GetComponentInChildren<CinemachineCamera>();

        // Validate required components
        if (playerCamera == null)
        {
            Debug.LogError($"[NetworkCameraRig] No Camera component found on {gameObject.name}");
            return;
        }

        if (virtualCamera == null)
        {
            Debug.LogError($"[NetworkCameraRig] No CinemachineCamera found in children of {gameObject.name}");
            return;
        }

#if debug
        Debug.Log($"<color=cyan>[NetworkCameraRig]</color> <color=white>Components initialized - Camera: {playerCamera.name}, VCam: {virtualCamera.name}</color>");
#endif
    }

    private void SetupCameraForOwnership()
    {
        if (IsOwner)
        {
            // Owner: Enable camera and set high priority
            playerCamera.enabled = true;
            virtualCamera.Priority = ownerCameraPriority;

            // Ensure this is the active camera
            if (cinemachineBrain != null)
            {
                cinemachineBrain.enabled = true;
            }

            // Set as main camera for owner (optional)
            if (playerCamera.tag != "MainCamera")
            {
                // Remove MainCamera tag from scene camera if it exists
                var existingMainCamera = Camera.main;
                if (existingMainCamera != null && existingMainCamera != playerCamera)
                {
                    existingMainCamera.tag = "Untagged";
#if debug
                    Debug.Log($"<color=yellow>[NetworkCameraRig]</color> <color=white>Removed MainCamera tag from: {existingMainCamera.name}</color>");
#endif
                }

                playerCamera.tag = "MainCamera";
            }

#if debug
            Debug.Log($"<color=green>[NetworkCameraRig]</color> <color=white>Owner camera enabled - Priority: {virtualCamera.Priority}</color>");
#endif
        }
        else
        {
            // Non-owner: Disable or deprioritize camera
            if (disableNonOwnerCamera)
            {
                playerCamera.enabled = false;
                if (cinemachineBrain != null)
                {
                    cinemachineBrain.enabled = false;
                }
            }

            virtualCamera.Priority = nonOwnerCameraPriority;

#if debug
            Debug.Log($"<color=red>[NetworkCameraRig]</color> <color=white>Non-owner camera disabled/deprioritized - Priority: {virtualCamera.Priority}</color>");
#endif
        }
    }

    private void ConfigureFollowTarget()
    {
        if (!autoFindFollowTarget && followTarget != null)
        {
            virtualCamera.Follow = followTarget;
            virtualCamera.LookAt = followTarget;
            return;
        }

        // Auto-find follow target (should be the player this camera belongs to)
        NetworkThirdPersonController playerController = GetComponentInParent<NetworkThirdPersonController>();
        if (playerController == null)
        {
            // Try to find in siblings
            playerController = GetComponentInParent<Transform>().GetComponentInChildren<NetworkThirdPersonController>();
        }

        if (playerController != null)
        {
            followTarget = playerController.transform;
            virtualCamera.Follow = followTarget;
            virtualCamera.LookAt = followTarget;

#if debug
            Debug.Log($"<color=lightblue>[NetworkCameraRig]</color> <color=white>Follow target set to: {followTarget.name}</color>");
#endif
        }
        else
        {
#if debug
            Debug.LogWarning($"<color=orange>[NetworkCameraRig]</color> <color=white>No follow target found for camera rig on {gameObject.name}</color>");
#endif
        }
    }

    private void HandleMainCameraConflict()
    {
        if (!IsOwner) return;

        // Handle scene-level Main Camera conflicts
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            if (cam != playerCamera && cam.CompareTag("MainCamera"))
            {
                // Disable scene cameras for the owner
                cam.enabled = false;
                cam.tag = "Untagged";

#if debug
                Debug.Log($"<color=yellow>[NetworkCameraRig]</color> <color=white>Disabled scene camera: {cam.name} to prevent conflicts</color>");
#endif
            }
        }
    }

    /// <summary>
    /// Call this to manually set the follow target (useful for character switching, etc.)
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        if (!IsOwner) return; // Only owner can change their camera target

        followTarget = target;
        if (virtualCamera != null)
        {
            virtualCamera.Follow = target;
            virtualCamera.LookAt = target;

#if debug
            Debug.Log($"<color=lightblue>[NetworkCameraRig]</color> <color=white>Follow target manually set to: {target.name}</color>");
#endif
        }
    }

    /// <summary>
    /// Get the camera component (useful for other systems that need camera reference)
    /// </summary>
    public Camera GetCamera()
    {
        return playerCamera;
    }

    /// <summary>
    /// Get the virtual camera component
    /// </summary>
    public CinemachineCamera GetVirtualCamera()
    {
        return virtualCamera;
    }

    public override void OnNetworkDespawn()
    {
        // Cleanup: Re-enable scene camera if this was the owner
        if (IsOwner)
        {
            var sceneCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in sceneCameras)
            {
                if (cam.name.ToLower().Contains("main") && !cam.enabled)
                {
                    cam.enabled = true;
                    cam.tag = "MainCamera";
                    break;
                }
            }
        }

        base.OnNetworkDespawn();
    }
}
