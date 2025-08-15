using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Manages network operations for lobby including NetworkManager state management,
/// relay server operations, and host/client startup coordination.
/// Extracted from LobbyManager to provide focused network functionality.
/// </summary>
public class LobbyNetworkService : ThreadSafeSimpleSingleton<LobbyNetworkService>
{
    #region Events
    public event Action OnNetworkHostStarted;
    public event Action OnNetworkClientStarted;
    public event Action OnNetworkShutdown;
    public event Action<string> OnNetworkError;
    #endregion

    #region Properties
    public bool IsNetworkManagerClean => IsNetworkManagerInCleanState();
    public bool IsHost => NetworkManager.Singleton?.IsHost == true;
    public bool IsClient => NetworkManager.Singleton?.IsClient == true;
    public bool IsServer => NetworkManager.Singleton?.IsServer == true;
    #endregion

    #region NetworkManager State Management
    /// <summary>
    /// Checks if NetworkManager is in a clean state for starting new operations.
    /// </summary>
    private bool IsNetworkManagerInCleanState()
    {
        if (NetworkManager.Singleton == null) return false;

        bool isClean = !NetworkManager.Singleton.IsClient &&
                      !NetworkManager.Singleton.IsServer &&
                      !NetworkManager.Singleton.IsHost;

        GameLogger.LogDebug(GameLogger.LogCategory.Network, 
            $"NetworkManager clean state: IsClient={NetworkManager.Singleton.IsClient}, " +
            $"IsServer={NetworkManager.Singleton.IsServer}, IsHost={NetworkManager.Singleton.IsHost}, Clean={isClean}");

        return isClean;
    }

