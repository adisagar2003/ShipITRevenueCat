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

  
        // ---- Apply Glasses ----
        if (customizationDatabase != null && customizationDatabase.glassPrefabs != null && 
            customizationDatabase.glassPrefabs.Count > glassesIndex && hatsContainer != null)
        {
            // Clean up existing glasses
            if (currentGlassesInstance != null)
            {
                ResourceManager.SafeDestroy(currentGlassesInstance);
                currentGlassesInstance = null;
            }
            
            // Instantiate new glasses
            var glassPrefab = customizationDatabase.glassPrefabs[glassesIndex];
            if (glassPrefab != null)
            {
                currentGlassesInstance = Instantiate(glassPrefab, hatsContainer);
                ResourceManager.TrackObject(currentGlassesInstance, $"Glasses_{GetInstanceID()}");
            }
        }

        // ---- Apply Body Material ----
        if (customizationDatabase != null && customizationDatabase.bodyMaterials != null &&
            customizationDatabase.bodyMaterials.Count > bodyIndex && bodyRenderer != null)
        {
            var bodyMaterial = customizationDatabase.bodyMaterials[bodyIndex];
            if (bodyMaterial != null)
            {
                bodyRenderer.material = bodyMaterial;
            }
        }

        // ---- Apply Head Material ----
        if (customizationDatabase != null && customizationDatabase.headMaterials != null &&
            customizationDatabase.headMaterials.Count > headIndex && headRenderer != null)
        {
            var headMaterial = customizationDatabase.headMaterials[headIndex];
            if (headMaterial != null)
            {
                headRenderer.material = headMaterial;
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
