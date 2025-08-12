#define debug
using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// START RACE COUNTDOWN - Server-Controlled 3-2-1-GO Countdown System
/// 
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// COUNTDOWN SYSTEM OVERVIEW:
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// 
/// PHASE 1: 🎯 EVENT SUBSCRIPTION
/// - Subscribes to RaceLevelManager.OnAllPlayersReady during OnNetworkSpawn
/// - Only server-side instance manages countdown logic for network authority
/// 
/// PHASE 2: 🚀 COUNTDOWN TRIGGER  
/// - RaceLevelManager fires OnAllPlayersReady after all players spawn
/// - Server validates state (IsSpawned && IsServer) before starting countdown
/// 
/// PHASE 3: ⏰ COUNTDOWN EXECUTION
/// - Server runs 3-second countdown loop (3 → 2 → 1 → GO!)
/// - Each number broadcasts to clients via UpdateCountdownRpc
/// - Clients update UI text display synchronously
/// 
/// PHASE 4: 🏁 RACE ACTIVATION
/// - Countdown ends → Server fires OnPlayerPossessionEvent (local)
/// - Server sends PossessPlayerRpc to all clients
/// - Clients receive RPC → fire OnPlayerPossessionEvent (local)
/// - Movement systems enable, race begins!
/// 
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// NETWORK ARCHITECTURE:
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// 
/// 🌐 SERVER AUTHORITY:
/// - Only server manages countdown timing and progression
/// - Server-side coroutine drives entire countdown sequence
/// - Client-side instances are passive UI updaters
/// 
/// 📡 RPC COMMUNICATION:
/// - UpdateCountdownRpc: Server → Clients (UI updates for countdown numbers)
/// - PossessPlayerRpc: Server → Clients (movement activation signal)
/// - Both use [Rpc(SendTo.NotServer)] for efficiency
/// 
/// 📊 EVENT SYSTEM:
/// - Static event OnPlayerPossessionEvent consumed by movement systems
/// - Event fired on both server and clients for synchronized activation
/// - Decoupled architecture allows multiple systems to respond
/// 
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// INTEGRATION & DEPENDENCIES:
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// 
/// 🔗 CONNECTS FROM:
/// - RaceLevelManager.OnAllPlayersReady → Triggers countdown start
/// 
/// 🔗 CONNECTS TO:  
/// - Movement systems listen to OnPlayerPossessionEvent
/// - Player control scripts enable input handling on event
/// - Any system needing race start notification
/// 
/// 📋 REQUIRED COMPONENTS:
/// - countdownText: TextMesh component for displaying countdown (3, 2, 1, GO!)
/// - Must be NetworkBehaviour attached to networked GameObject
/// 
/// ⚙️ CONFIGURATION:
/// - countdownDuration: Duration of countdown in seconds (default: 3.0f)
/// - Countdown displays ceil values (3.9s shows "4", 3.1s shows "3")
/// </summary>
public class StartRaceCountdown : NetworkBehaviour
{
    #region Events
    /// <summary>
    /// Event fired when countdown completes and players can start moving.
    /// Consumed by movement systems, input handlers, and other race components.
    /// Fired on both server and clients for synchronized race start.
    /// </summary>
    public delegate void PlayerPossessionEvent();
    public static event PlayerPossessionEvent OnPlayerPossessionEvent;
    #endregion

    #region Serialized Fields
    [Header("Countdown Configuration")]
    [SerializeField] private TextMesh countdownText;
    [SerializeField] private float countdownDuration = 3f; // Total countdown duration in seconds
    #endregion

    #region Network Lifecycle
    /// <summary>
    /// Subscribe to RaceLevelManager events when spawning on network.
    /// Both server and clients subscribe, but only server will execute countdown logic.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        RaceLevelManager.OnAllPlayersReady += StartCountdown;

#if debug
        Debug.Log($"<color=#4CAF50><b>[StartRaceCountdown]</b></color> <color=white>Network spawned - Server: {IsServer}, Client: {IsClient}</color>");
        Debug.Log($"<color=#4CAF50><b>[StartRaceCountdown]</b></color> <color=cyan>Subscribed to RaceLevelManager.OnAllPlayersReady event</color>");
#endif
    }

    /// <summary>
    /// Clean up event subscriptions when despawning from network.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        RaceLevelManager.OnAllPlayersReady -= StartCountdown;
        base.OnNetworkDespawn();

#if debug
        Debug.Log($"<color=#FF9800><b>[StartRaceCountdown]</b></color> <color=white>Network despawned - Unsubscribed from events</color>");
