using Godot;

/// <summary>
/// Centralized game logic constants to avoid Magic Numbers scattered throughout the codebase.
/// All magic numbers should be defined here for easy adjustment and balancing.
/// </summary>
public static class GameConstants
{
    // ==================== Player Configuration ====================
    /// <summary>Player movement speed in pixels per second.</summary>
    public const float PLAYER_DEFAULT_MOVE_SPEED = 240f;

    /// <summary>Conversion factor from speed stat points to pixels per second.</summary>
    public const float PLAYER_SPEED_TO_PIXELS_MULTIPLIER = 50f;

    /// <summary>Default player body dimensions in tiles.</summary>
    public static readonly Vector2 PLAYER_BODY_SIZE_IN_TILES = new Vector2(1.0f, 1.9f);

    /// <summary>Default player collision body scale relative to sprite size.</summary>
    public static readonly Vector2 PLAYER_COLLISION_SCALE = new Vector2(0.88f, 0.94f);

    /// <summary>Default camera zoom level for the player.</summary>
    public static readonly Vector2 PLAYER_CAMERA_DEFAULT_ZOOM = new Vector2(0.7f, 0.7f);

    // ==================== Player Animation ====================
    /// <summary>Frames per second for walk animation.</summary>
    public const float PLAYER_WALK_ANIMATION_FPS = 3f;

    /// <summary>Frames per second for attack animation.</summary>
    public const float PLAYER_ATTACK_ANIMATION_FPS = 5f;

    /// <summary>Frames per second for idle/stand animation.</summary>
    public const float PLAYER_STAND_ANIMATION_FPS = 2f;

    // ==================== Combat ====================
    /// <summary>Basic attack skill range in pixels.</summary>
    public const float BASIC_ATTACK_RANGE = 60f;

    /// <summary>Basic attack cooldown duration in seconds.</summary>
    public const float BASIC_ATTACK_COOLDOWN = 0.3f;

    /// <summary>Minimum damage variance (-10%).</summary>
    public const float DAMAGE_VARIANCE_MIN = 0.9f;

    /// <summary>Maximum damage variance (+10%).</summary>
    public const float DAMAGE_VARIANCE_MAX = 1.1f;

    /// <summary>Defense stat damage reduction multiplier.</summary>
    public const float DEFENSE_DAMAGE_REDUCTION_FACTOR = 0.5f;

    // ==================== Status Effects ====================
    /// <summary>Attack reduction for Burn status.</summary>
    public const float STATUS_BURN_ATK_RATE = 0.5f;

    /// <summary>Defense reduction for Poison status.</summary>
    public const float STATUS_POISON_DEF_RATE = 0.5f;

    /// <summary>Speed reduction for Paralysis status.</summary>
    public const float STATUS_PARALYSIS_SPD_RATE = 0.5f;

    /// <summary>Stat multiplier for crowd control effects (Freeze, Sleep, Stun).</summary>
    public const float STATUS_CC_STAT_MULTIPLIER = 0f;

    // ==================== Portal & Teleport ====================
    /// <summary>Cooldown duration (in seconds) after portal activation to prevent consecutive triggers.</summary>
    public const float PORTAL_TELEPORT_COOLDOWN = 0.5f;

    /// <summary>Interference cooldown (in seconds) applied when interacting with objects (e.g., opening boxes) to prevent accidental portal activation.</summary>
    public const float INTERACTION_PORTAL_INTERFERENCE = 0.05f;

    /// <summary>Offset distance (in tiles) to nudge the player upon portal arrival, preventing immediate re-triggering.</summary>
    public const float PORTAL_ARRIVAL_NUDGE_TILES = 0.24f;

    // ==================== Interaction ====================
    /// <summary>Maximum detection distance (in tiles) for box interaction.</summary>
    public const float BOX_INTERACTION_MAX_DISTANCE_TILES = 1.15f;

    /// <summary>Maximum pickup distance (in pixels) required to collect dropped items.</summary>
    public const float PICKUP_INTERACTION_MAX_DISTANCE_PIXELS = 72f;

    // ==================== Lobby Configuration ====================
    /// <summary>Tile size for lobby map.</summary>
    public const int LOBBY_TILE_SIZE = 24;

    /// <summary>Room tile size for lobby map.</summary>
    public const int LOBBY_ROOM_TILE_SIZE = 100;

    /// <summary>Number of rooms horizontally in lobby.</summary>
    public const int LOBBY_ROOMS_X = 3;

    /// <summary>Number of rooms vertically in lobby.</summary>
    public const int LOBBY_ROOMS_Y = 3;

    /// <summary>Obstacle fill rate for lobby (0.0-1.0, lower for less congestion).</summary>
    public const float LOBBY_OBSTACLE_FILL_RATE = 0.2f;

    // ==================== Map Colors ====================
    /// <summary>Color configuration for map rendering.</summary>
    public static class MapColors
    {
        /// <summary>Color for room start tiles.</summary>
        public static readonly Color RoomStart = new Color(0.95f, 0.88f, 0.24f);

        /// <summary>Color for room exit tiles.</summary>
        public static readonly Color RoomExit = new Color(0.62f, 0.24f, 0.87f);

        /// <summary>Color for wall tiles.</summary>
        public static readonly Color Wall = new Color(0.12f, 0.12f, 0.15f);

        /// <summary>Color for box tiles.</summary>
        public static readonly Color Box = new Color(0.54f, 0.34f, 0.15f);

        /// <summary>Color for portal tiles.</summary>
        public static readonly Color Portal = new Color(0.55f, 0.55f, 0.58f);  // Muted gray instead of pink

        /// <summary>Color for lava tiles.</summary>
        public static readonly Color Lava = new Color(0.88f, 0.34f, 0.12f);

        /// <summary>Color for water tiles.</summary>
        public static readonly Color Water = new Color(0.20f, 0.50f, 0.86f);

        // ========== Scenario Floor Colors ==========
        /// <summary>Floor color for grassland scenario.</summary>
        public static readonly Color ScenarioGrassland = new Color(0.40f, 0.66f, 0.36f);

        /// <summary>Floor color for mountain scenario.</summary>
        public static readonly Color ScenarioMountain = new Color(0.50f, 0.52f, 0.55f);

        /// <summary>Floor color for lava scenario.</summary>
        public static readonly Color ScenarioLava = new Color(0.46f, 0.28f, 0.18f);

        /// <summary>Floor color for sea/water scenario.</summary>
        public static readonly Color ScenarioSea = new Color(0.34f, 0.56f, 0.74f);

        // ========== Debug/Fallback Colors ==========
        /// <summary>Color for debug rectangle fill.</summary>
        public static readonly Color DebugBodyFill = new Color(0.95f, 0.95f, 0.98f);

        /// <summary>Color for debug rectangle outline.</summary>
        public static readonly Color DebugBodyOutline = new Color(0.28f, 0.28f, 0.35f);
    }
}