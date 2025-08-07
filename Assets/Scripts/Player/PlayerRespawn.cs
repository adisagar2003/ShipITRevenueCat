using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerRespawn : NetworkBehaviour
{
    private Rigidbody rb;

    // Cached respawn points to avoid expensive repeated searches
    private static Transform[] cachedRespawnPoints;
    private static bool respawnPointsCached = false;

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
        // Use cached respawn points to avoid expensive repeated searches
        Transform[] respawnPoints = GetCachedRespawnPoints();

        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            Debug.LogWarning("[PlayerRespawn] No respawn points found in scene!");
            return null;
        }

        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var point in respawnPoints)
        {
            // Skip null references (destroyed objects)
            if (point == null) continue;

            float distance = Vector3.Distance(fromPosition, point.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = point;
            }
        }
        return nearest;
    }

    /// <summary>
    /// Gets cached respawn points, initializing cache if needed
    /// </summary>
    private static Transform[] GetCachedRespawnPoints()
    {
        if (!respawnPointsCached || cachedRespawnPoints == null)
        {
            RefreshRespawnPointCache();
        }
        return cachedRespawnPoints;
    }

    /// <summary>
    /// Refreshes the respawn point cache - call this when respawn points change
    /// </summary>
    public static void RefreshRespawnPointCache()
    {
        GameObject[] respawnPointObjects = GameObject.FindGameObjectsWithTag("RespawnPoint");
        cachedRespawnPoints = new Transform[respawnPointObjects.Length];

        for (int i = 0; i < respawnPointObjects.Length; i++)
        {
            cachedRespawnPoints[i] = respawnPointObjects[i].transform;
        }

        respawnPointsCached = true;

#if debug
        Debug.Log($"[PlayerRespawn] Cached {cachedRespawnPoints.Length} respawn points");
#endif
    }

    /// <summary>
    /// Call this when respawn points are added/removed from the scene
    /// </summary>
    public static void InvalidateRespawnPointCache()
    {
        respawnPointsCached = false;
        cachedRespawnPoints = null;
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
