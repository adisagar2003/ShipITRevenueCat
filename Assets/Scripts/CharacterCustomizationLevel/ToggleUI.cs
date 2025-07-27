using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The Toggler class is a comprehensive UI component for character customization,
/// allowing users to cycle through different appearance options for a 3D character model.
/// </summary>

public class Toggler : MonoBehaviour
{
    public enum TogglerTypeName
    {
        Glasses,
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


    private int currentIndex = 0;
    private void Awake()
    {
        if (leftButton != null)
            leftButton.onClick.AddListener(MoveLeftCircular);

        if (rightButton != null)
            rightButton.onClick.AddListener(MoveRightCircular);
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
        Debug.Log("Caching objeccts");
        foreach (var prefab in customizationDatabase.glassPrefabs)
        {
            
            string targetName = prefab.name;
            Transform found = glassGameObjectContainer.transform.Find(targetName);
            if (found != null)
            {
                Debug.Log("Now getting the cached item here" + found.ToString());
                sceneGlassesInstances.Add(found.gameObject);
                found.gameObject.SetActive(false); // Ensure all are disabled initially
            }
            else
            {
                Debug.LogWarning($"Glasses object named '{targetName}' not found in hierarchy under {transform.name}.");
            }
        }
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
        return toggleType switch
        {
            ToggleType.Glasses => customizationDatabase.glassPrefabs.Count,
            ToggleType.BodyMesh => customizationDatabase.bodyMeshes.Count,
            ToggleType.Head => customizationDatabase.headMeshes.Count,
            _ => 0
        };
    }

    private void ApplySelection()
    {
        switch (toggleType)
        {
            case ToggleType.Glasses:
                for (int i = 0; i < sceneGlassesInstances.Count; i++)
                    sceneGlassesInstances[i].SetActive(i == currentIndex);
                break;
            case ToggleType.BodyMesh:
                if (targetRenderer != null && customizationDatabase.bodyMeshes.Count > 0)
                    targetRenderer.sharedMesh = customizationDatabase.bodyMeshes[currentIndex];
                break;
            case ToggleType.Head:
                if (targetRenderer != null && customizationDatabase.headMeshes.Count > 0)
                    targetRenderer.sharedMesh = customizationDatabase.headMeshes[currentIndex];
                break;
        }

        if (previewImage != null)
        {
            // Optionally handle preview sprite updates if added to your SO
        }
    }

    private void SaveSelection()
    {
        string prefsKey = $"{togglerName}_Index";
        PlayerPrefs.SetInt(prefsKey, currentIndex);
        PlayerPrefs.Save();
    }
}
