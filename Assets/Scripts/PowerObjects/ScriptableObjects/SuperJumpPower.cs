#define debug
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Special Powers/Super Jump Power")]
public class SuperJumpPower : SpecialPower
{
    [Header("Super Jump Settings")]
    [SerializeField] private float jumpForce = 2000f;        // How strong the instant jump is
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem jumpParticles;   // Optional particle effect

    public override void ApplyEffect(GameObject player)
    {
        var playerPowerManager = player.GetComponent<PlayerPowerManager>();
        
        if (playerPowerManager != null)
        {
#if debug
            Debug.Log($"<color=#00FFAA><b>[SuperJumpPower]</b></color> <color=yellow>Starting quick super jump for player {player.name}.</color>");
#endif
            // Call the simplified instant super jump method
            playerPowerManager.StartQuickSuperJump(jumpForce, jumpParticles);
        }
#if debug
        else
        {
            Debug.Log("<color=#00FFAA><b>[SuperJumpPower]</b></color> <color=red>PlayerPowerManager component not found on player.</color>");
        }
#endif
    }

    public void OnEffectAppliedClientRpc(GameObject player)
    {
#if debug
        Debug.Log($"<color=#00FFAA><b>[SuperJumpPower]</b></color> <color=green>Super jump effect applied on client for player {player.name}.</color>");
#endif
        // No client-side physics - server authoritative only!
    }
}
