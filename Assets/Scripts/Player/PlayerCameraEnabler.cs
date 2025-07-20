using UnityEngine;
using Unity.Netcode;
using Cinemachine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerCameraEnabler : NetworkBehaviour
{
    private CinemachineVirtualCamera virtualCamera;

    private void Awake()
    {
        Transform cameraTransform = transform.FindDeepChild("Virtual Camera");
        if (cameraTransform != null)
        {
            virtualCamera = cameraTransform.GetComponent<CinemachineVirtualCamera>();
        }
        else
        {
            Debug.LogError("PlayerCameraEnabler: 'VirtualCamera' child not found on player prefab.");
        }
    }

    private void Start()
    {
        if (virtualCamera == null) return;

        if (IsOwner)
        {
            virtualCamera.gameObject.SetActive(true);
            Debug.Log("PlayerCameraEnabler: Virtual Camera enabled for owning player.");
        }
        else
        {
            virtualCamera.gameObject.SetActive(false);
        }
    }
}
