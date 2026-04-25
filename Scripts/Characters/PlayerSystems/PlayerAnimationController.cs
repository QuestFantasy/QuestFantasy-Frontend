using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    public enum AttackAnimationStyle
    {
        Sword,
        Bow,
        Fireball,
    }

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
        private readonly Texture[] _swordAttackFrames;
        private readonly Texture[] _bowAttackFrames;
        private readonly Texture[] _fireballAttackFrames;

        public bool IsAttacking => _isAttacking;

        public PlayerAnimationController(
            PlayerAnimationSystem animationSystem,
            PlayerAnimationConfig animationConfig)
        {
            _animationSystem = animationSystem;
            _animationConfig = animationConfig;

            _swordAttackFrames = BuildFrames(
                _animationConfig.AttackFrame1Path,
                _animationConfig.AttackFrame2Path,
                _animationConfig.AttackFrame3Path);

            _bowAttackFrames = BuildFrames(
                "res://Assets/Characters/shot_prepare.png",
                "res://Assets/Characters/shot.png",
                "res://Assets/Characters/shoted.png");

            _fireballAttackFrames = BuildFrames(
                "res://Assets/Characters/magic.png",
                "res://Assets/Characters/magic1.png",
                "res://Assets/Characters/magic1.png");
        }

        /// <summary>
        /// Update all animation-related logic for a frame
        /// </summary>
        public void Update(Vector2 input, float delta)
        {
            // Update player facing direction based on input only if not attacking
            if (!_isAttacking && Mathf.Abs(input.x) > 0.01f)
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
        public void PlayAttackAnimation(AttackAnimationStyle style = AttackAnimationStyle.Sword)
        {
            _isAttacking = true;

            Texture[] selectedFrames = _swordAttackFrames;
            if (style == AttackAnimationStyle.Bow)
            {
                selectedFrames = _bowAttackFrames;
            }
            else if (style == AttackAnimationStyle.Fireball)
            {
                selectedFrames = _fireballAttackFrames;
            }

            _animationSystem.SetAttackFrames(selectedFrames);
            _animationSystem.PlayAttackAnimation();
        }

        /// <summary>
        /// Trigger dead animation playback
        /// </summary>
        public void PlayDeadAnimation(Texture deadTexture)
        {
            _animationSystem.PlayDeadAnimation(deadTexture);
        }

        /// <summary>
        /// Trigger hit animation playback
        /// </summary>
        public void PlayHitAnimation(Texture hitTexture, float duration = 0.2f)
        {
            _animationSystem.PlayHitAnimation(hitTexture, duration);
        }

        public void Revive()
        {
            _animationSystem.Revive();
            _isAttacking = false;
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
        /// Explicitly set the facing direction
        /// </summary>
        public void SetFacingDirection(float facingX)
        {
            if (Mathf.Abs(facingX) > 0.01f)
            {
                _lastFacingX = Mathf.Sign(facingX);
            }
        }

        /// <summary>
        /// Refresh animation scale when map dimensions change
        /// </summary>
        public void RefreshAnimationScale(Vector2 bodySizeInPixels)
        {
            _animationSystem.RefreshScale(bodySizeInPixels);
        }

        private static Texture[] BuildFrames(string frame1, string frame2, string frame3)
        {
            return new[]
            {
                GD.Load<Texture>(frame1),
                GD.Load<Texture>(frame2),
                GD.Load<Texture>(frame3),
            };
        }
    }
}