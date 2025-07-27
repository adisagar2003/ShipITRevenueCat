using UnityEngine;
using Unity.Netcode;
using UnityEditor;

public enum ActivationType
{
    Passive,
    Active
}

// Removed [MenuItem("Player/Special Powers/")] from the class declaration.
// The MenuItem attribute is only valid on methods, not on classes or ScriptableObjects.
[CreateAssetMenu(menuName = "Player/Special Powers/")]
public abstract class SpecialPower : ScriptableObject
{
    [Header("Power Settings")]
    public ActivationType activationType;

    /// <summary>
    /// Called on the server to apply the power's effect to the player.
    /// </summary>
    /// <param name="player">The player GameObject to apply the effect to.</param>
    public abstract void ApplyEffect(GameObject player);

    /// <summary>
    /// Called on all clients to trigger visual/audio feedback.
    /// </summary>
    /// <param name="player">The player GameObject for feedback.</param>
    [ClientRpc]
    public virtual void OnEffectAppliedClientRpc(GameObject player)
    {
        // Implement visual/audio feedback in derived classes.
    }
}
