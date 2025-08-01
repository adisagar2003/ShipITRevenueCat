using UnityEngine;

/// <summary>
/// Contains constant values used throughout the game to avoid magic numbers and hardcoded strings.
/// Organized into nested classes for better categorization and maintainability.
/// </summary>
public static class GameConstants
{
    /// <summary>
    /// PlayerPrefs keys used for character customization persistence.
    /// </summary>
    public static class PlayerPrefsKeys
    {
        /// <summary>Index of the selected body mesh in the customization database.</summary>
        public const string BODY_INDEX = "Body_Index";
        /// <summary>Index of the selected head mesh in the customization database.</summary>
        public const string HEAD_INDEX = "Head_Index";
        /// <summary>Index of the selected glasses/hat in the customization database.</summary>
        public const string GLASSES_INDEX = "Glasses_Index";
    }

    /// <summary>
    /// Constants related to player movement and physics.
    /// </summary>
    public static class Movement
    {
        /// <summary>Distance for ground detection raycast in Unity units.</summary>
        public const float GROUND_CHECK_RAY_DISTANCE = 1.3f;
        /// <summary>Minimum magnitude threshold for considering movement input significant.</summary>
        public const float MOVEMENT_DIRECTION_THRESHOLD = 0.1f;
    }

    /// <summary>
    /// Constants for multiplayer networking and lobby management.
    /// </summary>
    public static class Networking
    {
        /// <summary>Default maximum number of players in a multiplayer lobby.</summary>
        public const int DEFAULT_MAX_PLAYERS = 2;
        /// <summary>Interval in seconds for polling lobby updates.</summary>
        public const float LOBBY_POLLING_INTERVAL = 1f;
        /// <summary>Interval in seconds for checking if all players have connected.</summary>
        public const float PLAYER_WAIT_POLLING_INTERVAL = 0.5f;
        /// <summary>Maximum relay connections (maxPlayers - 1 for host).</summary>
        public const int RELAY_MAX_CONNECTIONS = 1;
    }

    /// <summary>
    /// Constants for graphics settings and scene names.
    /// </summary>
    public static class Graphics
    {
        /// <summary>Target frame rate for the application.</summary>
        public const int TARGET_FRAME_RATE = 120;
        /// <summary>Value to disable VSync (0 = disabled).</summary>
        public const int VSYNC_DISABLED = 0;
        /// <summary>Scene name for the main race level.</summary>
        public const string SCENE_RACE_LEVEL = "RaceLevel";
        /// <summary>Scene name for the character customization screen.</summary>
        public const string SCENE_CHARACTER_CUSTOMIZER = "CharacterCustomizer";
    }

    /// <summary>
    /// Constants for debug UI and logging configuration.
    /// </summary>
    public static class Debug
    {
        /// <summary>How long debug log messages remain visible in seconds.</summary>
        public const float LOG_LIFETIME_SECONDS = 4f;
        /// <summary>Maximum number of debug log lines to display at once.</summary>
        public const int MAX_DEBUG_LOG_LINES = 30;
        /// <summary>Font size for debug UI text.</summary>
        public const int DEBUG_FONT_SIZE = 16;
        /// <summary>Alpha transparency for debug UI background (0-1).</summary>
        public const float DEBUG_BACKGROUND_ALPHA = 0.7f;
    }
}