using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AddAttachmentsToCharacter : NetworkBehaviour
{
    [Header("Customization References")]
    [SerializeField] private SOCustomizationDatabase customizationDatabase;

    [SerializeField] private Transform hatsContainer;
    [SerializeField] private SkinnedMeshRenderer bodyRenderer;
    [SerializeField] private SkinnedMeshRenderer headRenderer;

    private GameObject currentHatInstance;
    private GameObject currentGlassesInstance;
    private Material originalBodyMaterial;
    private Material originalHeadMaterial;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        
        // Store original materials for cleanup
        if (bodyRenderer != null)
        {
            originalBodyMaterial = bodyRenderer.material;
        }
        if (headRenderer != null)
        {
            originalHeadMaterial = headRenderer.material;
        }
        
        ApplyCustomization();
    }
    
    public override void OnNetworkDespawn()
    {
        CleanupCustomization();
        base.OnNetworkDespawn();
    }
    
    private void OnDestroy()
    {
        CleanupCustomization();
    }

    private void ApplyCustomization()
    {
        // Retrieve saved indices   
        int bodyIndex = PlayerPrefs.GetInt(GameConstants.PlayerPrefsKeys.BODY_INDEX, 0);
        int headIndex = PlayerPrefs.GetInt(GameConstants.PlayerPrefsKeys.HEAD_INDEX, 0);
        int glassesIndex = PlayerPrefs.GetInt(GameConstants.PlayerPrefsKeys.GLASSES_INDEX, 0);

        GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, $"Applying customization - Body: {bodyIndex}, Head: {headIndex}, Glasses: {glassesIndex}");
  
        // ---- Apply Glasses ----
        GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, "Starting glass attachment process");
        
        // Validate customization database
        if (customizationDatabase == null)
        {
            GameLogger.LogError(GameLogger.LogCategory.Gameplay, "Customization database is null - cannot apply glasses");
            return;
        }
        
        if (customizationDatabase.glassPrefabs == null)
        {
            GameLogger.LogError(GameLogger.LogCategory.Gameplay, "Glass prefabs list is null in customization database");
            return;
        }
        
        // Validate hats container - try fallback if not assigned
        if (hatsContainer == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Gameplay, "Hats container is not assigned - attempting to find 'Hats' transform");
            hatsContainer = transform.FindDeepChild("Hats");
            
            if (hatsContainer == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Gameplay, "Could not find 'Hats' transform in hierarchy - cannot attach glasses");
                return;
            }
            else
            {
                GameLogger.LogInfo(GameLogger.LogCategory.Gameplay, $"Found Hats transform automatically: {hatsContainer.name}");
            }
        }
        
        GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, $"Hats container found: {hatsContainer.name}");
        
        // Validate glasses index
        if (glassesIndex < 0 || glassesIndex >= customizationDatabase.glassPrefabs.Count)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Gameplay, $"Glasses index {glassesIndex} is out of bounds (0-{customizationDatabase.glassPrefabs.Count - 1})");
            return;
        }
        
        GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, $"Glass prefabs available: {customizationDatabase.glassPrefabs.Count}, selected index: {glassesIndex}");
        
        // Clean up existing glasses and any other children in hats container
        if (currentGlassesInstance != null)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, "Cleaning up existing glasses instance");
            ResourceManager.SafeDestroy(currentGlassesInstance);
            currentGlassesInstance = null;
        }
        
        // Also clear any other children that might exist in the hats container
        int childCount = hatsContainer.childCount;
        if (childCount > 0)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, $"Clearing {childCount} existing children from hats container");
            foreach (Transform child in hatsContainer)
            {
                ResourceManager.SafeDestroy(child.gameObject);
            }
        }
        
        // Get the glass prefab
        var glassPrefab = customizationDatabase.glassPrefabs[glassesIndex];
        if (glassPrefab == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Gameplay, $"Glass prefab at index {glassesIndex} is null");
            return;
        }
        
        GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, $"Instantiating glass prefab: {glassPrefab.name}");
        
        // Instantiate new glasses
        currentGlassesInstance = Instantiate(glassPrefab, hatsContainer);
        if (currentGlassesInstance != null)
        {
            currentGlassesInstance.SetActive(true);
            ResourceManager.TrackObject(currentGlassesInstance, $"Glasses_{GetInstanceID()}");
            GameLogger.LogInfo(GameLogger.LogCategory.Gameplay, $"Successfully attached glasses '{glassPrefab.name}' to {hatsContainer.name}");
        }
        else
        {
            GameLogger.LogError(GameLogger.LogCategory.Gameplay, "Failed to instantiate glass prefab");
        }

        // ---- Apply Body Mesh ----
        if (customizationDatabase != null && customizationDatabase.bodyMeshes != null &&
            customizationDatabase.bodyMeshes.Count > bodyIndex && bodyRenderer != null)
        {
            var bodyMesh = customizationDatabase.bodyMeshes[bodyIndex];
            if (bodyMesh != null)
            {
                bodyRenderer.sharedMesh = bodyMesh;
            }
        }

        // ---- Apply Head Mesh ----
        if (customizationDatabase != null && customizationDatabase.headMeshes != null &&
            customizationDatabase.headMeshes.Count > headIndex && headRenderer != null)
        {
            var headMesh = customizationDatabase.headMeshes[headIndex];
            if (headMesh != null)
            {
                headRenderer.sharedMesh = headMesh;
            }
        }
    }
    
    /// <summary>
    /// Clean up all customization objects and reset materials.
    /// </summary>
    private void CleanupCustomization()
    {
        // Clean up glasses instance
        if (currentGlassesInstance != null)
        {
            ResourceManager.SafeDestroy(currentGlassesInstance);
            currentGlassesInstance = null;
        }
        
        // Clean up hat instance
        if (currentHatInstance != null)
        {
            ResourceManager.SafeDestroy(currentHatInstance);
            currentHatInstance = null;
        }
        
        // Reset materials to originals to prevent material leaks
        if (bodyRenderer != null && originalBodyMaterial != null)
        {
            bodyRenderer.material = originalBodyMaterial;
        }
        
        if (headRenderer != null && originalHeadMaterial != null)
        {
            headRenderer.material = originalHeadMaterial;
        }
        
        GameLogger.LogDebug(GameLogger.LogCategory.Gameplay, "Character customization cleaned up");
    }
}
