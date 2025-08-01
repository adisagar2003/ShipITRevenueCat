using UnityEngine;

/// <summary>
/// Extension methods for Unity Transform objects to provide additional utility functionality.
/// </summary>
public static class TransformExtensions
{
    /// <summary>
    /// Recursively searches for a child with the specified name anywhere under this Transform.
    /// This performs a depth-first search through the entire hierarchy.
    /// </summary>
    /// <param name="parent">The parent Transform to search from.</param>
    /// <param name="name">The exact name to search for (case-sensitive).</param>
    /// <returns>The Transform with the matching name, or null if not found.</returns>
    public static Transform FindDeepChild(this Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = child.FindDeepChild(name);
            if (result != null)
                return result;
        }
        return null;
    }
}