    /// <summary>
    /// Safely shuts down NetworkManager with proper cleanup and timeout handling.
    /// </summary>
    /// <param name="timeoutSeconds">Maximum time to wait for shutdown</param>
    /// <returns>True if shutdown was successful</returns>
    public async Task<bool> SafeShutdownNetworkManagerAsync(float timeoutSeconds = 5f)
    {
        if (NetworkManager.Singleton == null) return true;

        if (IsNetworkManagerInCleanState())
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Network, "NetworkManager already clean, no shutdown needed");
            return true;
        }

        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Shutting down NetworkManager...");

        try
        {
            NetworkManager.Singleton.Shutdown();

            // Wait for clean shutdown with timeout
            float elapsed = 0f;
            while (!IsNetworkManagerInCleanState() && elapsed < timeoutSeconds)
            {
                await Task.Delay(100);
                elapsed += 0.1f;
            }

            bool success = IsNetworkManagerInCleanState();

            if (success)
            {
                GameLogger.LogInfo(GameLogger.LogCategory.Network, "NetworkManager shutdown successful");
                OnNetworkShutdown?.Invoke();
            }
            else
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Network, $"NetworkManager shutdown timeout after {timeoutSeconds}s");
            }

            return success;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to shutdown NetworkManager: {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            return false;
        }
    }

    /// <summary>
    /// Prepares NetworkManager for hosting by ensuring clean state.
    /// </summary>
    /// <returns>True if NetworkManager is ready for hosting</returns>
    public async Task<bool> PrepareNetworkManagerForHostAsync()
    {
        if (NetworkManager.Singleton == null)
        {
            var errorMsg = "NetworkManager.Singleton is null";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            return false;
        }

        if (IsNetworkManagerInCleanState())
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Network, "NetworkManager already clean, ready for host");
            return true;
        }

        GameLogger.LogInfo(GameLogger.LogCategory.Network, "NetworkManager not clean, performing shutdown...");
        return await SafeShutdownNetworkManagerAsync();
    }
    #endregion

    #region Relay Operations
    /// <summary>
    /// Creates a relay allocation for hosting a game.
    /// </summary>
    /// <param name="maxConnections">Maximum number of connections (excluding host)</param>
    /// <returns>Allocation and join code tuple</returns>
    public async Task<(Allocation allocation, string joinCode)> CreateRelayAllocationAsync(int maxConnections)
    {
        try
        {
            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Creating relay allocation for {maxConnections} connections...");

            var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Relay allocation created successfully. Join code: {joinCode}");
            
            return (allocation, joinCode);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to create relay allocation: {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            throw;
        }
    }

    /// <summary>
    /// Joins a relay server using the provided join code.
    /// </summary>
    /// <param name="joinCode">Join code from the host</param>
    /// <returns>Join allocation for client connection</returns>
    public async Task<JoinAllocation> JoinRelayAsync(string joinCode)
    {
        try
        {
            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Joining relay with code: {joinCode}");
            
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            GameLogger.LogInfo(GameLogger.LogCategory.Network, "Successfully joined relay");
            return joinAllocation;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to join relay: {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            throw;
        }
    }
    #endregion

    #region Transport Configuration
    /// <summary>
    /// Configures Unity Transport for host relay connection.
    /// </summary>
    /// <param name="allocation">Relay allocation from CreateRelayAllocationAsync</param>
    public void ConfigureHostTransport(Allocation allocation)
    {
        try
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
            
            GameLogger.LogDebug(GameLogger.LogCategory.Network, "Host transport configured successfully");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to configure host transport: {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            throw;
        }
    }

    /// <summary>
    /// Configures Unity Transport for client relay connection.
    /// </summary>
    /// <param name="joinAllocation">Join allocation from JoinRelayAsync</param>
    public void ConfigureClientTransport(JoinAllocation joinAllocation)
    {
        try
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );
            
            GameLogger.LogDebug(GameLogger.LogCategory.Network, "Client transport configured successfully");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to configure client transport: {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            throw;
        }
    }
    #endregion

    #region Network Startup
    /// <summary>
    /// Starts NetworkManager as a host.
    /// </summary>
    /// <returns>True if host started successfully</returns>
    public bool StartNetworkHost()
    {
        try
        {
            if (!NetworkManager.Singleton.StartHost())
            {
                var errorMsg = "NetworkManager.StartHost() returned false";
                GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
                OnNetworkError?.Invoke(errorMsg);
                return false;
            }

            GameLogger.LogInfo(GameLogger.LogCategory.Network, "Network host started successfully");
            OnNetworkHostStarted?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Exception during host startup: {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            return false;
        }
    }

    /// <summary>
    /// Starts NetworkManager as a client.
    /// </summary>
    /// <returns>True if client started successfully</returns>
    public bool StartNetworkClient()
    {
        try
        {
            if (!NetworkManager.Singleton.StartClient())
            {
                var errorMsg = "NetworkManager.StartClient() returned false";
                GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
                OnNetworkError?.Invoke(errorMsg);
                return false;
            }

            GameLogger.LogInfo(GameLogger.LogCategory.Network, "Network client started successfully");
            OnNetworkClientStarted?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Exception during client startup: {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            return false;
        }
    }
    #endregion

    #region High-Level Operations
    /// <summary>
    /// Complete workflow to start hosting with relay.
    /// </summary>
    /// <param name="maxPlayers">Maximum number of players including host</param>
    /// <returns>Join code for other players</returns>
    public async Task<string> StartHostWithRelayAsync(int maxPlayers)
    {
        // Prepare NetworkManager
        if (!await PrepareNetworkManagerForHostAsync())
        {
            throw new Exception("Failed to prepare NetworkManager for hosting");
        }

        // Create relay allocation
        var (allocation, joinCode) = await CreateRelayAllocationAsync(maxPlayers - 1);

        // Configure transport
        ConfigureHostTransport(allocation);

        // Start host
        if (!StartNetworkHost())
        {
            throw new Exception("Failed to start network host");
        }

        return joinCode;
    }

    /// <summary>
    /// Complete workflow to join a game as client.
    /// </summary>
    /// <param name="joinCode">Join code from the host</param>
    public async Task JoinAsClientAsync(string joinCode)
    {
        // Join relay
        var joinAllocation = await JoinRelayAsync(joinCode);

        // Configure transport
        ConfigureClientTransport(joinAllocation);

        // Start client
        if (!StartNetworkClient())
        {
            throw new Exception("Failed to start network client");
        }
    }
    #endregion

    #region Diagnostics
    /// <summary>
    /// Logs comprehensive NetworkManager state for debugging.
    /// </summary>
    /// <param name="context">Context description for the log</param>
    public void LogNetworkManagerState(string context)
    {
        if (NetworkManager.Singleton == null)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, $"[NETWORK STATE - {context}] NetworkManager.Singleton is NULL!");
            return;
        }

        var nm = NetworkManager.Singleton;
        GameLogger.LogInfo(GameLogger.LogCategory.Network, 
            $"[NETWORK STATE - {context}] " +
            $"IsServer: {nm.IsServer}, " +
            $"IsClient: {nm.IsClient}, " +
            $"IsHost: {nm.IsHost}, " +
            $"IsConnectedClient: {nm.IsConnectedClient}, " +
            $"ConnectedClients.Count: {nm.ConnectedClients.Count}");
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// Cleanup method for service destruction.
    /// </summary>
    public void Cleanup()
    {
        _ = SafeShutdownNetworkManagerAsync(); // Fire and forget cleanup
    }
    #endregion
}