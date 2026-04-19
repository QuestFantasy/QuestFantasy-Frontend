using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Data.Skills
{
    internal static class SkillProjectileSpawner
    {
        public static void SpawnArrow(Player player, Character target, float maxRange)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateArrow(player, direction, maxRange);
            AttachToScene(player, node);
        }

        public static void SpawnFireball(Player player, Character target, float maxRange)
        {
            Vector2 direction = ResolveDirection(player, target);
            var node = SkillProjectileNode.CreateFireball(player, direction, maxRange);
            AttachToScene(player, node);
        }

        private static Vector2 ResolveDirection(Player player, Character target)
        {
            if (target != null)
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

        private static void AttachToScene(Player player, SkillProjectileNode node)
        {
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
        private Texture[] _impactFrames;
        private float _projectileScale = FireballProjectileScale;
        private float _impactScale = FireballImpactScale;

        private Sprite _sprite;
        private bool _impacting;
        private float _impactTimer;
        private int _impactFrameIndex;

        public static SkillProjectileNode CreateArrow(Player owner, Vector2 direction, float maxRange)
        {
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
                _projectileTexture = GD.Load<Texture>("res://Assets/SkillAnimation/arrow.png"),
                _projectileScale = ArrowProjectileScale,
                _impactScale = ArrowImpactScale,
                _impactFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/arrow.png"),
                }
            };
        }

        public static SkillProjectileNode CreateFireball(Player owner, Vector2 direction, float maxRange)
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
                _projectileTexture = GD.Load<Texture>("res://Assets/SkillAnimation/fireball.png"),
                _projectileScale = FireballProjectileScale,
                _impactScale = FireballImpactScale,
                _impactFrames = new[]
                {
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit1.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit2.png"),
                    GD.Load<Texture>("res://Assets/SkillAnimation/fireball_hit3.png"),
                }
            };
        }

        public override void _Ready()
        {
            _sprite = new Sprite
            {
                Texture = _projectileTexture,
                Centered = true,
                Scale = new Vector2(_projectileScale, _projectileScale),
            };
            AddChild(_sprite);

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
            if (_impacting)
            {
                UpdateImpact(delta);
                return;
            }

            Vector2 step = _direction * _speed * delta;
            Vector2 nextPosition = GlobalPosition + step;

            if (IsBlockedByWall(nextPosition))
            {
                BeginImpact(nextPosition, null);
                return;
            }

            Character hitTarget = FindHitTarget(nextPosition);
            if (hitTarget != null)
            {
                BeginImpact(hitTarget.GlobalPosition, hitTarget);
                return;
            }

            GlobalPosition = nextPosition;
            _traveled += step.Length();
            if (_traveled >= _maxDistance)
            {
                QueueFree();
            }
        }

        private bool IsBlockedByWall(Vector2 position)
        {
            if (_map == null)
            {
                return false;
            }

            var probe = new Rect2(position - new Vector2(4f, 4f), new Vector2(8f, 8f));
            return !_map.CanMoveTo(probe);
        }

        private Character FindHitTarget(Vector2 position)
        {
            if (_owner == null)
            {
                return null;
            }

            foreach (Character enemy in EnumerateEnemyCharacters())
            {
                if (enemy?.Attributes?.HP == null || !enemy.Attributes.HP.IsAlive)
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

        private void UpdateImpact(float delta)
        {
            if (_impactFrames == null || _impactFrames.Length == 0)
            {
                QueueFree();
                return;
            }

            _impactTimer += delta;
            if (_impactTimer < 0.05f)
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
            if (target == null || _owner == null)
            {
                return;
            }

            if (_damagedTargets.Contains(target))
            {
                return;
            }

            int attackerAtk = _owner.Attributes?.TotalAtk ?? 1;
            int defenderDef = target.Attributes?.TotalDef ?? 0;
            int rolled = Mathf.RoundToInt((float)GD.RandRange(_damageMin, _damageMax));
            int damage = Mathf.Max(1, attackerAtk + rolled - Mathf.FloorToInt(defenderDef * 0.4f));

            target.TakeDamage(damage);
            _damagedTargets.Add(target);

            GD.Print($"[COMBAT] {_owner.EntityName} hit {target.EntityName} with projectile for {damage}. HP={target.Attributes.HP.CurrentHP}/{target.Attributes.HP.MaxHP}");
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

        private static Map FindMap(Player owner)
        {
            if (owner == null)
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
    }
}
