using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Items;

namespace QuestFantasy.Characters
{
    /// <summary>
    /// Monster character class. Represents NPCs that can be fought.
    /// Handles monster-specific attribute calculations and behavior.
    /// </summary>
    public class Monster : Character
    {
        [Export]
        public int MinDrops = 0;

        [Export]
        public int MaxDrops = 2;

        [Export]
        public int DropOptionCount = 3; // how many options to consider when generating drops

        [Export]
        public int DropLevelOffset = 1;
        public int ExperienceReward { get; set; }
        public int LootGoldReward { get; set; }

        public Vector2 Velocity { get; private set; }

        private Map _map;
        private Player _player;

        private List<Vector2> _currentPath = new List<Vector2>();
        private readonly Random _random = new Random();
        private static readonly List<Vector2> _occupiedSpawnPositions = new List<Vector2>();
        private const float SpawnPositionTolerance = 1.0f;
        private static readonly List<Monster> _activeMonsters = new List<Monster>();

        // Anti-stuck system
        private Vector2 _lastPosition;
        private float _stuckTime;
        private Vector2 _lastTargetTile = new Vector2(-999, -999);
        private float _repathCooldown = 0f;
        private const float RepathInterval = 1.2f;

        // Optimization: Frame slicing for pathfinding
        private static ulong _lastPathfindingFrame = 0;
        private static int _pathfindingsThisFrame = 0;
        private const int MaxPathsPerFrame = 2;

        // Animation logic
        private Texture _standTexture;
        private Texture _walkTexture;
        private Texture _attackTexture1;
        private Texture _attackTexture2;
        private float _animationTimer = 0f;
        private const float AnimationInterval = 0.2f;
        private bool _isWalkFrame = false;

        // Attack logic
        private bool _isAttacking = false;
        private float _attackTimer = 0f;
        private float _attackCooldownTimer = 0f;
        private const float AttackDuration = 0.5f;
        private const float AttackCooldown = 1.5f;
        private const float AttackRange = 40.0f;

        // Death state
        private Texture _deadTexture;
        private bool _isDead = false;
        private float _deathTimer = 2.0f;

        // Hit state
        private Texture _hitTexture;
        private bool _isHit = false;
        private float _hitTimer = 0f;

        // Health Bar
        private ProgressBar _healthBar;

        public Vector2 BodySizeInTiles = new Vector2(0.1f, 0.1f);

        public void SetEnvironment(Map map, Player player)
        {
            _map = map;
            _player = player;
            FindSafeSpawnLocation();
            _repathCooldown = (float)_random.NextDouble() * 1.0f;
        }

