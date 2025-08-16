using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;

/// <summary>
/// 
/// </summary>
public class StartRaceCountdown : NetworkBehaviour
{
        public delegate void PlayerPossessionEvent();
        public static event PlayerPossessionEvent OnPlayerPossessionEvent;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private float countdownDuration = 3f;

        private void Start()
        {
                // if (!IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                // {
                //         NetworkObject networkObject = GetComponent<NetworkObject>();
                //         if (networkObject != null && IsServer)
                //         {
                //                 networkObject.Spawn();
                //         }
                // }
        }

        public void StartCountdown()
        {
                if (!NetworkManager.Singleton.IsHost) return;

                StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
                float currentTime = countdownDuration;

                while (currentTime > 0)
                {
                        int displayTime = Mathf.CeilToInt(currentTime);
                        UpdateCountdownRpc(displayTime);
                        yield return new WaitForSeconds(1f);
                        currentTime -= 1f;
                }

                UpdateCountdownRpc(0); // "GO!"
                OnPlayerPossessionEvent?.Invoke();
                PossessPlayerRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void UpdateCountdownRpc(int time)
        {
                if (countdownText == null) return;

                string displayText = time > 0 ? time.ToString() : "GO!";
                countdownText.text = displayText;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PossessPlayerRpc()
        {
                OnPlayerPossessionEvent?.Invoke();
        }
}
