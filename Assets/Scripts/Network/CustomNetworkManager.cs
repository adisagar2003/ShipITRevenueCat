using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Custom NetworkManager that overrides player spawn positioning to prevent 
/// clients and server from spawning at the exact same location.
/// Adds random offsets on the X-axis to separate players.
/// </summary>
public class CustomNetworkManager : NetworkManager
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnOffsetRange = 5f; // Units to offset players on X-axis
    [SerializeField] private Vector3 baseSpawnPosition = Vector3.zero; // Base spawn position
    
    /// <summary>
    /// Overrides the default player spawn position to add random X-axis offset
    /// </summary>
    public override Vector3 GetPlayerSpawnPosition(ulong clientId, GameObject playerPrefab)
    {
        // Generate a random offset on the X-axis
        float randomXOffset = Random.Range(-spawnOffsetRange, spawnOffsetRange);
        
        // Calculate the spawn position with offset
        Vector3 spawnPosition = baseSpawnPosition + new Vector3(randomXOffset, 0f, 0f);
        
        Debug.Log($"<color=#00FFFF><b>[CustomNetworkManager]</b></color> " +
                  $"Spawning player for ClientId {clientId} at position {spawnPosition} " +
                  $"(offset: {randomXOffset:F2})");
        
        return spawnPosition;
    }
    
    /// <summary>
    /// Overrides player spawn rotation - keeps default rotation for now
    /// </summary>
    public override Quaternion GetPlayerSpawnRotation(ulong clientId, GameObject playerPrefab)
    {
        return Quaternion.identity; // Default forward rotation
    }
}