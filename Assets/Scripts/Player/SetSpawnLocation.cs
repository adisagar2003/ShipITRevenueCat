#define OnGUI

using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public class SetSpawnLocation : NetworkBehaviour
{
    [Header("Spawn Offset Settings")]
    [SerializeField] private float spawnOffsetRange = 5f; // Random offset range on X-axis
    [SerializeField] private Vector3 baseSpawnPosition = Vector3.zero; // Base spawn position if no spawn points
    
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[SetSpawnLocation] OnNetworkSpawn called for ClientID {OwnerClientId}, IsHost: {IsHost}");
        
        if (IsHost)
        {
            AssignRandomSpawnPosition();
        }
    }

    /// <summary>
    /// Assigns a random spawn position with X-axis offset to prevent players from spawning at the same location
    /// </summary>
    public void AssignRandomSpawnPosition()
    {
        Vector3 spawnPosition;
        Quaternion spawnRotation = Quaternion.identity;
        
        // Try to get a spawn point from SpawnManager first
        if (SpawnManager.Instance != null)
        {
            Transform spawnPoint = SpawnManager.Instance.GetRandomSpawnPoint();
            if (spawnPoint != null)
            {
                spawnPosition = spawnPoint.position;
                spawnRotation = spawnPoint.rotation;
                Debug.Log($"[Host] Using SpawnManager spawn point: {spawnPosition}");
            }
            else
            {
                spawnPosition = baseSpawnPosition;
                Debug.Log($"[Host] No spawn points found, using base position: {spawnPosition}");
            }
        }
        else
        {
            spawnPosition = baseSpawnPosition;
            Debug.Log($"[Host] SpawnManager not available, using base position: {spawnPosition}");
        }
        
        // Add random X-axis offset to prevent players from spawning at the exact same location
        float randomXOffset = Random.Range(-spawnOffsetRange, spawnOffsetRange);
        spawnPosition += new Vector3(randomXOffset, 0f, 0f);
        
        Debug.Log($"[Host] Assigning spawn position at {spawnPosition} for ClientID {OwnerClientId} (X offset: {randomXOffset:F2})");

        // Host sets authoritative position; NetworkTransform will sync to clients automatically
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        
        // Also set rigidbody position for physics
        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.rotation = spawnRotation;
        }
    }

#if OnGUI
    private void OnGUI()
    {
        if (IsOwner) // Only show for the owner
        {
            GUI.Label(new Rect(10, 30, 500, 20), $"Spawned at: {transform.position}");
            GUI.Label(new Rect(10, 50, 500, 20), $"ClientID: {OwnerClientId}, IsHost: {IsHost}");
        }
    }
#endif
}
