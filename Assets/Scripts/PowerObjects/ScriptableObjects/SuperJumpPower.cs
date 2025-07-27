#define debug
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Player/Special Powers/Super Jump Power")]
public class SuperJumpPower : SpecialPower
{
    public float jumpForce = 100f;
    public float jumpDuration = 1f; // Duration for which the jump force is applied

    public override void ApplyEffect(GameObject player)
    {
        var playerManager = player.GetComponent<PlayerPowerManager>();
        if (playerManager != null)
        {
#if debug
            Debug.Log($"<color=#00FFAA><b>[SuperJumpPower]</b></color> <color=yellow>Triggering gradual super jump: {jumpForce} for {jumpDuration}s on {player.name}.</color>");
#endif
            playerManager.StartSuperJump(jumpForce, jumpDuration);
        }
#if debug
        else
        {
            Debug.Log("<color=#00FFAA><b>[SuperJumpPower]</b></color> <color=red>PlayerPowerManager not found on player.</color>");
        }
#endif
    }

    public override void OnEffectAppliedClientRpc(GameObject player)
    {
#if debug
        Debug.Log($"<color=#00FFAA><b>[SuperJumpPower]</b></color> <color=green>Super jump effect applied on client for player {player.name}.</color>");
#endif
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }
}
