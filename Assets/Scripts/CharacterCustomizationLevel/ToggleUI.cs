using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Toggler : MonoBehaviour
{
    [SerializeField] private string togglerName;
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text headingText;

    private int currentIndex = 0;

    [SerializeField] private List<GameObject> gameObjectOptions;
    [SerializeField] private List<Material> materialOptions;
    [SerializeField] private SkinnedMeshRenderer targetRenderer;
    [SerializeField] private List<Mesh> meshOptions;
    [SerializeField] private List<Sprite> previewSprites;

    private enum ToggleType { GameObject, Material, Mesh }
    [SerializeField] private ToggleType toggleType;

    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    private void Awake()
    {
        if (leftButton != null)
            leftButton.onClick.AddListener(MoveLeftCircular);

        if (rightButton != null)
            rightButton.onClick.AddListener(MoveRightCircular);
    }

    public void MoveLeftCircular()
    {
        currentIndex = (currentIndex - 1 + GetOptionCount()) % GetOptionCount();
        ApplySelection();
    }

    public void MoveRightCircular()
    {
        currentIndex = (currentIndex + 1) % GetOptionCount();
        ApplySelection();
    }

    private int GetOptionCount()
    {
        switch (toggleType)
        {
            case ToggleType.GameObject:
                return gameObjectOptions.Count;
            case ToggleType.Material:
                return materialOptions.Count;
            case ToggleType.Mesh:
                return meshOptions.Count;
            default:
                return 0;
        }
    }

    private void ApplySelection()
    {
        switch (toggleType)
        {
            case ToggleType.GameObject:
                for (int i = 0; i < gameObjectOptions.Count; i++)
                    gameObjectOptions[i].SetActive(i == currentIndex);
                break;

            case ToggleType.Material:
                if (targetRenderer != null && materialOptions.Count > 0)
                    targetRenderer.material = materialOptions[currentIndex];
                break;

            case ToggleType.Mesh:
                if (targetRenderer != null && meshOptions.Count > 0)
                    targetRenderer.sharedMesh = meshOptions[currentIndex];
                break;
        }

        if (previewImage != null && previewSprites.Count > 0)
        {
            previewImage.sprite = previewSprites[currentIndex];
        }
    }


    private void Start()
    {
        ApplySelection();

        if (headingText != null && !string.IsNullOrEmpty(togglerName))
        {
            headingText.text = togglerName;
        }
    }


}