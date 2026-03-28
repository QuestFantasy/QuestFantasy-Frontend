using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Validates player configuration parameters and resets invalid values to defaults.
    /// Centralizes all parameter validation logic used during player initialization.
    /// </summary>
    public class PlayerConfigValidator
    {
        public class PlayerConfig
        {
            public float MoveSpeed { get; set; } = GameConstants.PLAYER_DEFAULT_MOVE_SPEED;
            public Vector2 BodySizeInTiles { get; set; } = GameConstants.PLAYER_BODY_SIZE_IN_TILES;
            public Vector2 CollisionBodyScale { get; set; } = GameConstants.PLAYER_COLLISION_SCALE;
            public Vector2 CameraZoom { get; set; } = GameConstants.PLAYER_CAMERA_DEFAULT_ZOOM;
            public float SpeedMultiplier { get; set; } = GameConstants.PLAYER_SPEED_TO_PIXELS_MULTIPLIER;
        }

        /// <summary>
        /// Validate all player configuration parameters
        /// </summary>
        public static void ValidateAll(PlayerConfig config)
        {
            if (config == null)
            {
                GD.PrintErr("[PlayerConfigValidator] Cannot validate null config");
                return;
            }

            ValidateMoveSpeed(config);
            ValidateBodySize(config);
            ValidateCollisionScale(config);
            ValidateCameraZoom(config);
            ValidateSpeedMultiplier(config);
        }

        /// <summary>
        /// Validate movement speed
        /// </summary>
        private static void ValidateMoveSpeed(PlayerConfig config)
        {
            if (config.MoveSpeed <= 0)
            {
                GD.PrintErr("[PlayerConfigValidator] MoveSpeed must be > 0, resetting to default");
                config.MoveSpeed = GameConstants.PLAYER_DEFAULT_MOVE_SPEED;
            }
        }

        /// <summary>
        /// Validate body size in tiles
        /// </summary>
        private static void ValidateBodySize(PlayerConfig config)
        {
            if (config.BodySizeInTiles.x <= 0 || config.BodySizeInTiles.y <= 0)
            {
                GD.PrintErr("[PlayerConfigValidator] BodySizeInTiles must be > 0, resetting to default");
                config.BodySizeInTiles = GameConstants.PLAYER_BODY_SIZE_IN_TILES;
            }
        }

        /// <summary>
        /// Validate collision body scale (should be between 0 and 1)
        /// </summary>
        private static void ValidateCollisionScale(PlayerConfig config)
        {
            bool isInvalid = config.CollisionBodyScale.x <= 0 || config.CollisionBodyScale.x > 1 ||
                             config.CollisionBodyScale.y <= 0 || config.CollisionBodyScale.y > 1;

            if (isInvalid)
            {
                GD.PrintErr("[PlayerConfigValidator] CollisionBodyScale must be between 0 and 1, resetting to default");
                config.CollisionBodyScale = GameConstants.PLAYER_COLLISION_SCALE;
            }
        }

        /// <summary>
        /// Validate camera zoom
        /// </summary>
        private static void ValidateCameraZoom(PlayerConfig config)
        {
            if (config.CameraZoom.x <= 0 || config.CameraZoom.y <= 0)
            {
                GD.PrintErr("[PlayerConfigValidator] CameraZoom must be > 0, resetting to default");
                config.CameraZoom = GameConstants.PLAYER_CAMERA_DEFAULT_ZOOM;
            }
        }

        /// <summary>
        /// Validate speed multiplier
        /// </summary>
        private static void ValidateSpeedMultiplier(PlayerConfig config)
        {
            if (config.SpeedMultiplier <= 0)
            {
                GD.PrintErr("[PlayerConfigValidator] SpeedMultiplier must be > 0, resetting to default");
                config.SpeedMultiplier = GameConstants.PLAYER_SPEED_TO_PIXELS_MULTIPLIER;
            }
        }
    }
}
