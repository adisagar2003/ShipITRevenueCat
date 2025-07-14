using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Centralized spawn manager that handles random player spawning
/// Should be attached to a singleton GameObject in the scene
/// </summary>
public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    private List<bool> occupiedSpawnPoints = new List<bool>();
    public static SpawnManager Instance { get; private set; }
    private void Awake()
    {
        Debug.Log("[SpawnManager] Awake called");
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[SpawnManager] Instance set successfully");
        }
        else
        {
            Debug.LogWarning("[SpawnManager] Duplicate SpawnManager found, destroying...");
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        // Find all spawn points in the scene
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        spawnPoints.Clear();
        foreach (GameObject spawnPointObj in spawnPointObjects)
        {
            spawnPoints.Add(spawnPointObj.transform);
        }
        // Initialize occupation tracking
        occupiedSpawnPoints = new List<bool>(new bool[spawnPoints.Count]);
    }

    public Transform GetRandomAvailableSpawnPoint()
    {
        List<int> availableIndices = new List<int>();
        // Find all available spawn points
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (!occupiedSpawnPoints[i])
            {
                availableIndices.Add(i);
            }
        }
        if (availableIndices.Count == 0)
        {
            Debug.LogWarning("No available spawn points!");
            return null;
        }
        int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        occupiedSpawnPoints[randomIndex] = true;
        return spawnPoints[randomIndex];
    }

    public void FreeSpawnPoint(Transform spawnPoint)
    {
        if (!IsServer) return;
        int index = spawnPoints.IndexOf(spawnPoint);
        if (index >= 0)
        {
            occupiedSpawnPoints[index] = false;
        }
    }

    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0) return null;
        int randomIndex = Random.Range(0, spawnPoints.Count);
        return spawnPoints[randomIndex];
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