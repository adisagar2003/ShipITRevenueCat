using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;

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

 
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownDuration = 3f; // 3 seconds
    private void OnEnable()
    {
        RaceLevelManager.OnAllPlayersReady += StartCountdown;
    }
    private void OnDisable()
    {
        RaceLevelManager.OnAllPlayersReady -= StartCountdown;
    }

    private void StartCountdown()
    {
        if (IsServer)
        {
            StartCoroutine(CountdownRoutine());
        }
    }

    private IEnumerator CountdownRoutine()
    {
        float currentTime = countdownDuration;

        while (currentTime > 0)
        {
            UpdateCountdownClientRpc(Mathf.CeilToInt(currentTime));
            yield return new WaitForSeconds(1f);
            currentTime -= 1f;
        }

        UpdateCountdownClientRpc(0);
        OnPlayerPossessionEvent?.Invoke();
        PossessPlayerClientRpc();
    }

    [ClientRpc]
    private void UpdateCountdownClientRpc(int time)
    {
        if (countdownText == null) return;

        countdownText.text = time > 0 ? time.ToString() : "GO!";
    }

    [ClientRpc]
    private void PossessPlayerClientRpc()
    {
        OnPlayerPossessionEvent?.Invoke();
    }
}
