using UnityEngine;
using Unity.Netcode;
using System.Collections;
public class PlayerRespawn : NetworkBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void RequestRespawn()
    {
        if (!IsSpawned) return;
        
        if (IsServer)
        {
            RespawnPlayerAtNearestPoint();
        }
        else if (IsOwner)
        {
            RequestRespawnRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestRespawnRpc()
    {
        Debug.Log("Server received respawn request, proceeding to respawn player.");
        RespawnPlayerAtNearestPoint();
    }

    public void RespawnPlayerAtNearestPoint()
    {
        Transform respawnPoint = FindNearestRespawnPoint(transform.position);
        if (respawnPoint != null)
        {
            StartCoroutine(RespawnRoutine(respawnPoint.position));
            RespawnRpc(respawnPoint.position);
        }
    }

    [Rpc(SendTo.NotServer)]
    private void RespawnRpc(Vector3 position)
    {
        if (IsOwner)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            transform.position = position;
            rb.isKinematic = false;
            Debug.Log("Client moved to respawn position via ClientRPC");
        }
    }

    private Transform FindNearestRespawnPoint(Vector3 fromPosition)
    {
        GameObject[] respawnPoints = GameObject.FindGameObjectsWithTag("RespawnPoint");
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var point in respawnPoints)
        {
            float distance = Vector3.Distance(fromPosition, point.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = point.transform;
            }
        }
        return nearest;
    }

    private IEnumerator RespawnRoutine(Vector3 targetPosition)
    {
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        yield return null;
        transform.position = targetPosition;
        yield return null;
        rb.isKinematic = false;
        Debug.Log("Player respawned at " + targetPosition);
    }

}
