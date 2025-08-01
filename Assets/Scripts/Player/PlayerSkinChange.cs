using UnityEngine;
using Unity.Netcode;

public class PlayerSkinChange : NetworkBehaviour
{
    [SerializeField] private SOCustomizationDatabase customizationDatabase;

    private NetworkVariable<int> bodyMeshIndex = new NetworkVariable<int>(0);
    private NetworkVariable<int> headMeshIndex = new NetworkVariable<int>(0);
    private NetworkVariable<int> hatIndex = new NetworkVariable<int>(0);

    private void Start()
    {
        ApplyCustomization();
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            int body = PlayerPrefs.GetInt(GameConstants.PlayerPrefsKeys.BODY_INDEX, 0);
            int head = PlayerPrefs.GetInt(GameConstants.PlayerPrefsKeys.HEAD_INDEX, 0);
            int hat = PlayerPrefs.GetInt(GameConstants.PlayerPrefsKeys.GLASSES_INDEX, 0);

            body = Mathf.Clamp(body, 0, customizationDatabase.bodyMeshes.Count - 1);
            head = Mathf.Clamp(head, 0, customizationDatabase.headMeshes.Count - 1);
            hat = Mathf.Clamp(hat, 0, customizationDatabase.glassPrefabs.Count - 1);

            SetCustomizationServerRpc(body, head, hat);
        }

        ApplyCustomization();

        bodyMeshIndex.OnValueChanged += (_, _) => ApplyCustomization();
        headMeshIndex.OnValueChanged += (_, _) => ApplyCustomization();
        hatIndex.OnValueChanged += (_, _) => ApplyCustomization();
    }

    [ServerRpc]
    private void SetCustomizationServerRpc(int body, int head, int hat)
    {
        bodyMeshIndex.Value = body;
        headMeshIndex.Value = head;
        hatIndex.Value = hat;
    }

    private void ApplyCustomization()
    {
        // Body Mesh
        Transform bodyMeshTransform = transform.FindDeepChild("body-mesh");
        if (bodyMeshTransform != null && bodyMeshTransform.TryGetComponent(out SkinnedMeshRenderer bodyRenderer))
        {
            if (bodyMeshIndex.Value >= 0 && bodyMeshIndex.Value < customizationDatabase.bodyMeshes.Count)
            {
                bodyRenderer.sharedMesh = customizationDatabase.bodyMeshes[bodyMeshIndex.Value];
            }
        }

        // Head Mesh
        Transform headMeshTransform = transform.FindDeepChild("head-mesh");
        if (headMeshTransform != null && headMeshTransform.TryGetComponent(out SkinnedMeshRenderer headRenderer))
        {
            if (headMeshIndex.Value >= 0 && headMeshIndex.Value < customizationDatabase.headMeshes.Count)
            {
                headRenderer.sharedMesh = customizationDatabase.headMeshes[headMeshIndex.Value];
            }
        }

        // Hats
        Transform hatsTransform = transform.FindDeepChild("Hats");
        if (hatsTransform != null)
        {
            // Clear existing children
            foreach (Transform child in hatsTransform)
            {
                Destroy(child.gameObject);
            }

            if (hatIndex.Value >= 0 && hatIndex.Value < customizationDatabase.glassPrefabs.Count)
            {
                GameObject hatPrefab = customizationDatabase.glassPrefabs[hatIndex.Value];
                if (hatPrefab != null)
                {
                    GameObject hat = Instantiate(hatPrefab, hatsTransform);
                    hat.SetActive(true);
                }
            }
        }
    }
}
