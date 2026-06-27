using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Items;
using QuestFantasy.Core.Systems.StatusEffects;

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

        [Export]
        public float ItemDropChance = 0.5f;

        [Export]
        public int BaseHp = 40;

        [Export]
        public int HpPerLevel = 8;

        [Export]
        public int BaseAttack = 4;

        [Export]
        public float AttackPerLevel = 0.5f;

        [Export]
        public int BaseDefense = 0;

        [Export]
        public float DefensePerTenLevels = 0.5f;

        public int ExperienceReward { get; set; }
        public int LootGoldReward { get; set; }

        public Vector2 Velocity { get; protected set; }

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
        protected Texture _standTexture;
        protected Texture _walkTexture;
        protected Texture _attackTexture1;
        protected Texture _attackTexture2;
        private float _animationTimer = 0f;
        private const float AnimationInterval = 0.2f;
        private bool _isWalkFrame = false;

        // Attack logic
        private bool _isAttacking = false;
        private float _attackTimer = 0f;
        private float _attackCooldownTimer = 0f;
        private const float AttackDuration = 0.5f;
        private const float AttackCooldown = 1.5f;
        protected const float BaseMoveSpeed = 100f;
        protected const float AttackRange = 40.0f;

        // Death state
        protected Texture _deadTexture;
        private bool _isDead = false;
        private float _deathTimer = 2.0f;

        // Hit state
        protected Texture _hitTexture;
        private bool _isHit = false;
        private float _hitTimer = 0f;

        // Health Bar
        private ProgressBar _healthBar;

        public Vector2 BodySizeInTiles = new Vector2(0.1f, 0.1f);

        protected Map CurrentMap => _map;
        protected Player TargetPlayer => _player;

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

            LoadTextures();
            InitializeMonsterAttributes();

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

            int referenceLevel = GetReferenceLevel();
            Attributes.TotalAtk = GetScaledAttack(referenceLevel);
            Attributes.TotalDef = GetScaledDefense(referenceLevel);
        }

        public override void TakeDamage(int damage, Character source = null)
        {
            base.TakeDamage(damage, source);
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

            // Tick all active status effects (Burn, Bleed, Stun, etc.)
            EffectManager?.Update(this, delta);

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

            // Stun: completely freeze the monster (no movement, attack, or animation)
            if (IsStunned)
            {
                Modulate = EffectManager?.GetModulateColor() ?? new Color(1f, 1f, 1f, 1f);
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
                    // Keep effect color even during hit flash
                    if (EffectManager != null && EffectManager.HasAnyEffect)
                        Modulate = EffectManager.GetModulateColor();
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
                // Apply effect color even when idle (out of range)
                Modulate = EffectManager?.GetModulateColor() ?? new Color(1f, 1f, 1f, 1f);
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
                Modulate = EffectManager?.GetModulateColor() ?? new Color(1f, 1f, 1f, 1f);
                return; // Skip moving while attacking
            }

            if (TryHandleSpecialBehavior(delta, distanceToPlayer))
            {
                return;
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

            // Apply status effect color overlay (white = no effect)
            Modulate = EffectManager?.GetModulateColor() ?? new Color(1f, 1f, 1f, 1f);
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
            float speed = BaseMoveSpeed * speedMultiplier;

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

        protected virtual void LoadTextures()
        {
            _standTexture = GD.Load<Texture>("res://Assets/Monster/slime_stand.png");
            _walkTexture = GD.Load<Texture>("res://Assets/Monster/slime_walk.png");
            _attackTexture1 = GD.Load<Texture>("res://Assets/Monster/slime_attack.png");
            _attackTexture2 = GD.Load<Texture>("res://Assets/Monster/slime_attack1.png");
            _deadTexture = GD.Load<Texture>("res://Assets/Monster/slime_knockdown.png");
            _hitTexture = GD.Load<Texture>("res://Assets/Monster/slime_hit.png");
            Texture = _standTexture;
        }

        protected virtual void InitializeMonsterAttributes()
        {
            if (Attributes != null)
            {
                int referenceLevel = GetReferenceLevel();
                Level = referenceLevel;
                Attributes.TotalAtk = GetScaledAttack(referenceLevel);
                Attributes.TotalDef = GetScaledDefense(referenceLevel);
                int hp = GetScaledHp(referenceLevel);
                Attributes.TotalVit = Mathf.Max(1, Mathf.CeilToInt(hp / 10f));
                Attributes.HP.SetMaxHPAndCurrentHP(hp);
            }
        }

        protected virtual float HpMultiplier
        {
            get { return 1f; }
        }

        protected virtual float AttackMultiplier
        {
            get { return 1f; }
        }

        private float GetDifficultyMultiplier()
        {
            DifficultyLevel mapDiff = _map != null ? _map.Difficulty : DifficultyLevel.Normal;
            switch (mapDiff)
            {
                case DifficultyLevel.Easy: return 0.5f;
                case DifficultyLevel.Normal: return 1.0f;
                case DifficultyLevel.Hard: return 3.0f;
                case DifficultyLevel.Nightmare: return 10.0f;
                default: return 1.0f;
            }
        }

        private int GetReferenceLevel()
        {
            int playerLevel = _player != null ? (int)_player.Level : (int)Level;
            return Mathf.Max(1, playerLevel);
        }

        private int GetScaledHp(int referenceLevel)
        {
            int baseHp = Math.Max(1, BaseHp);
            int hpPerLevel = Math.Max(0, HpPerLevel);
            return Mathf.Max(1, Mathf.RoundToInt((baseHp + referenceLevel * hpPerLevel) * HpMultiplier * GetDifficultyMultiplier()));
        }

        private int GetScaledAttack(int referenceLevel)
        {
            int baseAttack = Math.Max(1, BaseAttack);
            float attackPerLevel = Math.Max(0, AttackPerLevel);
            return Mathf.Max(1, Mathf.RoundToInt((baseAttack + referenceLevel * attackPerLevel) * AttackMultiplier * GetDifficultyMultiplier()));
        }

        private int GetScaledDefense(int referenceLevel)
        {
            float baseDefense = Math.Max(0, BaseDefense) + Math.Max(0, referenceLevel / 10) * Math.Max(0, DefensePerTenLevels);
            return Mathf.RoundToInt(baseDefense * GetDifficultyMultiplier());
        }

        protected virtual bool TryHandleSpecialBehavior(float delta, float distanceToPlayer)
        {
            return false;
        }

        protected Color GetEffectModulate()
        {
            return EffectManager?.GetModulateColor() ?? new Color(1f, 1f, 1f, 1f);
        }

        protected Rect2 GetBodyRect(Vector2 centerPosition)
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

            DifficultyLevel mapDiff = _map != null ? _map.Difficulty : DifficultyLevel.Normal;
            int playerLevel = _player != null ? (int)_player.Level : (int)Level;
            var manager = FindEquipmentManager();

            if (Main.Instance != null)
            {
                string token = Main.Instance.GetAuthToken();
                if (Main.Instance.PlayerDataApiClient != null && !string.IsNullOrEmpty(token))
                {
                    Main.Instance.PlayerDataApiClient.GenerateDrops(token, playerLevel, "monster", mapDiff.ToString(), result => {
                        if (result.NetworkOk && result.ResponseCode == 200 && result.ArrayData != null)
                        {
                            SpawnServerDrops(parent, GlobalPosition, manager, result.ArrayData);
                        }
                        else
                        {
                            GD.PrintErr("[Monster] Failed to generate secure drops from server. Falling back to local generation...");
                            SpawnLocalDrops(parent, GlobalPosition, manager, mapDiff);
                        }
                    });
                    return;
                }
            }

            SpawnLocalDrops(parent, GlobalPosition, manager, mapDiff);
        }

        private void SpawnServerDrops(Node parent, Vector2 centerPosition, EquipmentManager manager, Godot.Collections.Array drops)
        {
            var rng = new RandomNumberGenerator();
            rng.Randomize();

            for (int i = 0; i < drops.Count; i++)
            {
                if (!(drops[i] is Godot.Collections.Dictionary drop))
                {
                    continue;
                }

                string instanceId = drop.Contains("instance_id") ? drop["instance_id"]?.ToString() : string.Empty;
                string itemType = drop.Contains("item_type") ? drop["item_type"]?.ToString() : string.Empty;

                if (itemType == "gold")
                {
                    int goldAmount = drop.Contains("gold_amount") ? Convert.ToInt32(drop["gold_amount"]) : 0;
                    var coinDrop = new QuestFantasy.Items.CoinDrop();
                    coinDrop.InitializeSecure(instanceId, goldAmount, _player);
                    coinDrop.Position = centerPosition + new Vector2(rng.Randf() * 40f - 20f, rng.Randf() * 40f - 20f);
                    parent.AddChild(coinDrop);
                    GD.PrintS($"[Monster] Spawned Secure Coin drop of value {goldAmount} at {coinDrop.Position}");
                }
                else if (drop.Contains("item_data") && drop["item_data"] is Godot.Collections.Dictionary itemDataDict)
                {
                    itemDataDict["instance_id"] = instanceId;
                    Item item = PlayerItemSnapshotCodec.Decode(itemDataDict);
                    if (item != null)
                    {
                        float pscale = manager != null ? manager.PickupSpriteScale : 0.5f;
                        var itemPos = centerPosition + new Vector2(rng.Randf() * 100f - 50f, rng.Randf() * 100f - 50f);
                        LootItemFactory.SpawnPickup(parent, item, itemPos, pscale, "secure_monster");
                        GD.PrintS($"[Monster] Spawned secure pickup: {item.Name} at {itemPos}");
                    }
                }
            }
        }

        private void SpawnLocalDrops(Node parent, Vector2 centerPosition, EquipmentManager manager, DifficultyLevel mapDiff)
        {
            var coinDrop = new QuestFantasy.Items.CoinDrop();
            int pLevel = _player != null ? (int)_player.Level : (int)Level;
            coinDrop.Initialize(pLevel, mapDiff, 0.3f, _player);
            coinDrop.Position = centerPosition + new Vector2((float)_random.NextDouble() * 40f - 20f, (float)_random.NextDouble() * 40f - 20f);
            parent.AddChild(coinDrop);
            GD.PrintS($"[Monster] Spawned Coin drop at {coinDrop.Position}");

            var rng = new RandomNumberGenerator();
            rng.Randomize();

            float itemDropChance = Mathf.Clamp(ItemDropChance, 0f, 1f);
            if (rng.Randf() >= itemDropChance)
            {
                GD.PrintS("[Monster] Item drop roll failed.");
                return;
            }

            Item itemDrop = RollSingleItemDrop(rng, manager, mapDiff);
            if (itemDrop == null)
            {
                GD.PrintS("[Monster] Item drop roll passed, but no item was available.");
                return;
            }

            float itemScale = manager != null ? manager.PickupSpriteScale : 0.5f;
            var itemPos = centerPosition + new Vector2(rng.Randf() * 100f - 50f, rng.Randf() * 100f - 50f);
            LootItemFactory.SpawnPickup(parent, itemDrop, itemPos, itemScale, "monster_item");
            GD.PrintS($"[Monster] Spawned item drop: {itemDrop.Name} at {itemPos}");
        }

        private Item RollSingleItemDrop(RandomNumberGenerator rng, EquipmentManager manager, DifficultyLevel mapDiff)
        {
            Item potionDrop = LootItemFactory.RollPotion(rng, 0.08f);
            if (potionDrop != null)
            {
                return potionDrop;
            }

            Item ticketDrop = LootItemFactory.RollTicket(rng, mapDiff, 1f);
            if (ticketDrop != null)
            {
                return ticketDrop;
            }

            if (manager == null)
            {
                GD.PrintS("[Monster] No EquipmentManager found; skipping equipment item drop.");
                return null;
            }

            int playerLevel = _player != null ? (int)_player.Level : (int)Level;
            var options = manager.GetEquipmentSet(Math.Max(1, DropOptionCount), playerLevel, DropLevelOffset);
            var optList = new System.Collections.Generic.List<Item>();
            foreach (var option in options)
            {
                if (option is Item item)
                {
                    optList.Add(item);
                }
            }

            if (optList.Count == 0)
            {
                GD.PrintS("[Monster] No equipment options available for item drop.");
                return null;
            }

            int index = rng.RandiRange(0, optList.Count - 1);
            return optList[index];
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
            GD.Print($"Monster {EntityName} attacks with {Attributes?.EffectiveAtk} ATK!");
            if (_player != null && _player.Attributes?.HP != null)
            {
                // EffectiveAtk respects Burn debuff (ATK reduction while burning)
                int damage = Attributes?.EffectiveAtk ?? 1;
                _player.TakeDamage(damage, this);
                GD.Print($"[COMBAT] {EntityName} attacks Player for {damage} damage! Player HP: {_player.Attributes.HP.CurrentHP}/{_player.Attributes.HP.MaxHP}");
            }
        }
    }
}