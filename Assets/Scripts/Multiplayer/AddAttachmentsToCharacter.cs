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

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        ApplyCustomization();
    }

    private void ApplyCustomization()
    {
        // Retrieve saved indices   
        int bodyIndex = PlayerPrefs.GetInt("Body_Index", 0);
        int headIndex = PlayerPrefs.GetInt("Head_Index", 0);
        int glassesIndex = PlayerPrefs.GetInt("Glasses_Index", 0);

  
        // ---- Apply Glasses ----
        if (customizationDatabase.glassPrefabs.Count > glassesIndex)
        {
            if (currentGlassesInstance != null) Destroy(currentGlassesInstance);
            currentGlassesInstance = Instantiate(customizationDatabase.glassPrefabs[glassesIndex], hatsContainer);
        }

        // ---- Apply Body Material ----
        if (customizationDatabase.bodyMaterials.Count > bodyIndex && bodyRenderer != null)
        {
            bodyRenderer.material = customizationDatabase.bodyMaterials[bodyIndex];
        }

        // ---- Apply Head Material ----
        if (customizationDatabase.headMaterials.Count > headIndex && headRenderer != null)
        {
            headRenderer.material = customizationDatabase.headMaterials[headIndex];
        }
    }
}
