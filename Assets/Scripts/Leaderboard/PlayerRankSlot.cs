using UnityEngine;
using TMPro;

/// <summary>
/// Represents a single row in the leaderboard UI.
/// This script is attached to the PlayerRankSlot prefab and handles displaying
/// player rank and name information in the leaderboard scroll view.
/// </summary>
public class PlayerRankSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI playerNameText;

    [Header("Rank Display Settings")]
    [SerializeField] private string rankFormat = "#{0}";  // Format for rank display (e.g., "#1", "#2")
    [SerializeField] private int maxNameLength = 20;      // Maximum character length for player names

    /// <summary>
    /// Updates the rank slot with the provided player data.
    /// This method is called by the leaderboard manager when populating the scroll view.
    /// </summary>
    /// <param name="rank">The player's rank position (1-based)</param>
    /// <param name="playerName">The player's display name</param>
    public void SetData(int rank, string playerName)
    {
        // Update rank text with formatting
        if (rankText != null)
        {
            rankText.text = string.Format(rankFormat, rank);
        }

        // Update player name text with length limiting
        if (playerNameText != null)
        {
            // Truncate name if it exceeds maximum length
            string displayName = playerName;
            if (displayName.Length > maxNameLength)
            {
                displayName = displayName.Substring(0, maxNameLength - 3) + "...";
            }
            
            playerNameText.text = displayName;
        }

        // Apply special styling for top positions
        ApplyRankStyling(rank);
    }

    /// <summary>
    /// Applies special visual styling based on the player's rank.
    /// Top 3 positions get special colors to highlight achievement.
    /// </summary>
    /// <param name="rank">The player's rank position</param>
    private void ApplyRankStyling(int rank)
    {
        Color rankColor = Color.white;
        Color nameColor = Color.white;

        // Apply special colors for top positions
        switch (rank)
        {
            case 1: // Gold for 1st place
                rankColor = new Color(1f, 0.84f, 0f);      // Gold
                nameColor = new Color(1f, 0.84f, 0f);
                break;
            case 2: // Silver for 2nd place
                rankColor = new Color(0.75f, 0.75f, 0.75f); // Silver
                nameColor = new Color(0.75f, 0.75f, 0.75f);
                break;
            case 3: // Bronze for 3rd place
                rankColor = new Color(0.8f, 0.5f, 0.2f);    // Bronze
                nameColor = new Color(0.8f, 0.5f, 0.2f);
                break;
            default: // Default white for other positions
                rankColor = Color.white;
                nameColor = Color.white;
                break;
        }

        // Apply the colors to the text components
        if (rankText != null)
        {
            rankText.color = rankColor;
        }

        if (playerNameText != null)
        {
            playerNameText.color = nameColor;
        }
    }

    /// <summary>
    /// Validates that all required UI components are assigned.
    /// Called automatically by Unity when the object is validated in the editor.
    /// </summary>
    private void OnValidate()
    {
        // Help developers catch missing references in the editor
        if (rankText == null)
        {
            Debug.LogWarning($"[PlayerRankSlot] rankText is not assigned on {gameObject.name}", this);
        }

        if (playerNameText == null)
        {
            Debug.LogWarning($"[PlayerRankSlot] playerNameText is not assigned on {gameObject.name}", this);
        }
    }

    /// <summary>
    /// Alternative method to set data with additional player information.
    /// Can be extended in the future for more complex leaderboard data.
    /// </summary>
    /// <param name="rank">The player's rank position</param>
    /// <param name="playerName">The player's display name</param>
    /// <param name="score">The player's score (optional, for future use)</param>
    public void SetData(int rank, string playerName, int score)
    {
        SetData(rank, playerName);
        // Future implementation: could display score or other stats
        // For now, just call the basic SetData method
    }
}