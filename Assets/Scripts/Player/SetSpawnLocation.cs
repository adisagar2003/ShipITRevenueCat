#define OnGUI

using UnityEngine;
using Unity.Netcode;

public class SetSpawnLocation : NetworkBehaviour
{
    private Transform assignedSpawnPoint;

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
            Debug.Log($"[Server] Assigning spawn position at {assignedSpawnPoint.position}");
            transform.position = assignedSpawnPoint.position;
            transform.rotation = assignedSpawnPoint.rotation;

            UpdatePositionClientRpc(assignedSpawnPoint.position, assignedSpawnPoint.rotation);
        }
        else
        {
            Debug.LogWarning("[Server] No available spawn point found!");
        }
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsServer)
        {
            Debug.Log($"[ClientRpc] Updating position to {position}");
            transform.position = position;
            transform.rotation = rotation;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && assignedSpawnPoint != null)
        {
            SpawnManager.Instance.FreeSpawnPoint(assignedSpawnPoint);
        }
    }
}
