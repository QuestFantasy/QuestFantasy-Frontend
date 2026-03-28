using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Animation states for the player character
    /// </summary>
    public enum AnimationState
    {
        Idle,    // Standing still
        Walking, // Moving around
        Attacking // Performing basic attack
    }

    public class PlayerAnimationSystem
    {
        private Sprite _sprite;

        // Stand animations
        private Texture[] _standFrames;

        // Walk animations  
        private Texture[] _walkFrames;

        // Attack animations
        private Texture[] _attackFrames;

        private AnimationState _currentState = AnimationState.Idle;
        private int _frameIndex = 0;
        private float _animationTimer = 0f;
        private bool _hasAllFrames = false;
        private float _attackHoldTimer = 0f; // Extra time to hold last attack frame

        public void Initialize(Node2D owner, string standFrame1Path, string standFrame2Path,
                               string walkFrame1Path, string walkFrame2Path,
                               string attackFrame1Path, string attackFrame2Path, string attackFrame3Path,
                               Vector2 bodySize)
        {
            _sprite = new Sprite();
            _sprite.Centered = true;
            owner.AddChild(_sprite);

            // Load stand frames
            Texture standFrame1 = GD.Load<Texture>(standFrame1Path);
            Texture standFrame2 = GD.Load<Texture>(standFrame2Path);

            // Load walk frames (2 frames only)
            Texture walkFrame1 = GD.Load<Texture>(walkFrame1Path);
            Texture walkFrame2 = GD.Load<Texture>(walkFrame2Path);

            // Load attack frames
            Texture attackFrame1 = GD.Load<Texture>(attackFrame1Path);
            Texture attackFrame2 = GD.Load<Texture>(attackFrame2Path);
            Texture attackFrame3 = GD.Load<Texture>(attackFrame3Path);

            _hasAllFrames = standFrame1 != null && standFrame2 != null &&
                           walkFrame1 != null && walkFrame2 != null &&
                           attackFrame1 != null && attackFrame2 != null && attackFrame3 != null;

            if (!_hasAllFrames)
            {
                GD.Print("[PlayerAnimationSystem] Some animation frames not found, using fallback debug rectangle.");
                return;
            }

            _standFrames = new[] { standFrame1, standFrame2 };
            _walkFrames = new[] { walkFrame1, walkFrame2 };
            _attackFrames = new[] { attackFrame1, attackFrame2, attackFrame3 };

            RefreshScale(bodySize);
            ApplyCurrentFrame(1f);
        }

        public void RefreshScale(Vector2 bodySize)
        {
            if (!_hasAllFrames || _sprite == null || _standFrames == null || _standFrames.Length == 0)
            {
                return;
            }

            Vector2 textureSize = _standFrames[0].GetSize();
            if (textureSize.x <= 0f || textureSize.y <= 0f)
            {
                return;
            }

            _sprite.Scale = new Vector2(bodySize.x / textureSize.x, bodySize.y / textureSize.y);
        }

        /// <summary>
        /// Update animation based on movement state
        /// </summary>
        public void UpdateAnimation(bool isMoving, float delta, float walkAnimationFps, float facingX)
        {
            if (!_hasAllFrames)
            {
                return;
            }

            // Determine target state
            AnimationState targetState = isMoving ? AnimationState.Walking : AnimationState.Idle;

            // Reset animation if state changed
            if (targetState != _currentState)
            {
                _currentState = targetState;
                _frameIndex = 0;
                _animationTimer = 0f;
            }

            // Update animation based on current state
            switch (_currentState)
            {
                case AnimationState.Idle:
                    UpdateIdleAnimation(delta);
                    break;
                case AnimationState.Walking:
                    UpdateWalkingAnimation(delta, walkAnimationFps);
                    break;
                case AnimationState.Attacking:
                    // Attack animation will be updated separately
                    break;
            }

            ApplyCurrentFrame(facingX);
        }

        /// <summary>
        /// Update idle (stand) animation
        /// </summary>
        private void UpdateIdleAnimation(float delta)
        {
            if (_standFrames == null || _standFrames.Length == 0)
                return;

            // Very slow stand animation for idle breathing effect
            float frameDuration = 1.0f; // 1.0 second per frame
            _animationTimer += delta;

            if (_animationTimer >= frameDuration)
            {
                _animationTimer -= frameDuration;
                _frameIndex = (_frameIndex + 1) % _standFrames.Length;
            }
        }

        /// <summary>
        /// Update walking animation
        /// </summary>
        private void UpdateWalkingAnimation(float delta, float walkAnimationFps)
        {
            if (_walkFrames == null || _walkFrames.Length == 0)
                return;

            float frameDuration = 1f / Mathf.Max(1f, walkAnimationFps);
            _animationTimer += delta;

            if (_animationTimer >= frameDuration)
            {
                _animationTimer -= frameDuration;
                _frameIndex = (_frameIndex + 1) % _walkFrames.Length;
            }
        }

        /// <summary>
        /// Play attack animation sequence
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (!_hasAllFrames || _attackFrames == null || _attackFrames.Length == 0)
                return;

            _currentState = AnimationState.Attacking;
            _frameIndex = 0;
            _animationTimer = 0f;
            _attackHoldTimer = 0f;

            GD.Print($"[PlayerAnimationSystem] Playing attack animation with {_attackFrames.Length} frames");
        }

        /// <summary>
        /// Update attack animation and return true when finished
        /// </summary>
        public bool UpdateAttackAnimation(float delta, float attackAnimationFps, float facingX)
        {
            if (_currentState != AnimationState.Attacking || _attackFrames == null || _attackFrames.Length == 0)
                return true; // Animation complete

            float frameDuration = 1f / Mathf.Max(1f, attackAnimationFps);
            _animationTimer += delta;

            if (_animationTimer >= frameDuration)
            {
                _animationTimer -= frameDuration;
                _frameIndex++;

                // Check if animation finished
                if (_frameIndex >= _attackFrames.Length)
                {
                    // Hold last frame for 0.1 extra seconds to show impact
                    _attackHoldTimer = 0.1f;
                    _frameIndex = _attackFrames.Length - 1;
                    _currentState = AnimationState.Idle; // Return to idle at end
                    ApplyCurrentFrame(facingX);
                    return true; // Animation complete
                }
            }

            ApplyCurrentFrame(facingX);
            return false; // Still animating
        }

        private void ApplyCurrentFrame(float facingX)
        {
            if (!_hasAllFrames || _sprite == null)
            {
                return;
            }

            Texture frameToUse = null;

            switch (_currentState)
            {
                case AnimationState.Idle:
                    if (_standFrames != null && _standFrames.Length > 0)
                        frameToUse = _standFrames[_frameIndex % _standFrames.Length];
                    break;
                case AnimationState.Walking:
                    if (_walkFrames != null && _walkFrames.Length > 0)
                        frameToUse = _walkFrames[_frameIndex % _walkFrames.Length];
                    break;
                case AnimationState.Attacking:
                    if (_attackFrames != null && _attackFrames.Length > 0)
                    {
                        int safeIndex = Mathf.Clamp(_frameIndex, 0, _attackFrames.Length - 1);
                        frameToUse = _attackFrames[safeIndex];
                    }
                    break;
            }

            if (frameToUse != null)
            {
                _sprite.Texture = frameToUse;
                _sprite.FlipH = facingX < 0f;
            }
        }

        public void DrawFallback(Node2D owner, Vector2 bodySize)
        {
            if (_hasAllFrames)
            {
                return;
            }

            Rect2 rect = new Rect2(-bodySize / 2f, bodySize);
            owner.DrawRect(rect, GameConstants.MapColors.DebugBodyFill);
            owner.DrawRect(rect.Grow(-2), GameConstants.MapColors.DebugBodyOutline, false, 2f);
        }

        public AnimationState CurrentState => _currentState;
        public bool IsAttacking => _currentState == AnimationState.Attacking;
    }
}