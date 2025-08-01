using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject database containing all available character customization options.
/// This asset stores references to meshes, materials, and prefabs used for character appearance.
/// </summary>
[CreateAssetMenu(fileName = "CustomizationDatabase", menuName = "Game/Customization Database")]
public class SOCustomizationDatabase : ScriptableObject
{
    /// <summary>Prefabs for glasses/hat accessories that can be equipped on characters.</summary>
    public List<GameObject> glassPrefabs;
    
    /// <summary>Materials for character body textures and appearance variations.</summary>
    public List<Material> bodyMaterials;
    
    /// <summary>Materials for character head textures and appearance variations.</summary>
    public List<Material> headMaterials;
    
    /// <summary>3D meshes for different character body shapes and styles.</summary>
    public List<Mesh> bodyMeshes;
    
    /// <summary>3D meshes for different character head shapes and styles.</summary>
    public List<Mesh> headMeshes;
}
