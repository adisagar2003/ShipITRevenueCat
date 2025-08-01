using UnityEngine;

/// <summary>
/// Contains constant values used throughout the game to avoid magic numbers and hardcoded strings.
/// </summary>
public static class GameConstants
{
    // PlayerPrefs Keys
    public static class PlayerPrefsKeys
    {
        public const string BODY_INDEX = "Body_Index";
        public const string HEAD_INDEX = "Head_Index";
        public const string GLASSES_INDEX = "Glasses_Index";
    }

    // Movement Constants
    public static class Movement
    {
        public const float GROUND_CHECK_RAY_DISTANCE = 1.3f;
        public const float MOVEMENT_DIRECTION_THRESHOLD = 0.1f;
    }

    // Networking Constants
    public static class Networking
    {
        public const int DEFAULT_MAX_PLAYERS = 2;
        public const float LOBBY_POLLING_INTERVAL = 1f;
        public const float PLAYER_WAIT_POLLING_INTERVAL = 0.5f;
        public const int RELAY_MAX_CONNECTIONS = 1; // maxPlayers - 1
    }

    // Graphics Constants
    public static class Graphics
    {
        public const int TARGET_FRAME_RATE = 120;
        public const int VSYNC_DISABLED = 0;
        public const string SCENE_RACE_LEVEL = "RaceLevel";
        public const string SCENE_CHARACTER_CUSTOMIZER = "CharacterCustomizer";
    }

    // Debug Constants
    public static class Debug
    {
        public const float LOG_LIFETIME_SECONDS = 4f;
        public const int MAX_DEBUG_LOG_LINES = 30;
        public const int DEBUG_FONT_SIZE = 16;
        public const float DEBUG_BACKGROUND_ALPHA = 0.7f;
    }
}