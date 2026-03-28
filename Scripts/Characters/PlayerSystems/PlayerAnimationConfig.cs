using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Centralized animation configuration for the player.
    /// Includes all animation paths, FPS settings, and validation logic.
    /// </summary>
    public class PlayerAnimationConfig
    {
        // Stand animation
        public string StandFrame1Path { get; set; } = "res://Assets/Characters/stand.png";
        public string StandFrame2Path { get; set; } = "res://Assets/Characters/stand2.png";

        // Walk animation
        public float WalkAnimationFps { get; set; } = GameConstants.PLAYER_WALK_ANIMATION_FPS;
        public string WalkFrame1Path { get; set; } = "res://Assets/Characters/walk.png";
        public string WalkFrame2Path { get; set; } = "res://Assets/Characters/walk1.png";

        // Attack animation
        public float AttackAnimationFps { get; set; } = GameConstants.PLAYER_ATTACK_ANIMATION_FPS;
        public string AttackFrame1Path { get; set; } = "res://Assets/Characters/attack.png";
        public string AttackFrame2Path { get; set; } = "res://Assets/Characters/attack1.png";
        public string AttackFrame3Path { get; set; } = "res://Assets/Characters/attack2.png";

        /// <summary>
        /// Validate all animation settings and reset to defaults if invalid
        /// </summary>
        public void Validate()
        {
            ValidateAnimationFPS();
            ValidateAnimationPaths();
        }

        /// <summary>
        /// Validate animation FPS values
        /// </summary>
        private void ValidateAnimationFPS()
        {
            if (WalkAnimationFps <= 0)
            {
                GD.PrintErr("[PlayerAnimationConfig] WalkAnimationFps must be > 0, resetting to default");
                WalkAnimationFps = GameConstants.PLAYER_WALK_ANIMATION_FPS;
            }

            if (AttackAnimationFps <= 0)
            {
                GD.PrintErr("[PlayerAnimationConfig] AttackAnimationFps must be > 0, resetting to default");
                AttackAnimationFps = GameConstants.PLAYER_ATTACK_ANIMATION_FPS;
            }
        }

        /// <summary>
        /// Validate animation frame paths
        /// </summary>
        private void ValidateAnimationPaths()
        {
            if (string.IsNullOrEmpty(StandFrame1Path) || string.IsNullOrEmpty(StandFrame2Path))
            {
                GD.PrintErr("[PlayerAnimationConfig] WARNING: Stand animation frame paths are missing");
            }

            if (string.IsNullOrEmpty(WalkFrame1Path) || string.IsNullOrEmpty(WalkFrame2Path))
            {
                GD.PrintErr("[PlayerAnimationConfig] WARNING: Walk animation frame paths are missing");
            }

            if (string.IsNullOrEmpty(AttackFrame1Path) || string.IsNullOrEmpty(AttackFrame2Path) ||
                string.IsNullOrEmpty(AttackFrame3Path))
            {
                GD.PrintErr("[PlayerAnimationConfig] WARNING: Attack animation frame paths are missing");
            }
        }

        /// <summary>
        /// Reset all animation settings to defaults from GameConstants
        /// </summary>
        public void ResetToDefaults()
        {
            StandFrame1Path = "res://Assets/Characters/stand.png";
            StandFrame2Path = "res://Assets/Characters/stand2.png";
            WalkAnimationFps = GameConstants.PLAYER_WALK_ANIMATION_FPS;
            WalkFrame1Path = "res://Assets/Characters/walk.png";
            WalkFrame2Path = "res://Assets/Characters/walk1.png";
            AttackAnimationFps = GameConstants.PLAYER_ATTACK_ANIMATION_FPS;
            AttackFrame1Path = "res://Assets/Characters/attack.png";
            AttackFrame2Path = "res://Assets/Characters/attack1.png";
            AttackFrame3Path = "res://Assets/Characters/attack2.png";
        }
    }
}