#define debug
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Special Powers/Super Jump Power")]
public class SuperJumpPower : SpecialPower
{
    [Header("Super Jump Settings - Easy to Tweak")]
    [SerializeField] private float jumpForce = 100f;          // How strong the jump is
    [SerializeField] private float jumpDuration = 1f;        // How long the jump lasts  
    [SerializeField] private float airControl = 0.5f;        // How much movement control in air
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem jumpParticles;   // Optional particle effect

    public override void ApplyEffect(GameObject player)
    {
        var playerPowerManager = player.GetComponent<PlayerPowerManager>();
        var networkController = player.GetComponent<NetworkThirdPersonController>();
        
        if (playerPowerManager != null && networkController != null)
        {
#if debug
            Debug.Log($"<color=#00FFAA><b>[SuperJumpPower]</b></color> <color=yellow>Starting smooth super jump for player {player.name}.</color>");
#endif
            // Call our simple method with server authority
            playerPowerManager.StartSmoothSuperJump(jumpForce, jumpDuration, airControl, jumpParticles);
        }
#if debug
        else
        {
            Debug.Log("<color=#00FFAA><b>[SuperJumpPower]</b></color> <color=red>Required components not found on player.</color>");
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
