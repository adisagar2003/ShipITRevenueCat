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
            transform.position = assignedSpawnPoint.position;
            transform.rotation = assignedSpawnPoint.rotation;

            UpdatePositionClientRpc(assignedSpawnPoint.position, assignedSpawnPoint.rotation);
        }
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsServer) // Server already set position
        {
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
