using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Handles all animation-related logic for the player:
    /// - Attack animation state management
    /// - Walk/idle animation updates
    /// - Animation state transitions
    /// </summary>
    public class PlayerAnimationController
    {
        private readonly PlayerAnimationSystem _animationSystem;
        private readonly PlayerAnimationConfig _animationConfig;

        private bool _isAttacking = false;
        private float _lastFacingX = 1f;

        public bool IsAttacking => _isAttacking;

        public PlayerAnimationController(
            PlayerAnimationSystem animationSystem,
            PlayerAnimationConfig animationConfig)
        {
            _animationSystem = animationSystem;
            _animationConfig = animationConfig;
        }

        /// <summary>
        /// Update all animation-related logic for a frame
        /// </summary>
        public void Update(Vector2 input, float delta)
        {
            // Update player facing direction based on input
            if (Mathf.Abs(input.x) > 0.01f)
            {
                _lastFacingX = input.x;
            }

            // Handle attack animation state
            if (_isAttacking)
            {
                UpdateAttackAnimationState(delta);
            }
            else
            {
                UpdateNormalAnimationState(input, delta);
            }
        }

        /// <summary>
        /// Update attack animation and check if finished
        /// </summary>
        private void UpdateAttackAnimationState(float delta)
        {
            bool attackFinished = _animationSystem.UpdateAttackAnimation(
                delta,
                _animationConfig.AttackAnimationFps,
                _lastFacingX);

            if (attackFinished)
            {
                _isAttacking = false;
                GD.Print("[PlayerAnimationController] Attack animation finished");
            }
        }

        /// <summary>
        /// Update walk/idle animation based on movement
        /// </summary>
        private void UpdateNormalAnimationState(Vector2 input, float delta)
        {
            bool isMoving = input.LengthSquared() > 0;
            _animationSystem.UpdateAnimation(
                isMoving,
                delta,
                _animationConfig.WalkAnimationFps,
                _lastFacingX);
        }

        /// <summary>
        /// Trigger attack animation playback
        /// </summary>
        public void PlayAttackAnimation()
        {
            _isAttacking = true;
            _animationSystem.PlayAttackAnimation();
        }

        /// <summary>
        /// Reset attack state without playing animation
        /// </summary>
        public void ResetAttackState()
        {
            _isAttacking = false;
        }

        /// <summary>
        /// Get the direction the player is currently facing
        /// </summary>
        public float GetFacingDirection()
        {
            return _lastFacingX;
        }

        /// <summary>
        /// Refresh animation scale when map dimensions change
        /// </summary>
        public void RefreshAnimationScale(Vector2 bodySizeInPixels)
        {
            _animationSystem.RefreshScale(bodySizeInPixels);
        }
    }
}
