using Godot;

/// <summary>
/// Centralized game logic constants to avoid Magic Numbers scattered throughout the codebase.
/// </summary>
public static class GameConstants
{
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
        public static readonly Color Portal = new Color(0.88f, 0.26f, 0.80f);

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