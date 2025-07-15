#define OnGUI

using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public class SetSpawnLocation : NetworkBehaviour
{
    private Transform assignedSpawnPoint;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            AssignNewSpawnPosition();
        }
    }

    public void AssignNewSpawnPosition()
    {
        assignedSpawnPoint = SpawnManager.Instance.GetRandomAvailableSpawnPoint();

        if (assignedSpawnPoint != null)
        {
            Debug.Log($"[Server] Assigning spawn position at {assignedSpawnPoint.position} for ClientID {OwnerClientId}");

            // Server sets authoritative position; NetworkTransform will sync to clients automatically
            rb.position = assignedSpawnPoint.position;
            rb.rotation = assignedSpawnPoint.rotation;
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
        }
    }

#if OnGUI
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 500, 20), $"Spawned at: {assignedSpawnPoint?.position ?? Vector3.zero}");
    }
#endif
}
