using UnityEngine;

public static class TransformExtensions
{
    /// <summary>
    /// Recursively searches for a child with the specified name anywhere under this Transform.
    /// Returns the Transform if found, otherwise null.
    /// </summary>
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
