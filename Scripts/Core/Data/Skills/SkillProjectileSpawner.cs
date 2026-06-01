using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Systems.StatusEffects;

namespace QuestFantasy.Core.Data.Skills
{
    internal static class SkillProjectileSpawner
    {
        public static void SpawnArrow(
            Player player,
            Character target,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateArrow(player, direction, maxRange, 0, onHitEffect, onHitChance);
            AttachToScene(player, node);
        }

        public static void SpawnTripleArrow(
            Player player,
            Character target,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            Vector2 direction = ResolveDirection(player, target);

            // Calculate a perpendicular offset vector (10 pixels wide) to separate the arrows
            Vector2 perpOffset = new Vector2(-direction.y, direction.x) * 12f;

            var nodeCenter = SkillProjectileNode.CreateArrow(player, direction, maxRange, 0, onHitEffect, onHitChance);
            var nodeLeft = SkillProjectileNode.CreateArrow(player, direction, maxRange, 0, onHitEffect, onHitChance);
            var nodeRight = SkillProjectileNode.CreateArrow(player, direction, maxRange, 0, onHitEffect, onHitChance);

            AttachToScene(player, nodeCenter);
            AttachToScene(player, nodeLeft);
            AttachToScene(player, nodeRight);

            // Apply the visual offsets
            nodeLeft.GlobalPosition += perpOffset;
            nodeRight.GlobalPosition -= perpOffset;
        }

        public static void SpawnFireball(
            Player player,
            Character target,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateFireball(player, direction, maxRange, onHitEffect, onHitChance);
            AttachToScene(player, node);
        }

        public static void SpawnTripleFireball(
            Player player,
            Character target,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            Vector2 direction = ResolveDirection(player, target);

            // Calculate spread directions (±15 degrees = ±0.261799 radians)
            float spreadAngle = 15f * Mathf.Pi / 180f;
            Vector2 dirLeft = direction.Rotated(-spreadAngle);
            Vector2 dirRight = direction.Rotated(spreadAngle);

            var nodeCenter = SkillProjectileNode.CreateFireball(player, direction, maxRange, onHitEffect, onHitChance);
            var nodeLeft = SkillProjectileNode.CreateFireball(player, dirLeft, maxRange, onHitEffect, onHitChance);
            var nodeRight = SkillProjectileNode.CreateFireball(player, dirRight, maxRange, onHitEffect, onHitChance);

            AttachToScene(player, nodeCenter);
            AttachToScene(player, nodeLeft);
            AttachToScene(player, nodeRight);
        }

        public static void SpawnGiantFireball(
            Player player,
            Character target,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateGiantFireball(player, direction, maxRange, onHitEffect, onHitChance);
            AttachToScene(player, node);
        }

        public static void SpawnFlyingSword(
            Player player,
            Character target,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateFlyingSword(player, direction, maxRange, onHitEffect, onHitChance);
            AttachToScene(player, node);
        }

        public static void SpawnRicochetArrow(
            Player player,
            Character target,
            float maxRange,
            int bounces,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateArrow(player, direction, maxRange, bounces, onHitEffect, onHitChance);
            AttachToScene(player, node);
        }

        public static void SpawnKnightExplosion(Player player)
        {
            if (player == null)
            {
                return;
            }

            var node = new KnightExplosionNode(player);
            AttachToScene(player, node);
            node.GlobalPosition = player.GlobalPosition;

            GD.Print($"[KnightExplosion] Spawned at {player.GlobalPosition}");
        }

        public static void SpawnDigitArrow(Player player, Character target, float maxRange)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateDigitArrow(player, direction, maxRange);
            AttachToScene(player, node);
        }

        public static void SpawnSuperArrow(
            Player player,
            Character target,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateSuperArrow(player, direction, maxRange, onHitEffect, onHitChance);
            AttachToScene(player, node);
        }

        public static void SpawnIceSpear(
            Player player,
            Character target,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateIceSpear(player, direction, maxRange, onHitEffect, onHitChance);
            AttachToScene(player, node);
        }

