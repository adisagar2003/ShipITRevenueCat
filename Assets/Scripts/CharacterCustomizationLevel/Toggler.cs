using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Toggler : MonoBehaviour
{
    public enum TogglerTypeName
    {
        Glasses,
        BodyMaterial,
        HeadMaterial,
        BodyMesh,
        Head
    }

    [Header("Basic Info")]
    [SerializeField] private TogglerTypeName togglerName;
    [SerializeField] private Image previewImage; // later implementation
    [SerializeField] private TMP_Text headingText;
    [SerializeField] private GameObject glassGameObjectContainer;

    [Header("Database")]
    [SerializeField] private SOCustomizationDatabase customizationDatabase;

    private enum ToggleType { Glasses, BodyMaterial, HeadMaterial, BodyMesh, Head }
    [SerializeField] private ToggleType toggleType;

    [Header("Target Renderer")]
    [SerializeField] private SkinnedMeshRenderer targetRenderer;

    [Header("Navigation")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("For Glasses")]
    private List<GameObject> sceneGlassesInstances = new List<GameObject>();
    private Material originalMaterial; // Store original material for cleanup
    private Mesh originalMesh; // Store original mesh for cleanup

    private int currentIndex = 0;
    private void Awake()
    {
        if (leftButton != null)
            leftButton.onClick.AddListener(MoveLeftCircular);

        if (rightButton != null)
            rightButton.onClick.AddListener(MoveRightCircular);
            
        // Store original material and mesh for cleanup
        if (targetRenderer != null)
        {
            originalMaterial = targetRenderer.material;
            originalMesh = targetRenderer.sharedMesh;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event listeners
        if (leftButton != null)
            leftButton.onClick.RemoveListener(MoveLeftCircular);

        if (rightButton != null)
            rightButton.onClick.RemoveListener(MoveRightCircular);
            
        // Reset to original material and mesh to prevent leaks
        if (targetRenderer != null)
        {
            if (originalMaterial != null)
                targetRenderer.material = originalMaterial;
            if (originalMesh != null)
                targetRenderer.sharedMesh = originalMesh;
        }
        
        // Clear the glasses instances list
        sceneGlassesInstances?.Clear();
        
        GameLogger.LogDebug(GameLogger.LogCategory.UI, "Toggler cleaned up");
    }

    private void Start()
    {

        if (toggleType == ToggleType.Glasses)
        {
            CacheGlassesInstancesInScene();
        }

        string prefsKey = $"{togglerName}_Index";
        int savedIndex = PlayerPrefs.GetInt(prefsKey, 0);
        if (savedIndex >= 0 && savedIndex < GetOptionCount())
        {
            currentIndex = savedIndex;
        }

        ApplySelection();

        if (headingText != null)
        {
            headingText.text = togglerName.ToString();
        }
    }

    private void CacheGlassesInstancesInScene()
    {
        if (customizationDatabase == null || customizationDatabase.glassPrefabs == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Customization database or glass prefabs is null");
            return;
        }
        
        if (glassGameObjectContainer == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Glass game object container is null");
            return;
        }
        
        GameLogger.LogDebug(GameLogger.LogCategory.UI, "Caching glasses objects");
        
        // Clear existing instances to prevent duplicates
        sceneGlassesInstances.Clear();
        
        foreach (var prefab in customizationDatabase.glassPrefabs)
        {
            if (prefab == null) continue;
            
            string targetName = prefab.name;
            Transform found = glassGameObjectContainer.transform.Find(targetName);
            if (found != null)
            {
                GameLogger.LogDebug(GameLogger.LogCategory.UI, $"Found cached glasses item: {found.name}");
                sceneGlassesInstances.Add(found.gameObject);
                found.gameObject.SetActive(false); // Ensure all are disabled initially
            }
            else
            {
                GameLogger.LogWarning(GameLogger.LogCategory.UI, $"Glasses object named '{targetName}' not found in hierarchy under {transform.name}.");
            }
        }
        
        GameLogger.LogInfo(GameLogger.LogCategory.UI, $"Cached {sceneGlassesInstances.Count} glasses instances");
    }

    public void MoveLeftCircular()
    {
        currentIndex = (currentIndex - 1 + GetOptionCount()) % GetOptionCount();
        ApplySelection();
        SaveSelection();
    }

    public void MoveRightCircular()
    {
        currentIndex = (currentIndex + 1) % GetOptionCount();
        ApplySelection();
        SaveSelection();
    }

    private int GetOptionCount()
    {
        if (customizationDatabase == null) return 0;
        
        return toggleType switch
        {
            ToggleType.Glasses => customizationDatabase.glassPrefabs?.Count ?? 0,
            ToggleType.BodyMaterial => customizationDatabase.bodyMaterials?.Count ?? 0,
            ToggleType.HeadMaterial => customizationDatabase.headMaterials?.Count ?? 0,
            ToggleType.BodyMesh => customizationDatabase.bodyMeshes?.Count ?? 0,
            ToggleType.Head => customizationDatabase.headMeshes?.Count ?? 0,
            _ => 0
        };
    }

    private void ApplySelection()
    {
        if (customizationDatabase == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Cannot apply selection: customization database is null");
            return;
        }
        
        switch (toggleType)
        {
            case ToggleType.Glasses:
                ApplyGlassesSelection();
                break;
            case ToggleType.BodyMaterial:
                ApplyMaterialSelection(customizationDatabase.bodyMaterials);
                break;
            case ToggleType.HeadMaterial:
                ApplyMaterialSelection(customizationDatabase.headMaterials);
                break;
            case ToggleType.BodyMesh:
                ApplyMeshSelection(customizationDatabase.bodyMeshes);
                break;
            case ToggleType.Head:
                ApplyMeshSelection(customizationDatabase.headMeshes);
                break;
        }

        if (previewImage != null)
        {
            // Optionally handle preview sprite updates if added to your SO
        }
    }
    
    private void ApplyGlassesSelection()
    {
        if (sceneGlassesInstances == null || sceneGlassesInstances.Count == 0)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "No glasses instances cached");
            return;
        }
        
        for (int i = 0; i < sceneGlassesInstances.Count; i++)
        {
            if (sceneGlassesInstances[i] != null)
            {
                sceneGlassesInstances[i].SetActive(i == currentIndex);
            }
        }
    }
    
    private void ApplyMaterialSelection(List<Material> materials)
    {
        if (targetRenderer == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Target renderer is null");
            return;
        }
        
        if (materials == null || materials.Count == 0 || currentIndex >= materials.Count)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Invalid material selection");
            return;
        }
        
        var selectedMaterial = materials[currentIndex];
        if (selectedMaterial != null)
        {
            targetRenderer.material = selectedMaterial;
        }
    }
    
    private void ApplyMeshSelection(List<Mesh> meshes)
    {
        if (targetRenderer == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Target renderer is null");
            return;
        }
        
        if (meshes == null || meshes.Count == 0 || currentIndex >= meshes.Count)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Invalid mesh selection");
            return;
        }
        
        var selectedMesh = meshes[currentIndex];
        if (selectedMesh != null)
        {
            targetRenderer.sharedMesh = selectedMesh;
        }
    }

    private void SaveSelection()
    {
        string prefsKey = $"{togglerName}_Index";
        PlayerPrefs.SetInt(prefsKey, currentIndex);
        PlayerPrefs.Save();
    }
}
