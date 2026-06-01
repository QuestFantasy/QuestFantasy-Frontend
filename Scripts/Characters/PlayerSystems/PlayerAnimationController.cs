using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    public enum AttackAnimationStyle
    {
        Sword,
        Bow,
        Fireball,
        ArcherShot,
        KnightExplose,
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

        // Per-skill-style attack frames — updated by UpdateClassFrames() on class switch.
        private Texture[] _swordAttackFrames;
        private Texture[] _bowAttackFrames;
        private Texture[] _fireballAttackFrames;
        private readonly Texture[] _archerShotFrames;
        private readonly Texture[] _knightExploseFrames;

        private Texture _defenseTexture;
        private Texture _counterTexture;

        // Default (Adventurer) frame paths for each skill style.
        private static readonly string[] DefaultSwordPaths = new[]
        {
            "res://Assets/Characters/adventurer/slash.png",
            "res://Assets/Characters/adventurer/slash1.png",
            "res://Assets/Characters/adventurer/slash2.png",
        };
        private static readonly string[] DefaultBowPaths = new[]
        {
            "res://Assets/Characters/adventurer/shot_prepare.png",
            "res://Assets/Characters/adventurer/shot.png",
            "res://Assets/Characters/adventurer/shoted.png",
        };
        private static readonly string[] DefaultFireballPaths = new[]
        {
            "res://Assets/Characters/adventurer/magic.png",
            "res://Assets/Characters/adventurer/magic1.png",
            "res://Assets/Characters/adventurer/magic1.png",
        };

        public bool IsAttacking => _isAttacking;

        public PlayerAnimationController(
            PlayerAnimationSystem animationSystem,
            PlayerAnimationConfig animationConfig)
        {
            _animationSystem = animationSystem;
            _animationConfig = animationConfig;

            // Load default (Adventurer) attack frames at startup.
            _swordAttackFrames = BuildFrames(
                _animationConfig.AttackFrame1Path,
                _animationConfig.AttackFrame2Path,
                _animationConfig.AttackFrame3Path);

            _bowAttackFrames = BuildFrames(DefaultBowPaths[0], DefaultBowPaths[1], DefaultBowPaths[2]);
            _fireballAttackFrames = BuildFrames(DefaultFireballPaths[0], DefaultFireballPaths[1], DefaultFireballPaths[2]);

            _archerShotFrames = new[]
            {
                GD.Load<Texture>("res://Assets/Characters/archer/shot.png"),
                GD.Load<Texture>("res://Assets/Characters/archer/shot1.png")
            };
            _knightExploseFrames = new[]
            {
                GD.Load<Texture>("res://Assets/Characters/warrior/super_attack.png")
            };
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
            else if (style == AttackAnimationStyle.ArcherShot)
            {
                selectedFrames = _archerShotFrames;
            }
            else if (style == AttackAnimationStyle.KnightExplose)
            {
                selectedFrames = _knightExploseFrames;
            }

            _animationSystem.SetAttackFrames(selectedFrames);
            _animationSystem.PlayAttackAnimation();
        }

        /// <summary>
        /// Reload per-style attack frames to match the active player class.
        /// Pass null for a style to fall back to the shared Adventurer frames.
        /// </summary>
        /// <param name="swordPaths">3-element array: [frame1, frame2, frame3] for sword style, or null to reset to default.</param>
        /// <param name="bowPaths">3-element array for bow style, or null to reset to default.</param>
        /// <param name="fireballPaths">3-element array for fireball style, or null to reset to default.</param>
        public void UpdateClassFrames(string[] swordPaths, string[] bowPaths, string[] fireballPaths)
        {
            if (swordPaths != null && swordPaths.Length >= 3)
            {
                _swordAttackFrames = BuildFramesFallback(swordPaths, DefaultSwordPaths);
            }
            else
            {
                _swordAttackFrames = BuildFrames(
                    _animationConfig.AttackFrame1Path,
                    _animationConfig.AttackFrame2Path,
                    _animationConfig.AttackFrame3Path);
            }

            _bowAttackFrames = bowPaths != null && bowPaths.Length >= 3
                ? BuildFramesFallback(bowPaths, DefaultBowPaths)
                : BuildFrames(DefaultBowPaths[0], DefaultBowPaths[1], DefaultBowPaths[2]);

            _fireballAttackFrames = fireballPaths != null && fireballPaths.Length >= 3
                ? BuildFramesFallback(fireballPaths, DefaultFireballPaths)
                : BuildFrames(DefaultFireballPaths[0], DefaultFireballPaths[1], DefaultFireballPaths[2]);

            GD.Print("[PlayerAnimationController] Attack frames updated for class.");
        }

        public void LoadDefenseTextures(string defensePath, string counterPath)
        {
            _defenseTexture = !string.IsNullOrEmpty(defensePath) ? GD.Load<Texture>(defensePath) : null;
            _counterTexture = !string.IsNullOrEmpty(counterPath) ? GD.Load<Texture>(counterPath) : null;
        }

        public void PlayDefenseAnimation()
        {
            if (_defenseTexture != null)
                _animationSystem.PlayDefenseAnimation(_defenseTexture);
        }

        public void PlayDefenseCounterAnimation(float duration = 0.2f)
        {
            if (_counterTexture != null)
                _animationSystem.PlayDefenseCounterAnimation(_counterTexture, duration);
        }

        public void StopDefenseAnimation()
        {
            _animationSystem.StopDefenseAnimation();
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

        /// <summary>
        /// Load frames from <paramref name="paths"/>, falling back to the corresponding
        /// <paramref name="fallbackPaths"/> entry when a primary path cannot be loaded.
        /// </summary>
        private static Texture[] BuildFramesFallback(string[] paths, string[] fallbackPaths)
        {
            var frames = new Texture[3];
            for (int i = 0; i < 3; i++)
            {
                string primary = i < paths.Length ? paths[i] : null;
                string fallback = i < fallbackPaths.Length ? fallbackPaths[i] : null;

                Texture tex = (!string.IsNullOrEmpty(primary)) ? GD.Load<Texture>(primary) : null;
                if (tex == null && !string.IsNullOrEmpty(fallback))
                {
                    tex = GD.Load<Texture>(fallback);
                }
                frames[i] = tex;
            }
            return frames;
        }
    }
}