        public static void SpawnMagicSlash(Player player, Character target, float maxRange)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateMagicSlash(player, direction, maxRange);
            AttachToScene(player, node);
        }

        public static void SpawnTemporaryVisualEffect(
            Player player,
            string texturePath,
            Vector2 position,
            float duration,
            float initialScale,
            float targetScale = -1f,
            float rotationSpeed = 0f,
            bool flipH = false,
            float initialRotation = 0f)
        {
            var node = new TemporaryVisualEffectNode(texturePath, duration, initialScale, targetScale, rotationSpeed, flipH, initialRotation);
            AttachToScene(player, node);
            node.GlobalPosition = position;
        }

        private static Vector2 ResolveDirection(Player player, Character target)
        {
            if (target != null && Godot.Object.IsInstanceValid(target))
            {
                Vector2 towardTarget = target.GlobalPosition - player.GlobalPosition;
                if (towardTarget.LengthSquared() > 0.0001f)
                {
                    return towardTarget.Normalized();
                }
            }

            Vector2 towardMouse = player.GetGlobalMousePosition() - player.GlobalPosition;
            if (towardMouse.LengthSquared() > 0.0001f)
            {
                return towardMouse.Normalized();
            }

            return Vector2.Right;
        }

        private static void AttachToScene(Player player, Node2D node)
        {
            if (player == null || !Godot.Object.IsInstanceValid(player))
            {
                return;
            }

            Node parent = player.GetTree()?.Root;
            if (parent == null || node == null)
            {
                return;
            }

            parent.AddChild(node);
            node.GlobalPosition = player.GlobalPosition;
        }
    }

    internal class SkillProjectileNode : Node2D
    {
        private const float ArrowSpeed = 450f;
        private const float FireballSpeed = 320f;
        private const float ArrowProjectileScale = 0.3f;
        private const float ArrowImpactScale = 0.3f;
        private const float FireballProjectileScale = 0.5f;
        private const float FireballImpactScale = 0.6f;
        private const float ArrowImpactFrameDuration = 0.05f;
        private const float FireballFlightFrameDuration = 0.11f;
        private const float FireballImpactFrameDuration = 0.09f;

        private readonly List<Character> _damagedTargets = new List<Character>();

        private Player _owner;
        private Map _map;
        private Vector2 _direction = Vector2.Right;
        private float _speed;
        private float _maxDistance;
        private float _traveled;
        private float _hitRadius;
        private int _damageMin;
        private int _damageMax;
        private bool _isAoe;
        private float _aoeRadius;
        private Texture _projectileTexture;
        private Texture[] _flightFrames;
        private Texture[] _impactFrames;
        private float _projectileScale = FireballProjectileScale;
        private float _impactScale = FireballImpactScale;

        // Status effect applied on hit — factory keeps each target getting its own instance
        private Func<StatusEffect> _onHitEffect;
        private float _onHitChance;

        private Sprite _sprite;
        private bool _impacting;
        private float _impactTimer;
        private int _impactFrameIndex;
        private float _flightFrameTimer;
        private int _flightFrameIndex = -1;
        private float _flightFrameDuration = 0f;
        private float _impactFrameDuration = ArrowImpactFrameDuration;
        private int _bouncesRemaining = 0;
        private readonly float _bounceRange = 150f;
        private bool _isPiercing = false;
        private bool _isReturning = false;
        private bool _returningPhase = false;
        private bool _ignoreWalls = false;
        private bool _loopFlightAnimation = true;

        public static SkillProjectileNode CreateArrow(
            Player owner,
            Vector2 direction,
            float maxRange,
            int bounces = 0,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            string arrowTexturePath = bounces > 0
                ? "res://Assets/SkillAnimation/arrow_ricochet.png"
                : "res://Assets/SkillAnimation/arrow.png";

            return new SkillProjectileNode
            {
                _owner = owner,
                _map = FindMap(owner),
                _direction = direction,
                _speed = ArrowSpeed,
                _maxDistance = Mathf.Max(10f, maxRange),
                _hitRadius = 12f,
                _damageMin = 2,
                _damageMax = 4,
                _isAoe = false,
                _aoeRadius = 0f,
                _bouncesRemaining = bounces,
                _onHitEffect = onHitEffect,
                _onHitChance = onHitChance,
                _projectileTexture = GD.Load<Texture>(arrowTexturePath),
                _projectileScale = ArrowProjectileScale,
                _impactScale = ArrowImpactScale,
                _flightFrames = new[]
                {
                    GD.Load<Texture>(arrowTexturePath),
                },
                _impactFrames = new[]
                {
                    GD.Load<Texture>(arrowTexturePath),
                }
            };
        }

        public static SkillProjectileNode CreateFireball(
            Player owner,
            Vector2 direction,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            return new SkillProjectileNode
            {
                _owner = owner,
                _map = FindMap(owner),
                _direction = direction,
                _speed = FireballSpeed,
                _maxDistance = Mathf.Max(10f, maxRange),
                _hitRadius = 14f,
                _damageMin = 3,
                _damageMax = 6,
                _isAoe = true,
                _aoeRadius = 52f,
                _onHitEffect = onHitEffect,
                _onHitChance = onHitChance,
                _projectileTexture = GD.Load<Texture>("res://Assets/SkillAnimation/fireball.png"),
                _projectileScale = FireballProjectileScale,
                _impactScale = FireballImpactScale,
                _flightFrameDuration = FireballFlightFrameDuration,
                _impactFrameDuration = FireballImpactFrameDuration,
                _flightFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball1.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball2.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball3.png"),
                },
                _impactFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit1.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit2.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit3.png"),
                }
            };
        }

        public static SkillProjectileNode CreateGiantFireball(
            Player owner,
            Vector2 direction,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            return new SkillProjectileNode
            {
                _owner = owner,
                _map = FindMap(owner),
                _direction = direction,
                _speed = FireballSpeed * 0.8f, // Slightly slower because it's massive
                _maxDistance = Mathf.Max(10f, maxRange),
                _hitRadius = 24f,
                _damageMin = 10,
                _damageMax = 18,
                _isAoe = true,
                _aoeRadius = 80f,
                _onHitEffect = onHitEffect,
                _onHitChance = onHitChance,
                _projectileTexture = GD.Load<Texture>("res://Assets/SkillAnimation/fireball.png"),
                _projectileScale = FireballProjectileScale * 2f,
                _impactScale = FireballImpactScale * 2f,
                _flightFrameDuration = FireballFlightFrameDuration,
                _impactFrameDuration = FireballImpactFrameDuration,
                _flightFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball1.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball2.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball3.png"),
                },
                _impactFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit1.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit2.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit3.png"),
                }
            };
        }

        public static SkillProjectileNode CreateFlyingSword(
            Player owner,
            Vector2 direction,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            return new SkillProjectileNode
            {
                _owner = owner,
                _map = FindMap(owner),
                _direction = direction,
                _speed = ArrowSpeed * 0.9f,
                _maxDistance = Mathf.Max(10f, maxRange),
                _hitRadius = 16f,
                _damageMin = 6,
                _damageMax = 12,
                _isAoe = false,
                _aoeRadius = 0f,
                _isPiercing = true,
                _isReturning = true,
                _onHitEffect = onHitEffect,
                _onHitChance = onHitChance,
                _projectileTexture = GD.Load<Texture>("res://Assets/SkillAnimation/fly_sword.png"),
                _projectileScale = 0.3f,
                _impactScale = 0.3f,
                _flightFrameDuration = 0.08f,
                _impactFrameDuration = ArrowImpactFrameDuration,
                _flightFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/fly_sword.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fly_sword-1.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fly_sword-2.png"),
                },
                _impactFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/fly_sword.png"),
                }
            };
        }

        public static SkillProjectileNode CreateDigitArrow(
            Player owner,
            Vector2 direction,
            float maxRange)
        {
            return new SkillProjectileNode
            {
                _owner = owner,
                _map = FindMap(owner),
                _direction = direction,
                _speed = ArrowSpeed * 1.3f,
                _maxDistance = Mathf.Max(10f, maxRange),
                _hitRadius = 12f,
                _damageMin = 4,
                _damageMax = 7,
                _isAoe = false,
                _isPiercing = true,
                _ignoreWalls = true,
                _projectileTexture = GD.Load<Texture>("res://Assets/SkillAnimation/digit_arrow.png"),
                _projectileScale = 0.25f,
                _impactScale = 0.25f,
                _flightFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/digit_arrow.png"),
                },
                _impactFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/digit_arrow.png"),
                }
            };
        }

        public static SkillProjectileNode CreateSuperArrow(
            Player owner,
            Vector2 direction,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            return new SkillProjectileNode
            {
                _owner = owner,
                _map = FindMap(owner),
                _direction = direction,
                _speed = ArrowSpeed * 0.75f,
                _maxDistance = Mathf.Max(10f, maxRange),
                _hitRadius = 28f,
                _damageMin = 8,
                _damageMax = 15,
                _isAoe = false,
                _ignoreWalls = true,
                _isPiercing = true,
                _onHitEffect = onHitEffect,
                _onHitChance = onHitChance,
                _projectileTexture = GD.Load<Texture>("res://Assets/SkillAnimation/super_arrow.png"),
                _projectileScale = 0.55f,
                _impactScale = 0.55f,
                _flightFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/super_arrow.png"),
                },
                _impactFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/super_arrow.png"),
                }
            };
        }

        public static SkillProjectileNode CreateIceSpear(
            Player owner,
            Vector2 direction,
            float maxRange,
            Func<StatusEffect> onHitEffect = null,
            float onHitChance = 0f)
        {
            return new SkillProjectileNode
            {
                _owner = owner,
                _map = FindMap(owner),
                _direction = direction,
                _speed = ArrowSpeed * 0.6f,
                _maxDistance = Mathf.Max(10f, maxRange),
                _hitRadius = 15f,
                _damageMin = 5,
                _damageMax = 9,
                _isAoe = false,
                _onHitEffect = onHitEffect,
                _onHitChance = onHitChance,
                _projectileTexture = GD.Load<Texture>("res://Assets/SkillAnimation/ice_spear-1.png"),
                _projectileScale = 0.3f,
                _impactScale = 0.35f,
                _flightFrameDuration = 0.1f,
                _impactFrameDuration = 0.08f,
                _loopFlightAnimation = false,
                _flightFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/ice_spear-1.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/ice_spear-2.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/ice_spear-3.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/ice_spear-4.png"),
                },
                _impactFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/ice_spear-hit.png"),
                }
            };
        }

        public static SkillProjectileNode CreateMagicSlash(
            Player owner,
            Vector2 direction,
            float maxRange)
        {
            return new SkillProjectileNode
            {
                _owner = owner,
                _map = FindMap(owner),
                _direction = direction,
                _speed = ArrowSpeed * 1.2f,
                _maxDistance = Mathf.Max(10f, maxRange),
                _hitRadius = 20f,
                _damageMin = 4,
                _damageMax = 8,
                _isAoe = false,
                _onHitEffect = null,
                _onHitChance = 0f,
                _projectileTexture = GD.Load<Texture>("res://Assets/SkillAnimation/magic_slash-2.png"),
                _projectileScale = 0.3f,
                _impactScale = 0.3f,
                _flightFrameDuration = 0f,
                _impactFrameDuration = ArrowImpactFrameDuration,
                _flightFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/magic_slash-2.png"),
                },
                _impactFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/magic_slash-2.png"),
                }
            };
        }

        public override void _Ready()
        {
            Texture firstFlightTexture = _projectileTexture;

            _sprite = new Sprite
            {
                Texture = firstFlightTexture,
                Centered = true,
                Scale = new Vector2(_projectileScale, _projectileScale),
            };
            AddChild(_sprite);

            _flightFrameIndex = -1;
            _flightFrameTimer = 0f;

            if (_direction.LengthSquared() < 0.0001f)
            {
                _direction = Vector2.Right;
            }
            _direction = _direction.Normalized();
            Rotation = _direction.Angle();

            SetProcess(true);
        }

        public override void _Process(float delta)
        {
            if (!IsOwnerAlive())
            {
                QueueFree();
                return;
            }

            if (_impacting)
            {
                UpdateImpact(delta);
                return;
            }

            UpdateFlightAnimation(delta);

            if (_returningPhase)
            {
                Vector2 toOwner = _owner.GlobalPosition - GlobalPosition;
                if (toOwner.LengthSquared() < 400f) // approx 20 units
                {
                    QueueFree();
                    return;
                }
                _direction = toOwner.Normalized();
                Rotation = _direction.Angle();
            }

            Vector2 step = _direction * _speed * delta;
            Vector2 nextPosition = GlobalPosition + step;

            if (IsBlockedByWall(nextPosition) && !_returningPhase)
            {
                if (_ignoreWalls)
                {
                    // Do not block - pierce the wall/obstacle!
                }
                else if (_bouncesRemaining > 0)
                {
                    _bouncesRemaining--;

                    // Simple reflection based on axis
                    bool xBlocked = IsBlockedByWall(GlobalPosition + new Vector2(step.x, 0));
                    bool yBlocked = IsBlockedByWall(GlobalPosition + new Vector2(0, step.y));

                    if (xBlocked) _direction.x *= -1;
                    if (yBlocked) _direction.y *= -1;

                    // Corner case: if both or neither (diagonal corner), just flip both
                    if (!xBlocked && !yBlocked) { _direction = -_direction; }

                    _direction = _direction.Normalized();
                    Rotation = _direction.Angle();
                    return; // continue next frame
                }
                else
                {
                    if (_isReturning)
                    {
                        StartReturningPhase();
                        return;
                    }
                    BeginImpact(nextPosition, null);
                    return;
                }
            }

            Character hitTarget = FindHitTarget(nextPosition);
            if (hitTarget != null)
            {
                if (_bouncesRemaining > 0)
                {
                    ApplyDamage(hitTarget);
                    _bouncesRemaining--;

                    // Find next target
                    Character nextTarget = FindNextBounceTarget(hitTarget.GlobalPosition);
                    if (nextTarget != null)
                    {
                        _direction = (nextTarget.GlobalPosition - GlobalPosition).Normalized();
                        Rotation = _direction.Angle();
                        return; // continue next frame
                    }
                    else
                    {
                        // No target to bounce to, just impact
                        BeginImpact(hitTarget.GlobalPosition, hitTarget);
                        return;
                    }
                }
                else if (_isPiercing)
                {
                    ApplyDamage(hitTarget);
                }
                else
                {
                    BeginImpact(hitTarget.GlobalPosition, hitTarget);
                    return;
                }
            }

            GlobalPosition = nextPosition;

            if (!_returningPhase)
            {
                _traveled += step.Length();
                if (_traveled >= _maxDistance)
                {
                    if (_isReturning)
                    {
                        StartReturningPhase();
                    }
                    else
                    {
                        QueueFree();
                    }
                }
            }
        }

        private void StartReturningPhase()
        {
            _returningPhase = true;
            _damagedTargets.Clear();
            _isPiercing = true;
        }

        private bool IsBlockedByWall(Vector2 position)
        {
            if (_map == null || !Godot.Object.IsInstanceValid(_map))
            {
                return false;
            }

            var probe = new Rect2(position - new Vector2(4f, 4f), new Vector2(8f, 8f));
            return !_map.CanMoveTo(probe);
        }

        private Character FindHitTarget(Vector2 position)
        {
            if (!IsOwnerAlive())
            {
                return null;
            }

            foreach (Character enemy in EnumerateEnemyCharacters())
            {
                if (enemy?.Attributes?.HP == null || !enemy.Attributes.HP.IsAlive)
                {
                    continue;
                }

                if (_damagedTargets.Contains(enemy))
                {
                    continue;
                }

                if (enemy.GlobalPosition.DistanceTo(position) <= _hitRadius)
                {
                    return enemy;
                }
            }

            return null;
        }

        private Character FindNextBounceTarget(Vector2 currentPosition)
        {
            if (!IsOwnerAlive())
            {
                return null;
            }

            Character bestTarget = null;
            float closestDistSq = _bounceRange * _bounceRange;

            foreach (Character enemy in EnumerateEnemyCharacters())
            {
                if (enemy?.Attributes?.HP == null || !enemy.Attributes.HP.IsAlive)
                {
                    continue;
                }

                // Do not bounce back to already hit targets
                if (_damagedTargets.Contains(enemy))
                {
                    continue;
                }

                float distSq = enemy.GlobalPosition.DistanceSquaredTo(currentPosition);
                if (distSq <= closestDistSq)
                {
                    closestDistSq = distSq;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        private void BeginImpact(Vector2 impactPosition, Character directHitTarget)
        {
            GlobalPosition = impactPosition;
            _impacting = true;
            _impactTimer = 0f;
            _impactFrameIndex = 0;

            if (_isAoe)
            {
                DamageInRadius(impactPosition, _aoeRadius);
            }
            else if (directHitTarget != null)
            {
                ApplyDamage(directHitTarget);
            }

            if (_sprite != null && _impactFrames != null && _impactFrames.Length > 0)
            {
                _sprite.Texture = _impactFrames[0] ?? _sprite.Texture;
                _sprite.Scale = new Vector2(_impactScale, _impactScale);
                _sprite.Rotation = 0f;
            }
        }

        private void UpdateFlightAnimation(float delta)
        {
            if (_sprite == null || _flightFrames == null || _flightFrames.Length <= 1)
            {
                return;
            }

            float duration = _flightFrameDuration > 0f ? _flightFrameDuration : FireballFlightFrameDuration;
            _flightFrameTimer += delta;
            if (_flightFrameTimer < duration)
            {
                return;
            }

            _flightFrameTimer = 0f;
            int nextIndex = _flightFrameIndex + 1;
            if (nextIndex >= _flightFrames.Length)
            {
                if (_loopFlightAnimation)
                {
                    _flightFrameIndex = 0;
                }
                else
                {
                    _flightFrameIndex = _flightFrames.Length - 1; // Hold on the last frame
                }
            }
            else
            {
                _flightFrameIndex = nextIndex;
            }
            _sprite.Texture = _flightFrames[_flightFrameIndex] ?? _sprite.Texture;
        }

        private void UpdateImpact(float delta)
        {
            if (_impactFrames == null || _impactFrames.Length == 0)
            {
                QueueFree();
                return;
            }

            _impactTimer += delta;
            if (_impactTimer < _impactFrameDuration)
            {
                return;
            }

            _impactTimer = 0f;
            _impactFrameIndex += 1;
            if (_impactFrameIndex >= _impactFrames.Length)
            {
                QueueFree();
                return;
            }

            if (_sprite != null)
            {
                _sprite.Texture = _impactFrames[_impactFrameIndex] ?? _sprite.Texture;
            }
        }

        private void DamageInRadius(Vector2 center, float radius)
        {
            foreach (Character enemy in EnumerateEnemyCharacters())
            {
                if (enemy?.Attributes?.HP == null || !enemy.Attributes.HP.IsAlive)
                {
                    continue;
                }

                if (enemy.GlobalPosition.DistanceTo(center) <= radius)
                {
                    ApplyDamage(enemy);
                }
            }
        }

        private void ApplyDamage(Character target)
        {
            if (target == null || !Godot.Object.IsInstanceValid(target) || !IsOwnerAlive())
            {
                return;
            }

            if (_damagedTargets.Contains(target))
            {
                return;
            }

            // Use effective stats so Burn (ATK debuff) and Bleed (DEF debuff) are reflected
            int attackerAtk = _owner.Attributes?.EffectiveAtk ?? 1;
            int defenderDef = target.Attributes?.EffectiveDef ?? 0;
            int rolled = Mathf.RoundToInt((float)GD.RandRange(_damageMin, _damageMax));
            int damage = Mathf.Max(1, attackerAtk + rolled - Mathf.FloorToInt(defenderDef * 0.4f));

            target.TakeDamage(damage);
            _damagedTargets.Add(target);

            GD.Print($"[COMBAT] {_owner.EntityName} hit {target.EntityName} with projectile for {damage}. HP={target.Attributes.HP.CurrentHP}/{target.Attributes.HP.MaxHP}");

            // Apply on-hit status effect with configured probability
            if (_onHitEffect != null)
            {
                StatusEffectHelper.TryApplyWithChance(target, _onHitEffect, _onHitChance);
            }
        }

        private IEnumerable<Character> EnumerateEnemyCharacters()
        {
            if (!IsOwnerAlive())
            {
                yield break;
            }

            Node root = _owner?.GetTree()?.Root;
            if (root == null)
            {
                yield break;
            }

            var stack = new Stack<Node>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                Node node = stack.Pop();
                if (node is Character character && character != _owner)
                {
                    yield return character;
                }

                foreach (Node child in node.GetChildren())
                {
                    stack.Push(child);
                }
            }
        }

        private static Map FindMap(Player owner)
        {
            if (owner == null || !Godot.Object.IsInstanceValid(owner))
            {
                return null;
            }

            Node root = owner.GetTree()?.Root;
            if (root == null)
            {
                return null;
            }

            var stack = new Stack<Node>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                Node node = stack.Pop();
                if (node is Map map)
                {
                    return map;
                }

                foreach (Node child in node.GetChildren())
                {
                    stack.Push(child);
                }
            }

            return null;
        }

        private bool IsOwnerAlive()
        {
            return _owner != null && Godot.Object.IsInstanceValid(_owner);
        }
    }

    internal class TemporaryVisualEffectNode : Node2D
    {
        private readonly string _texturePath;
        private readonly float _duration;
        private readonly float _initialScale;
        private readonly float _targetScale;
        private readonly float _rotationSpeed;
        private readonly bool _flipH;
        private readonly float _initialRotation;

        private Sprite _sprite;
        private float _elapsed = 0f;

        public TemporaryVisualEffectNode(string texturePath, float duration, float initialScale, float targetScale = -1f, float rotationSpeed = 0f, bool flipH = false, float initialRotation = 0f)
        {
            _texturePath = texturePath;
            _duration = duration;
            _initialScale = initialScale;
            _targetScale = targetScale < 0f ? initialScale : targetScale;
            _rotationSpeed = rotationSpeed;
            _flipH = flipH;
            _initialRotation = initialRotation;
        }

        public override void _Ready()
        {
            Texture tex = GD.Load<Texture>(_texturePath);
            _sprite = new Sprite
            {
                Texture = tex,
                Centered = true,
                Scale = new Vector2(_initialScale, _initialScale),
                FlipH = _flipH
            };
            AddChild(_sprite);
            Rotation = _initialRotation;
            SetProcess(true);
        }

        public override void _Process(float delta)
        {
            _elapsed += delta;
            if (_elapsed >= _duration)
            {
                QueueFree();
                return;
            }

            // Lerp scale
            float t = _elapsed / _duration;
            float currentScale = Mathf.Lerp(_initialScale, _targetScale, t);
            _sprite.Scale = new Vector2(currentScale, currentScale);

            // Lerp opacity (fade out)
            float opacity = Mathf.Lerp(1f, 0f, t);
            _sprite.Modulate = new Color(1f, 1f, 1f, opacity);

            // Rotate
            if (_rotationSpeed != 0f)
            {
                Rotation += _rotationSpeed * delta;
            }
        }
    }

    internal class KnightExplosionNode : Node2D
    {
        private const float DURATION = 3.0f; // 3 seconds duration
        private const float INITIAL_SCALE = 0.25f;
        private const float TARGET_SCALE = 0.5f; // growing to 2x initial scale (0.25 * 2 = 0.5)
        private const float DAMAGE_TICK_INTERVAL = 0.5f; // damage tick every 0.5s
        private const float BASE_RADIUS = 160f; // base radius of explosion at scale 1.0

        private readonly Player _owner;
        private Sprite _sprite;
        private float _elapsed = 0f;
        private float _tickTimer = 0f;

        public KnightExplosionNode(Player owner)
        {
            _owner = owner;
        }

        public override void _Ready()
        {
            Texture tex = GD.Load<Texture>("res://Assets/SkillAnimation/knight_explose.png");
            _sprite = new Sprite
            {
                Texture = tex,
                Centered = true,
                Scale = new Vector2(INITIAL_SCALE, INITIAL_SCALE)
            };
            AddChild(_sprite);
            SetProcess(true);

            // Deal first tick immediately
            DealExplosionDamage();
        }

        public override void _Process(float delta)
        {
            if (_owner == null || !Godot.Object.IsInstanceValid(_owner))
            {
                QueueFree();
                return;
            }

            _elapsed += delta;
            if (_elapsed >= DURATION)
            {
                QueueFree();
                return;
            }

            // Lerp scale
            float t = _elapsed / DURATION;
            float currentScale = Mathf.Lerp(INITIAL_SCALE, TARGET_SCALE, t);
            _sprite.Scale = new Vector2(currentScale, currentScale);

            // Lerp opacity (fade out towards the end)
            float opacity = t > 0.8f ? Mathf.Lerp(1f, 0f, (t - 0.8f) / 0.2f) : 1f;
            _sprite.Modulate = new Color(1f, 1f, 1f, opacity);

            // Tick damage
            _tickTimer += delta;
            if (_tickTimer >= DAMAGE_TICK_INTERVAL)
            {
                _tickTimer -= DAMAGE_TICK_INTERVAL;
                DealExplosionDamage();
            }
        }

        private void DealExplosionDamage()
        {
            if (_owner == null || !Godot.Object.IsInstanceValid(_owner))
            {
                return;
            }

            float currentScale = _sprite.Scale.x;
            float currentRadius = BASE_RADIUS * currentScale;

            int attackerAtk = _owner.Attributes?.EffectiveAtk ?? 1;

            foreach (Character enemy in EnumerateEnemyCharacters())
            {
                if (enemy?.Attributes?.HP == null || !enemy.Attributes.HP.IsAlive)
                {
                    continue;
                }

                if (enemy.GlobalPosition.DistanceTo(GlobalPosition) <= currentRadius)
                {
                    int defenderDef = enemy.Attributes?.EffectiveDef ?? 0;
                    int baseDamage = Mathf.Max(1, attackerAtk - Mathf.FloorToInt(defenderDef * 0.4f));
                    // Roll smaller random damage per tick
                    int rolled = Mathf.RoundToInt((float)GD.RandRange(1, 3));
                    int damage = Mathf.Max(1, baseDamage + rolled);

                    enemy.TakeDamage(damage);
                    GD.Print($"[COMBAT] Knight Explosion ticked on {enemy.EntityName} for {damage} damage!");
                }
            }
        }

        private IEnumerable<Character> EnumerateEnemyCharacters()
        {
            Node root = _owner?.GetTree()?.Root;
            if (root == null)
            {
                yield break;
            }

            var stack = new Stack<Node>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                Node node = stack.Pop();
                if (node is Character character && character != _owner)
                {
                    yield return character;
                }

                foreach (Node child in node.GetChildren())
                {
                    stack.Push(child);
                }
            }
        }
    }
}