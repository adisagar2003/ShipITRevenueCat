using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Enables Countdown UI, server-controlled, UI counts from 3 to 1, then all players
/// can start moving.
/// Requires RaceLevelManager script.
/// Invokes event after countdown ends.
/// Takes a [SerializeField] TMPro TextMeshProUGUI for displaying the countdown.
/// </summary>
public class StartRaceCountdown : NetworkBehaviour
{
    public delegate void PlayerPossessionEvent();
    public static event PlayerPossessionEvent OnPlayerPossessionEvent;


    [SerializeField] private TextMesh countdownText;
    [SerializeField] private float countdownDuration = 3f; // 3 seconds
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        RaceLevelManager.OnAllPlayersReady += StartCountdown;
    }

    public override void OnNetworkDespawn()
    {
        RaceLevelManager.OnAllPlayersReady -= StartCountdown;
        base.OnNetworkDespawn();
    }

    private void StartCountdown()
    {
        if (!IsSpawned || !IsServer) return;
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        float currentTime = countdownDuration;

        while (currentTime > 0)
        {
            UpdateCountdownRpc(Mathf.CeilToInt(currentTime));
            yield return new WaitForSeconds(1f);
            currentTime -= 1f;
        }

        UpdateCountdownRpc(0);
        OnPlayerPossessionEvent?.Invoke();
        PossessPlayerRpc();
    }

    [Rpc(SendTo.NotServer)]
    private void UpdateCountdownRpc(int time)
    {
        if (countdownText == null) return;

        countdownText.text = time > 0 ? time.ToString() : "GO!";
    }

    [Rpc(SendTo.NotServer)]
    private void PossessPlayerRpc()
    {
        OnPlayerPossessionEvent?.Invoke();
    }
}
