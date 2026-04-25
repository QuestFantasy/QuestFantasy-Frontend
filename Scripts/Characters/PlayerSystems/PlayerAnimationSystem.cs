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
        Attacking, // Performing basic attack
        Dead,     // Character is dead
        Hit       // Character took damage
    }

    public class PlayerAnimationSystem
    {
        private Sprite _sprite;

        // Reference max texture size across all frames (used for consistent alignment)
        private Vector2 _referenceTextureSize = Vector2.Zero;

        // Stand animations
        private Texture[] _standFrames;

        // Walk animations  
        private Texture[] _walkFrames;

        // Attack animations
        private Texture[] _attackFrames;

        // Dead animation
        private Texture _deadTexture;

        // Hit animation
        private Texture _hitTexture;
        private float _hitTimer = 0f;

        private AnimationState _currentState = AnimationState.Idle;
        private int _frameIndex = 0;
        private float _animationTimer = 0f;
        private bool _hasAllFrames = false;
        // (attack hold timer removed - not used)

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

            // Compute maximum texture size across all frames so we use a
            // consistent reference size. This prevents a single wider or
            // taller attack frame from changing the visual width/height
            // between frames.
            Vector2 textureSize = Vector2.Zero;

            if (_standFrames != null)
            {
                foreach (var t in _standFrames)
                {
                    if (t == null) continue;
                    var s = t.GetSize();
                    textureSize.x = Mathf.Max(textureSize.x, s.x);
                    textureSize.y = Mathf.Max(textureSize.y, s.y);
                }
            }

            if (_walkFrames != null)
            {
                foreach (var t in _walkFrames)
                {
                    if (t == null) continue;
                    var s = t.GetSize();
                    textureSize.x = Mathf.Max(textureSize.x, s.x);
                    textureSize.y = Mathf.Max(textureSize.y, s.y);
                }
            }

            if (_attackFrames != null)
            {
                foreach (var t in _attackFrames)
                {
                    if (t == null) continue;
                    var s = t.GetSize();
                    textureSize.x = Mathf.Max(textureSize.x, s.x);
                    textureSize.y = Mathf.Max(textureSize.y, s.y);
                }
            }

            if (textureSize.x <= 0f || textureSize.y <= 0f)
            {
                return;
            }

            // Store reference size for alignment in ApplyCurrentFrame
            _referenceTextureSize = textureSize;

            // Use a uniform scale based on the max texture size so all frames
            // render with consistent visual width/height. Keep the existing
            // 2x multiplier for larger visuals.
            float scaleX = bodySize.x / textureSize.x;
            float scaleY = bodySize.y / textureSize.y;
            float uniformScale = Mathf.Min(scaleX, scaleY) * 2.5f;
            _sprite.Scale = new Vector2(uniformScale, uniformScale);
        }

        /// <summary>
        /// Update animation based on movement state
        /// </summary>
        public void UpdateAnimation(bool isMoving, float delta, float walkAnimationFps, float facingX)
        {
            if (!_hasAllFrames) return;

            if (_currentState == AnimationState.Dead) return;

            if (_currentState == AnimationState.Hit)
            {
                _hitTimer -= delta;
                if (_hitTimer <= 0)
                {
                    _currentState = AnimationState.Idle;
                }
                else
                {
                    ApplyCurrentFrame(facingX);
                    return;
                }
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

            if (_currentState == AnimationState.Dead) return;

            _currentState = AnimationState.Attacking;
            _frameIndex = 0;
            _animationTimer = 0f;
            // attack hold timer removed

            GD.Print($"[PlayerAnimationSystem] Playing attack animation with {_attackFrames.Length} frames");
        }

        public void SetAttackFrames(Texture[] attackFrames)
        {
            if (attackFrames == null || attackFrames.Length == 0)
            {
                return;
            }

            _attackFrames = attackFrames;
        }

        public void PlayDeadAnimation(Texture deadTexture)
        {
            if (_currentState == AnimationState.Dead) return;

            _deadTexture = deadTexture;
            _currentState = AnimationState.Dead;
            ApplyCurrentFrame(1f); // Just apply the texture
        }

        public void PlayHitAnimation(Texture hitTexture, float duration)
        {
            if (_currentState == AnimationState.Dead) return;

            _currentState = AnimationState.Hit;
            _hitTexture = hitTexture;
            _hitTimer = duration;
            ApplyCurrentFrame(1f); // Keep facing (it will be updated next frame anyway, use 1f or last direction but 1f is just for parameter, FlipH will be recalculated in ApplyCurrentFrame if needed, but wait it flips based on passing facingX. It's fine)
        }

        public void Revive()
        {
            if (_currentState == AnimationState.Dead)
            {
                _currentState = AnimationState.Idle;
                ApplyCurrentFrame(1f);
            }
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
                    // Finish attack animation and return to idle
                    _frameIndex = _attackFrames.Length - 1;
                    _currentState = AnimationState.Idle;
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
                case AnimationState.Dead:
                    if (_deadTexture != null)
                    {
                        frameToUse = _deadTexture;
                    }
                    break;
                case AnimationState.Hit:
                    if (_hitTexture != null)
                    {
                        frameToUse = _hitTexture;
                    }
                    break;
            }

            if (frameToUse != null)
            {
                _sprite.Texture = frameToUse;

                // Align frame vertically so bottoms match the reference size.
                // When the current frame is shorter than the reference height,
                // offset it downward by half the difference (because the sprite
                // is centered). Multiply by scale to convert texture pixels to
                // node space.
                var frameSize = frameToUse.GetSize();
                if (_referenceTextureSize.y > 0f)
                {
                    float dy = (_referenceTextureSize.y - frameSize.y) * 0.5f * _sprite.Scale.y;
                    _sprite.Offset = new Vector2(0f, dy);
                }

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