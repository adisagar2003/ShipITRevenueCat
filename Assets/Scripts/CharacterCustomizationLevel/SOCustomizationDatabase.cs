using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomizationDatabase", menuName = "Game/Customization Database")]
public class SOCustomizationDatabase : ScriptableObject
{
    public List<GameObject> glassPrefabs;
    public List<Material> bodyMaterials;
    public List<Material> headMaterials;
    public List<Mesh> bodyMeshes;
    public List<Mesh> headMeshes;
}