#endif
    }
    #endregion

    #region Countdown Management
    /// <summary>
    /// Event handler for RaceLevelManager.OnAllPlayersReady.
    /// Only server instances will execute the countdown logic.
    /// </summary>
    private void StartCountdown()
    {
#if debug
        Debug.Log($"<color=#2196F3><b>[StartRaceCountdown]</b></color> <color=yellow>🎯 OnAllPlayersReady event received! Starting countdown...</color>");
#endif

        // Validate server authority and network state
        if (!IsSpawned || !IsServer)
        {
#if debug
            Debug.LogWarning($"<color=#FF5722><b>[StartRaceCountdown]</b></color> <color=white>❌ Cannot start countdown - Spawned: {IsSpawned}, Server: {IsServer}</color>");
#endif
            return;
        }

#if debug
        Debug.Log($"<color=#2196F3><b>[StartRaceCountdown]</b></color> <color=cyan>✅ Server authority confirmed - Starting countdown routine (Duration: {countdownDuration}s)</color>");
#endif
        StartCoroutine(CountdownRoutine());
    }

    /// <summary>
    /// Server-only coroutine that manages the countdown sequence.
    /// Broadcasts countdown numbers to clients and triggers race start.
    /// </summary>
    private IEnumerator CountdownRoutine()
    {
        float currentTime = countdownDuration;
#if debug
        Debug.Log($"<color=#9C27B0><b>[COUNTDOWN SEQUENCE]</b></color> <color=white>⏰ Countdown routine started - Initial time: {currentTime}s</color>");
#endif

        // Countdown loop: 3 → 2 → 1
        while (currentTime > 0)
        {
            int displayTime = Mathf.CeilToInt(currentTime);
#if debug
            Debug.Log($"<color=#9C27B0><b>[COUNTDOWN SEQUENCE]</b></color> <color=yellow>📢 Broadcasting countdown number: {displayTime}</color>");
#endif

            UpdateCountdownRpc(displayTime);
            yield return new WaitForSeconds(1f);
            currentTime -= 1f;
        }

        // Final "GO!" signal
#if debug
        Debug.Log($"<color=#9C27B0><b>[COUNTDOWN SEQUENCE]</b></color> <color=lime>🏁 Countdown finished! Broadcasting GO signal...</color>");
#endif
        UpdateCountdownRpc(0); // 0 triggers "GO!" display

        // Activate race on server
#if debug
        Debug.Log($"<color=#9C27B0><b>[RACE ACTIVATION]</b></color> <color=orange>🚀 Invoking PlayerPossessionEvent for server...</color>");
#endif
        OnPlayerPossessionEvent?.Invoke();

        // Activate race on all clients
#if debug
        Debug.Log($"<color=#9C27B0><b>[RACE ACTIVATION]</b></color> <color=orange>📡 Sending PossessPlayerRpc to clients...</color>");
#endif
        PossessPlayerRpc();

#if debug
        Debug.Log($"<color=#9C27B0><b>[RACE ACTIVATION]</b></color> <color=lime>✅ Race activation sequence complete! Players can now move!</color>");
#endif
    }
    #endregion

    #region Network RPCs
    /// <summary>
    /// RPC sent from server to all clients to update countdown display.
    /// Updates UI text with countdown numbers (3, 2, 1) or "GO!" when time = 0.
    /// </summary>
    /// <param name="time">Countdown number to display (0 = "GO!")</param>
    [Rpc(SendTo.NotServer)]
    private void UpdateCountdownRpc(int time)
    {
        // Validate UI component exists
        if (countdownText == null)
        {
#if debug
            Debug.LogWarning($"<color=#E91E63><b>[COUNTDOWN UI ERROR]</b></color> <color=white>CountdownText is null! Cannot update UI.</color>");
#endif
            return;
        }

        // Update display text
        string displayText = time > 0 ? time.ToString() : "GO!";
        countdownText.text = displayText;

#if debug
        Debug.Log($"<color=#E91E63><b>[COUNTDOWN UI]</b></color> <color=cyan>📱 Client UI updated: '{displayText}'</color>");
#endif
    }

    /// <summary>
    /// RPC sent from server to all clients to activate player movement.
    /// Triggers OnPlayerPossessionEvent on client side for race start.
    /// </summary>
    [Rpc(SendTo.NotServer)]
    private void PossessPlayerRpc()
    {
#if debug
        Debug.Log($"<color=#00BCD4><b>[PLAYER ACTIVATION]</b></color> <color=lime>📡 Client received PossessPlayerRpc - Enabling player movement!</color>");
#endif
        OnPlayerPossessionEvent?.Invoke();
    }
    #endregion
}
