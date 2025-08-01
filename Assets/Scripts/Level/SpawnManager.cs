using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Centralized spawn manager that handles random player spawning
/// Thread-safe networked singleton for managing player spawn points
/// </summary>
public class SpawnManager : ThreadSafeNetworkSingleton<SpawnManager>
{
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    private List<bool> occupiedSpawnPoints = new List<bool>();
    private readonly object spawnLock = new object();
    protected override void Initialize()
    {
        base.Initialize();
        GameLogger.LogInfo(GameLogger.LogCategory.Gameplay, "SpawnManager initialized");
    }

    private void Start()
    {
        Debug.Log("[SpawnManager] Start called");
        // Find all spawn points in the scene
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        spawnPoints.Clear();
        foreach (GameObject spawnPointObj in spawnPointObjects)
        {
            spawnPoints.Add(spawnPointObj.transform);
            Debug.Log($"[SpawnManager] Found spawn point: {spawnPointObj.name} at {spawnPointObj.transform.position}");
        }
        
        Debug.Log($"[SpawnManager] Total spawn points found: {spawnPoints.Count}");
    }

    public Transform GetRandomAvailableSpawnPoint()
    {
        lock (spawnLock)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, $"GetRandomAvailableSpawnPoint called. Total spawn points: {spawnPoints.Count}");
            
            if (spawnPoints.Count == 0)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Gameplay, "No spawn points configured");
                return null;
            }
            
            // Ensure occupation tracking is initialized
            if (occupiedSpawnPoints.Count != spawnPoints.Count)
            {
                occupiedSpawnPoints = new List<bool>(new bool[spawnPoints.Count]);
            }
            
            List<int> availableIndices = new List<int>();
            
            // Find all available spawn points
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] != null && !occupiedSpawnPoints[i])
                {
                    availableIndices.Add(i);
                }
            }
            
            GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, $"Available spawn points: {availableIndices.Count}");
            
            if (availableIndices.Count == 0)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Gameplay, "No available spawn points!");
                return null;
            }
            
            int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
            occupiedSpawnPoints[randomIndex] = true;
            
            Transform selectedSpawn = spawnPoints[randomIndex];
            GameLogger.LogInfo(GameLogger.LogCategory.Gameplay, $"Selected spawn point {randomIndex}: {selectedSpawn.name} at {selectedSpawn.position}");
            
            return selectedSpawn;
        }
    }

    public void FreeSpawnPoint(Transform spawnPoint)
    {
        if (!IsServer) return;
        
        lock (spawnLock)
        {
            int index = spawnPoints.IndexOf(spawnPoint);
            if (index >= 0 && index < occupiedSpawnPoints.Count)
            {
                occupiedSpawnPoints[index] = false;
                GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, $"Freed spawn point {index}");
            }
            else
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Gameplay, $"Could not free spawn point: not found in list");
            }
        }
    }

    public Transform GetRandomSpawnPoint()
    {
        lock (spawnLock)
        {
            if (spawnPoints.Count == 0) return null;
            
            // Filter out null spawn points
            var validSpawnPoints = new List<Transform>();
            foreach (var spawn in spawnPoints)
            {
                if (spawn != null) validSpawnPoints.Add(spawn);
            }
            
            if (validSpawnPoints.Count == 0)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Gameplay, "All spawn points are null");
                return null;
            }
            
            int randomIndex = Random.Range(0, validSpawnPoints.Count);
            return validSpawnPoints[randomIndex];
        }
    }

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && OnGUI
    private void OnGUI()
    {
        if (!IsServer) return; // Only show debug on server

        // Create a debug window
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical("Box");

        // Title
        GUILayout.Label("SPAWN MANAGER DEBUG", GUI.skin.box);
        GUILayout.Space(10);

        // Basic info
        GUILayout.Label($"Total Spawn Points: {spawnPoints.Count}", GUI.skin.label);
        GUILayout.Label($"Network Role: {(IsServer ? "SERVER" : "CLIENT")}", GUI.skin.label);
        GUILayout.Label($"Is Spawned: {IsSpawned}", GUI.skin.label);
        GUILayout.Space(10);

        GUILayout.Label("SPAWN POINTS STATUS:", GUI.skin.box);

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
                bool isOccupied = i < occupiedSpawnPoints.Count && occupiedSpawnPoints[i];
                string status = isOccupied ? "[OCCUPIED]" : "[FREE]";
                Color originalColor = GUI.color;
                GUI.color = isOccupied ? Color.red : Color.green;

                GUILayout.Label($"Point {i}: {status} - {spawnPoints[i].name}", GUI.skin.label);
                GUI.color = originalColor;
            }
            else
            {
                GUI.color = Color.yellow;
                GUILayout.Label($"Point {i}: [NULL REFERENCE]", GUI.skin.label);
                GUI.color = Color.white;
            }
        }

        GUILayout.Space(10);

        int availableCount = 0;
        int occupiedCount = 0;
        for (int i = 0; i < occupiedSpawnPoints.Count; i++)
        {
            if (occupiedSpawnPoints[i])
                occupiedCount++;
            else
                availableCount++;
        }

        GUILayout.Label("STATISTICS:", GUI.skin.box);
        GUILayout.Label($"Available: {availableCount}", GUI.skin.label);
        GUILayout.Label($"Occupied: {occupiedCount}", GUI.skin.label);
        GUILayout.Label($"Utilization: {(occupiedCount / (float)spawnPoints.Count * 100f):F1}%", GUI.skin.label);

        GUILayout.Space(10);

        if (GUILayout.Button("Test Get Random Spawn"))
        {
            Transform testSpawn = GetRandomAvailableSpawnPoint();
            if (testSpawn != null)
            {
                Debug.Log($"Got spawn point: {testSpawn.name} at {testSpawn.position}");
            }
        }

        if (GUILayout.Button("Test NetworkManager Player Spawning"))
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                Debug.Log($"NetworkManager PlayerPrefab: {(NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null ? NetworkManager.Singleton.NetworkConfig.PlayerPrefab.name : "NULL")}");
                Debug.Log($"Connected Clients: {NetworkManager.Singleton.ConnectedClientsList.Count}");
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    Debug.Log($"Client {client.ClientId}: PlayerObject = {(client.PlayerObject != null ? client.PlayerObject.name : "NULL")}");
                }
            }
            else
            {
                Debug.Log("NetworkManager is null or not server");
            }
        }

        if (GUILayout.Button("Free All Spawn Points"))
        {
            for (int i = 0; i < occupiedSpawnPoints.Count; i++)
            {
                occupiedSpawnPoints[i] = false;
            }
            Debug.Log("All spawn points freed!");
        }

        if (GUILayout.Button("Refresh Spawn Points"))
        {
            Start(); // Re-run the Start method to refresh spawn points
            Debug.Log("Spawn points refreshed!");
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
#endif
}