        private bool IsSpawnPositionOccupied(Vector2 position)
        {
            foreach (Vector2 occupied in _occupiedSpawnPositions)
            {
                if (occupied.DistanceTo(position) <= SpawnPositionTolerance)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsPlayerSpawnPosition(Vector2 position)
        {
            if (_player == null) return false;
            return position.DistanceTo(_player.GlobalPosition) <= _map.TileSize * 3.5f;  // Keep 3+ tiles away
        }

        private bool IsTileOccupiedByOtherMonster(int x, int y)
        {
            foreach (Monster monster in _activeMonsters)
            {
                if (monster == null || monster == this) continue;

                Vector2 otherTile = WorldToTile(monster.GlobalPosition);
                if ((int)otherTile.x == x && (int)otherTile.y == y)
                {
                    return true;
                }
            }
            return false;
        }

        private void FindSafeSpawnLocation()
        {
            int maxAttempts = 200;
            for (int i = 0; i < maxAttempts; i++)
            {
                int tx = _random.Next(0, _map.WorldTileWidth);
                int ty = _random.Next(0, _map.WorldTileHeight);

                Vector2 checkPos = TileToWorldCenter(new Vector2(tx, ty));

                if (!_map.CanMoveTo(GetBodyRect(checkPos))) continue;
                if (IsPlayerSpawnPosition(checkPos)) continue;
                if (IsSpawnPositionOccupied(checkPos)) continue;
                if (IsTileOccupiedByOtherMonster(tx, ty)) continue;

                Position = checkPos;
                _occupiedSpawnPositions.Add(checkPos);
                return;
            }

            Vector2 spawnCenter = _map.GetSpawnWorldPosition();
            if (!IsPlayerSpawnPosition(spawnCenter))
            {
                Position = spawnCenter;
                _occupiedSpawnPositions.Add(spawnCenter);
            }
        }

        public override void _Ready()
        {
            // From main branch
            InitializeCharacter();

            if (ExperienceReward <= 0) ExperienceReward = 10;
            if (LootGoldReward <= 0) LootGoldReward = 5;

            // From feature branch
            SetPhysicsProcess(true);
            _lastPosition = GlobalPosition;
            if (!_activeMonsters.Contains(this))
            {
                _activeMonsters.Add(this);
            }

            _standTexture = GD.Load<Texture>("res://Assets/Monster/slime_stand.png");
            _walkTexture = GD.Load<Texture>("res://Assets/Monster/slime_walk.png");
            _attackTexture1 = GD.Load<Texture>("res://Assets/Monster/slime_attack.png");
            _attackTexture2 = GD.Load<Texture>("res://Assets/Monster/slime_attack1.png");
            _deadTexture = GD.Load<Texture>("res://Assets/Monster/slime_knockdown.png");
            _hitTexture = GD.Load<Texture>("res://Assets/Monster/slime_hit.png");
            Texture = _standTexture;

            if (Attributes != null)
            {
                Attributes.TotalAtk = 1;
                Attributes.HP.SetMaxHPAndCurrentHP(5);
            }

            // Add HP bar
            _healthBar = new ProgressBar
            {
                RectSize = new Vector2(96, 4),
                RectPosition = new Vector2(-40, -70),
                PercentVisible = false
            };
            var bgStyle = new StyleBoxFlat { BgColor = new Color(0.5f, 0.5f, 0.5f, 1f) };
            var fgStyle = new StyleBoxFlat { BgColor = new Color(0.9f, 0.1f, 0.1f, 1f) };
            _healthBar.AddStyleboxOverride("bg", bgStyle);
            _healthBar.AddStyleboxOverride("fg", fgStyle);
            AddChild(_healthBar);

            GD.Print($"Monster ready at {GlobalPosition}");
        }

        public override void _ExitTree()
        {
            _activeMonsters.Remove(this);
            base._ExitTree();
        }

        public override void UpdateAttributes()
        {
            if (Attributes == null || Abilities == null)
            {
                GD.PrintErr($"[Monster] {EntityName}: Attributes or Abilities not initialized");
                return;
            }

            // Fixed for current assignment spec
            Attributes.TotalAtk = 1;
            Attributes.TotalDef = 0;
        }

        public override void TakeDamage(int damage)
        {
            base.TakeDamage(damage);
            if (!_isDead && Attributes?.HP != null && Attributes.HP.IsAlive)
            {
                _isHit = true;
                _hitTimer = 0.2f;
                Texture = _hitTexture;
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_isDead)
            {
                _deathTimer -= delta;
                if (_deathTimer <= 0)
                {
                    QueueFree();
                }
                return;
            }

            if (Attributes != null && Attributes.HP != null && _healthBar != null)
            {
                _healthBar.MaxValue = Attributes.HP.MaxHP;
                _healthBar.Value = Attributes.HP.CurrentHP;
            }

            if (Attributes != null && Attributes.HP != null && !Attributes.HP.IsAlive)
            {
                Die();
                return;
            }

            if (_isHit)
            {
                _hitTimer -= delta;
                if (_hitTimer <= 0)
                {
                    _isHit = false;
                }
                else
                {
                    Texture = _hitTexture;
                    return;
                }
            }

            if (_map == null || _player == null) return;

            float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
            if (distanceToPlayer > 450f)
            {
                Velocity = Vector2.Zero;
                if (_isWalkFrame)
                {
                    _isWalkFrame = false;
                    Texture = _standTexture;
                }
                return;
            }

            if (_attackCooldownTimer > 0f)
            {
                _attackCooldownTimer -= delta;
            }

            if (_isAttacking)
            {
                _attackTimer -= delta;
                UpdateAttackAnimation(delta);

                if (_attackTimer <= 0f)
                {
                    _isAttacking = false;
                }
                return; // Skip moving while attacking
            }

            if (distanceToPlayer <= AttackRange && _attackCooldownTimer <= 0f)
            {
                PerformAttack();
                return;
            }

            if (_repathCooldown > 0f)
            {
                _repathCooldown -= delta;
            }

            CheckPathflowAndStuck(delta);
            MoveProcess(delta, distanceToPlayer);
            UpdateAnimation(delta);
        }

        private void PerformAttack()
        {
            _isAttacking = true;
            _attackTimer = AttackDuration;
            _attackCooldownTimer = AttackCooldown;
            Velocity = Vector2.Zero; // Stop moving to attack
            Attack();
        }

        private void UpdateAttackAnimation(float delta)
        {
            if (_attackTimer > AttackDuration / 2f)
            {
                Texture = _attackTexture1;
            }
            else
            {
                Texture = _attackTexture2;
            }

            // Face the player
            if (_player.GlobalPosition.x < GlobalPosition.x) FlipH = true;
            else if (_player.GlobalPosition.x > GlobalPosition.x) FlipH = false;
        }

        private void UpdateAnimation(float delta)
        {
            if (Velocity.LengthSquared() > 0.1f)
            {
                _animationTimer += delta;
                if (_animationTimer >= AnimationInterval)
                {
                    _animationTimer = 0f;
                    _isWalkFrame = !_isWalkFrame;
                    Texture = _isWalkFrame ? _walkTexture : _standTexture;
                }

                if (Velocity.x < 0) FlipH = true;
                else if (Velocity.x > 0) FlipH = false;
            }
            else
            {
                _animationTimer = 0f;
                _isWalkFrame = false;
                Texture = _standTexture;
            }
        }

        private void CheckPathflowAndStuck(float delta)
        {
            float distMoved = _lastPosition.DistanceTo(GlobalPosition);

            if (distMoved < 0.5f) _stuckTime += delta;
            else _stuckTime = 0;

            _lastPosition = GlobalPosition;

            Vector2 targetTile = WorldToTile(_player.Position);
            bool targetMoved = targetTile != _lastTargetTile;
            bool isStuck = _stuckTime > 1.0f;

            if ((targetMoved || isStuck) && _repathCooldown <= 0f)
            {
                ulong currentFrame = Engine.GetPhysicsFrames();
                if (currentFrame != _lastPathfindingFrame)
                {
                    _lastPathfindingFrame = currentFrame;
                    _pathfindingsThisFrame = 0;
                }

                if (_pathfindingsThisFrame < MaxPathsPerFrame)
                {
                    _pathfindingsThisFrame++;
                    if (isStuck) _stuckTime = 0f;

                    _lastTargetTile = targetTile;
                    RecomputePath();
                    _repathCooldown = RepathInterval + (float)_random.NextDouble() * 0.2f;
                }
                else
                {
                    _repathCooldown = 0.2f + (float)_random.NextDouble() * 0.3f;
                }
            }
        }

        private void RecomputePath()
        {
            if (_map == null || _player == null) return;
            Vector2 startTile = WorldToTile(GlobalPosition);
            Vector2 targetTile = WorldToTile(_player.Position);

            if (startTile == targetTile)
            {
                _currentPath.Clear();
                return;
            }

            _currentPath = FindAStarPath(startTile, targetTile, inflateObstacles: true);
            if (_currentPath.Count == 0)
            {
                _currentPath = FindAStarPath(startTile, targetTile, inflateObstacles: false);
            }
        }

        private void MoveProcess(float delta, float distanceToPlayer)
        {
            if (_currentPath.Count == 0)
            {
                Velocity = Vector2.Zero;
                return;
            }

            Vector2 nextWaypoint = _currentPath[0];
            float dist = GlobalPosition.DistanceTo(nextWaypoint);

            if (dist < 12.0f)
            {
                _currentPath.RemoveAt(0);
                if (_currentPath.Count == 0) return;
                nextWaypoint = _currentPath[0];
            }

            float speedMultiplier = distanceToPlayer > 200f ? 1.5f : (distanceToPlayer > 80f ? 1.0f : 0.8f);

            // In case Player doesnt have MoveSpeed, hardcode fallback to 100f. Assuming it might have been refactored in main.
            float speed = 100f * speedMultiplier;

            Vector2 direction = (nextWaypoint - GlobalPosition).Normalized();
            Velocity = direction * speed;

            MoveAndSlide(distanceToPlayer);
        }

        private void MoveAndSlide(float distanceToPlayer)
        {
            Vector2 deltaMove = Velocity * GetPhysicsProcessDeltaTime();
            Vector2 newPos = GlobalPosition + deltaMove;
            float minDistance = 24.0f; // Approx 1 tile minimal distance

            // Anti-overlap with player
            if (_player != null && _player.Attributes?.HP?.IsAlive == true)
            {
                if (Math.Abs(newPos.x - _player.GlobalPosition.x) < minDistance &&
                    Math.Abs(newPos.y - _player.GlobalPosition.y) < minDistance)
                {
                    if (newPos.DistanceTo(_player.GlobalPosition) < minDistance)
                    {
                        Vector2 pushDir = (newPos - _player.GlobalPosition).Normalized();
                        newPos = _player.GlobalPosition + pushDir * minDistance;
                    }
                }
            }

            // Anti-overlap with other monsters
            if (distanceToPlayer < 400f)
            {
                int checks = 0;
                foreach (var monster in _activeMonsters)
                {
                    if (monster == null || monster == this || monster.Attributes?.HP?.IsAlive != true) continue;

                    if (Math.Abs(newPos.x - monster.GlobalPosition.x) > minDistance ||
                        Math.Abs(newPos.y - monster.GlobalPosition.y) > minDistance)
                        continue;

                    if (newPos.DistanceTo(monster.GlobalPosition) < minDistance)
                    {
                        Vector2 pushDir = (newPos - monster.GlobalPosition).Normalized();
                        if (pushDir.LengthSquared() == 0) pushDir = new Vector2(1, 0); // fallback
                        newPos = monster.GlobalPosition + pushDir * minDistance;

                        checks++;
                        if (checks > 5) break;
                    }
                }
            }

            // Check wall collision if we pushed
            if (_map != null && !_map.CanMoveTo(GetBodyRect(newPos)))
            {
                // If push implies wall breach, ignore push if natural movement is fine
                if (_map.CanMoveTo(GetBodyRect(GlobalPosition + deltaMove)))
                {
                    newPos = GlobalPosition + deltaMove;
                }
                else
                {
                    newPos = GlobalPosition; // Completely stuck
                }
            }

            GlobalPosition = newPos;
        }

        private Rect2 GetBodyRect(Vector2 centerPosition)
        {
            Vector2 bodySize = new Vector2(0.1f, 0.1f);
            return new Rect2(centerPosition - bodySize / 2f, bodySize);
        }

        private bool IsWalkableInflated(int x, int y)
        {
            if (!_map.IsWalkableTile(x, y)) return false;
            return true;
        }

        private List<Vector2> FindAStarPath(Vector2 startVec, Vector2 goalVec, bool inflateObstacles)
        {
            (int x, int y) start = (Mathf.RoundToInt(startVec.x), Mathf.RoundToInt(startVec.y));
            (int x, int y) goal = (Mathf.RoundToInt(goalVec.x), Mathf.RoundToInt(goalVec.y));

            var openSet = new List<(int x, int y)> { start };
            var closedSet = new HashSet<(int x, int y)>();
            var cameFrom = new Dictionary<(int x, int y), (int x, int y)>();

            var gScore = new Dictionary<(int x, int y), float> { [start] = 0 };
            var fScore = new Dictionary<(int x, int y), float> { [start] = ManhattanDistance(start, goal) };

            int maxIterations = 500;
            int iterations = 0;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                (int x, int y) current = openSet[0];
                foreach (var node in openSet)
                {
                    float currentF = fScore.ContainsKey(current) ? fScore[current] : float.MaxValue;
                    float nodeF = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
                    if (nodeF < currentF) current = node;
                }

                if (current.x == goal.x && current.y == goal.y)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                var neighbors = new (int x, int y)[] {
                    (current.x + 1, current.y),
                    (current.x - 1, current.y),
                    (current.x, current.y + 1),
                    (current.x, current.y - 1)
                };

                foreach (var neighbor in neighbors)
                {
                    if (closedSet.Contains(neighbor)) continue;

                    bool isSafe = (neighbor.x == goal.x && neighbor.y == goal.y) ? _map.IsWalkableTile(neighbor.x, neighbor.y) :
                        (inflateObstacles ? IsWalkableInflated(neighbor.x, neighbor.y) : _map.IsWalkableTile(neighbor.x, neighbor.y));

                    if (!isSafe) continue;

                    if (!(neighbor.x == goal.x && neighbor.y == goal.y) && IsTileOccupiedByOtherMonster(neighbor.x, neighbor.y))
                    {
                        continue;
                    }

                    float tentative_gScore = (gScore.ContainsKey(current) ? gScore[current] : float.MaxValue) + 1;
                    float neighbor_gScore = gScore.ContainsKey(neighbor) ? gScore[neighbor] : float.MaxValue;

                    if (tentative_gScore < neighbor_gScore)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentative_gScore;
                        fScore[neighbor] = gScore[neighbor] + ManhattanDistance(neighbor, goal);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }
            return new List<Vector2>();
        }

        private float ManhattanDistance((int x, int y) a, (int x, int y) b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        private List<Vector2> ReconstructPath(Dictionary<(int x, int y), (int x, int y)> cameFrom, (int x, int y) current)
        {
            var path = new List<Vector2>();
            while (cameFrom.ContainsKey(current))
            {
                path.Add(TileToWorldCenter(new Vector2(current.x, current.y)));
                current = cameFrom[current];
            }
            path.Reverse();
            return path;
        }

        private Vector2 WorldToTile(Vector2 worldPos)
        {
            int tX = Mathf.Clamp(Mathf.FloorToInt((worldPos.x - _map.GlobalPosition.x) / _map.TileSize), 0, _map.WorldTileWidth - 1);
            int tY = Mathf.Clamp(Mathf.FloorToInt((worldPos.y - _map.GlobalPosition.y) / _map.TileSize), 0, _map.WorldTileHeight - 1);
            return new Vector2(tX, tY);
        }

        private Vector2 TileToWorldCenter(Vector2 tile)
        {
            float gX = _map.GlobalPosition.x + tile.x * _map.TileSize + (_map.TileSize / 2f);
            float gY = _map.GlobalPosition.y + tile.y * _map.TileSize + (_map.TileSize / 2f);
            return new Vector2(gX, gY);
        }

        private void Die()
        {
            _isDead = true;
            Texture = _deadTexture;
            Velocity = Vector2.Zero;
            if (_healthBar != null) _healthBar.Visible = false;
            GD.Print($"[Monster] {EntityName} Died");
            TrySpawnDrops();
        }

        private void TrySpawnDrops()
        {
            var expPickup = new ExpPickup();
            expPickup.SetPlayer(_player);
            expPickup.Position = GlobalPosition;
            Node parent = GetParent() ?? GetTree().Root;
            parent.AddChild(expPickup);
            GD.PrintS($"[Monster] Spawned EXP drop at {expPickup.Position}");

            // Find EquipmentManager in the scene
            var manager = FindEquipmentManager();
            if (manager == null)
            {
                GD.PrintS("[Monster] No EquipmentManager found; skipping drops.");
                return;
            }

            var rng = new RandomNumberGenerator();
            rng.Randomize();
            int minD = Math.Max(0, MinDrops);
            int maxD = Math.Max(minD, MaxDrops);
            int drops = rng.RandiRange(minD, maxD);
            if (drops <= 0)
            {
                return;
            }

            int playerLevel = _player != null ? (int)_player.Level : (int)Level;
            var options = manager.GetEquipmentSet(DropOptionCount, playerLevel, DropLevelOffset);
            var optList = new System.Collections.Generic.List<Item>();
            foreach (var o in options)
            {
                if (o is Item it) optList.Add(it);
            }

            if (optList.Count == 0)
            {
                GD.PrintS("[Monster] No equipment options available for drops.");
                return;
            }

            // Shuffle
            var shuffled = new System.Collections.Generic.List<Item>(optList);
            for (int s = shuffled.Count - 1; s > 0; s--)
            {
                int j = (int)rng.RandiRange(0, s);
                var tmp = shuffled[s];
                shuffled[s] = shuffled[j];
                shuffled[j] = tmp;
            }

            int take = Math.Min(drops, shuffled.Count);
            for (int i = 0; i < take; i++)
            {
                var it = shuffled[i];
                if (it == null) continue;

                var pickup = new EquipmentPickup();
                pickup.ItemData = it;
                // Try to use manager configured pickup scale
                pickup.SpriteScale = manager.PickupSpriteScale;
                var offset = new Vector2(rng.Randf() * 120f - 60f, rng.Randf() * 120f - 60f);
                pickup.Position = GlobalPosition + offset;

                string baseName = "equipment";
                var spriteTex = (it is Equipment pe) ? pe.Sprite : (it is Weapon pw ? pw.Sprite : null);
                if (spriteTex != null)
                {
                    var rp = spriteTex.ResourcePath;
                    if (!string.IsNullOrEmpty(rp))
                    {
                        baseName = System.IO.Path.GetFileNameWithoutExtension(rp).Replace(' ', '_');
                    }
                }
                pickup.Name = $"Pickup_{baseName}_monster_{i}";
                parent.AddChild(pickup);
                GD.PrintS($"[Monster] Spawned drop: {pickup.Name} at {pickup.Position}");
            }
        }

        private EquipmentManager FindEquipmentManager()
        {
            var root = GetTree().Root;
            return FindEquipmentManagerRecursive(root);
        }

        private EquipmentManager FindEquipmentManagerRecursive(Node node)
        {
            if (node is EquipmentManager em) return em;
            foreach (Node child in node.GetChildren())
            {
                var found = FindEquipmentManagerRecursive(child);
                if (found != null) return found;
            }
            return null;
        }

        public override void Attack()
        {
            GD.Print($"Monster {EntityName} attacks with {Attributes?.TotalAtk} ATK!");
            if (_player != null && _player.Attributes?.HP != null)
            {
                _player.TakeDamage(Attributes?.TotalAtk ?? 1);
                GD.Print($"[COMBAT] {EntityName} attacks Player for {Attributes?.TotalAtk ?? 1} damage! Player HP: {_player.Attributes.HP.CurrentHP}/{_player.Attributes.HP.MaxHP}");
            }
        }
    }
}