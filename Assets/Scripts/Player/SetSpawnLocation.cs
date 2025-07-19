#define OnGUI

using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public class SetSpawnLocation : NetworkBehaviour
{
    [SerializeField] private Transform assignedSpawnPoint;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[SetSpawnLocation] OnNetworkSpawn called for ClientID {OwnerClientId}, IsServer: {IsServer}");
        
        if (IsServer)
        {
            AssignNewSpawnPosition();
        }
    }

    public void AssignNewSpawnPosition()
    {
        if (SpawnManager.Instance == null)
        {
            Debug.LogError("[SetSpawnLocation] SpawnManager.Instance is null!");
            return;
        }

        assignedSpawnPoint = SpawnManager.Instance.GetRandomAvailableSpawnPoint();

        if (assignedSpawnPoint != null)
        {
            Debug.Log($"[Server] Assigning spawn position at {assignedSpawnPoint.position} for ClientID {OwnerClientId}");

            // Server sets authoritative position; NetworkTransform will sync to clients automatically
            transform.position = assignedSpawnPoint.position;
            transform.rotation = assignedSpawnPoint.rotation;
            
            // Also set rigidbody position for physics
            if (rb != null)
            {
                rb.position = assignedSpawnPoint.position;
                rb.rotation = assignedSpawnPoint.rotation;
            }
        }
        else
        {
            Debug.LogWarning("[Server] No available spawn point found!");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && assignedSpawnPoint != null)
        {
            SpawnManager.Instance.FreeSpawnPoint(assignedSpawnPoint);
            Debug.Log($"[Server] Freed spawn point for ClientID {OwnerClientId}");
        }
    }

#if OnGUI
    private void OnGUI()
    {
        if (IsOwner) // Only show for the owner
        {
            GUI.Label(new Rect(10, 30, 500, 20), $"Spawned at: {assignedSpawnPoint?.position ?? Vector3.zero}");
            GUI.Label(new Rect(10, 50, 500, 20), $"ClientID: {OwnerClientId}, IsServer: {IsServer}");
        }
    }
#endif
}
