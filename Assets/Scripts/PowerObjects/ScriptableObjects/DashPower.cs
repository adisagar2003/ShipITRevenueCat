using UnityEngine;

[CreateAssetMenu(menuName = "Player/Special Powers/Dash Power")]
public class DashPower : SpecialPower
{   
    public float dashForce = 500f;

    public override void ApplyEffect(GameObject player)
    {
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
        #if debug
            Debug.Log($"<color=#00FFFF><b>[DashPower]</b></color> <color=yellow>Applying dash force: {dashForce} to player {player.name}.</color>");
        #endif
            rb.AddForce(player.transform.forward * dashForce, ForceMode.VelocityChange);
        }
        #if debug
            else
            {
                Debug.Log("<color=#00FFFF><b>[DashPower]</b></color> <color=red>Rigidbody not found on player.</color>");
            }
        #endif
    }

    // Now also apply force on the client for immediate feedback
    public override void OnEffectAppliedClientRpc(GameObject player)
    {
        #if debug
            Debug.Log($"<color=#00FFFF><b>[DashPower]</b></color> <color=green>Dash effect applied on client for player {player.name}.</color>");
        #endif
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(player.transform.forward * dashForce, ForceMode.VelocityChange);
        }
    }
}
