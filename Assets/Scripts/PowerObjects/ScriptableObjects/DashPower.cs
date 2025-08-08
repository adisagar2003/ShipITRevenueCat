using UnityEngine;

[CreateAssetMenu(menuName = "Player/Special Powers/Dash Power")]
public class DashPower : SpecialPower
{
    [Header("Dash Settings - Easy to Tweak")]
    [SerializeField] private float dashSpeed = 15f;           // How fast the dash goes
    [SerializeField] private float accelerationTime = 0.3f;   // Time to reach full speed
    [SerializeField] private float dashDuration = 1.0f;       // Total dash time
    [SerializeField] private float decelerationTime = 0.5f;   // Time to slow down

    public override void ApplyEffect(GameObject player)
    {
        var playerPowerManager = player.GetComponent<PlayerPowerManager>();
        var networkController = player.GetComponent<NetworkThirdPersonController>();
        
        if (playerPowerManager != null && networkController != null)
        {
        #if debug
            Debug.Log($"<color=#00FFFF><b>[DashPower]</b></color> <color=yellow>Starting smooth dash for player {player.name}.</color>");
        #endif
            // Just call our simple method with the 4 parameters
            playerPowerManager.StartSmoothDash(dashSpeed, accelerationTime, dashDuration, decelerationTime);
        }
        #if debug
            else
            {
                Debug.Log("<color=#00FFFF><b>[DashPower]</b></color> <color=red>Required components not found on player.</color>");
            }
        #endif
    }

    public void OnEffectAppliedClientRpc(GameObject player)
    {
        #if debug
            Debug.Log($"<color=#00FFFF><b>[DashPower]</b></color> <color=green>Dash effect applied on client for player {player.name}.</color>");
        #endif
    }
}
