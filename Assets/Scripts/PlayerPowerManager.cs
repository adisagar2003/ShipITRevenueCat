using UnityEngine;
using Unity.Netcode;

// Assumes SpecialPower.cs defines: 
// - enum ActivationType { Passive, Active }
// - class SpecialPower : ScriptableObject { ActivationType activationType; void ApplyEffect(GameObject player); void OnEffectAppliedClientRpc(GameObject player); }

public class PlayerPowerManager : NetworkBehaviour
{
    [SerializeField] private SpecialPower[] availablePowers;
    private NetworkVariable<int> currentPowerIndex = new NetworkVariable<int>(0);
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
#if debug
        Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=cyan>Awake called for {gameObject.name}.</color>");
#endif
    }

    private void Start()
    {
#if debug
        Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=cyan>Start called for {gameObject.name}.</color>");
#endif
    }

    private void Update()
    {
        if (!IsServer) return;

        SpecialPower currentPower = GetCurrentPower();
        if (currentPower == null) return;

        if (currentPower.activationType == ActivationType.Active && Input.GetKeyDown(KeyCode.Space))
        {
#if debug
            Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=yellow>Activating current active power for {gameObject.name}.</color>");
#endif
            ActivateCurrentPowerServerRpc(OwnerClientId);
        }
    }

    private SpecialPower GetCurrentPower()
    {
        if (availablePowers == null || availablePowers.Length == 0) return null;
        int idx = Mathf.Clamp(currentPowerIndex.Value, 0, availablePowers.Length - 1);
        if (idx < 0 || idx >= availablePowers.Length) return null;
        return availablePowers[idx];
    }

    public void OnServerPowerObjectCollision(SpecialPower power)
    {
        if (!IsServer) return;
        if (power.activationType == ActivationType.Passive)
        {
        #if debug
            Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=yellow>Passive power collision detected. Setting and activating power for {gameObject.name}.</color>");
        #endif
            SetCurrentPower(power);
            ActivateCurrentPowerServerRpc(OwnerClientId);
        }
    }

    // Sets the current power and syncs index
    private void SetCurrentPower(SpecialPower power)
    {
        int idx = System.Array.IndexOf(availablePowers, power);
        if (idx >= 0)
        {
        #if debug
            Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=yellow>Setting current power index to {idx} for {gameObject.name}.</color>");
        #endif
            currentPowerIndex.Value = idx;
        }
        #if debug
            else
            {
                Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=red>Power not found in availablePowers for {gameObject.name}.</color>");
            }
        #endif
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateCurrentPowerServerRpc(ulong playerClientId)
    {
        if (!IsServer) return;

        SpecialPower currentPower = GetCurrentPower();
        if (currentPower == null) return;

#if debug
        Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=green>Applying effect for current power on {gameObject.name}.</color>");
#endif
        currentPower.ApplyEffect(gameObject);
        currentPower.OnEffectAppliedClientRpc(gameObject);
    }

    /// <summary>
    /// ServerRpc to deactivate the current power.
    /// Resets the currentPowerIndex so no power is active.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void DeactivateCurrentPowerServerRpc()
    {
        if (!IsServer) return;

#if debug
        Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=magenta>Deactivating current power for {gameObject.name}.</color>");
#endif
        currentPowerIndex.Value = -1;
    }

    [ClientRpc]
    public void ActivateDashPowerClientRpc(ulong targetClientId)
    {
        // Only run on the client that owns this player
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

#if debug
        Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=cyan>DashPower activated on client {targetClientId} for {gameObject.name}.</color>");
#endif
        SpecialPower currentPower = GetCurrentPower();

        currentPower.ApplyEffect(gameObject);
    }
}
