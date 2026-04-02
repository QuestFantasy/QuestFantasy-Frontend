using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Characters
{
    /// <summary>
    /// Monster character class. Represents NPCs that can be fought.
    /// Handles monster-specific attribute calculations and behavior.
    /// </summary>
    public class Monster : Character
    {
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
        private const float RepathInterval = 0.3f;

        // Optimization: Frame slicing for pathfinding
        private static ulong _lastPathfindingFrame = 0;
        private static int _pathfindingsThisFrame = 0;
        private const int MaxPathsPerFrame = 2;

        // Animation logic
        private Texture _standTexture;
        private Texture _walkTexture;
        private float _animationTimer = 0f;
        private const float AnimationInterval = 0.2f;
        private bool _isWalkFrame = false;

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
            return position.DistanceTo(_player.GlobalPosition) <= _map.TileSize * 0.75f;
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
            Vector2 spawnCenter = _map.GetSpawnWorldPosition();
            Vector2 fallbackPosition = spawnCenter;

            for (int radius = 0; radius <= 5; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius) continue;

                        Vector2 checkPos = spawnCenter + new Vector2(x * _map.TileSize, y * _map.TileSize);

                        if (!_map.CanMoveTo(GetBodyRect(checkPos))) continue;
                        if (IsSpawnPositionOccupied(checkPos)) continue;
                        if (IsPlayerSpawnPosition(checkPos)) continue;

                        Vector2 candidateTile = WorldToTile(checkPos);
                        if (IsTileOccupiedByOtherMonster((int)candidateTile.x, (int)candidateTile.y)) continue;

                        Position = checkPos;
                        _occupiedSpawnPositions.Add(checkPos);
                        return;
                    }
                }
            }

            if (!IsPlayerSpawnPosition(fallbackPosition))
            {
                Position = fallbackPosition;
                _occupiedSpawnPositions.Add(fallbackPosition);
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
            Texture = _standTexture;

            GD.Print($"Monster ready at {GlobalPosition}");
        }

        public override void _ExitTree()
        {
            _activeMonsters.Remove(this);
            base._ExitTree();
        }

        public override void UpdateAttributes()
        {
            // Uses logic from main branch
            if (Attributes == null || Abilities == null)
            {
                GD.PrintErr($"[Monster] {EntityName}: Attributes or Abilities not initialized");
                return;
            }

            int levelBonus = (int)(Level - 1);

            Attributes.TotalAtk = Abilities.Atk + levelBonus;
            Attributes.TotalDef = Abilities.Def + levelBonus;
            Attributes.TotalSpd = Abilities.Spd + levelBonus;
            Attributes.TotalVit = Abilities.Vit + levelBonus;

            if (Attributes.HP != null)
                Attributes.HP.UpdateMax(Attributes.TotalVit);
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_map == null || _player == null) return;

            if (_repathCooldown > 0f)
            {
                _repathCooldown -= delta;
            }

            CheckPathflowAndStuck(delta);
            MoveProcess(delta);
            UpdateAnimation(delta);
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

        private void MoveProcess(float delta)
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

            float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
            float speedMultiplier = distanceToPlayer > 200f ? 1.5f : (distanceToPlayer > 80f ? 1.0f : 0.8f);

            // In case Player doesnt have MoveSpeed, hardcode fallback to 100f. Assuming it might have been refactored in main.
            float speed = 100f * speedMultiplier;

            Vector2 direction = (nextWaypoint - GlobalPosition).Normalized();
            Velocity = direction * speed;

            MoveAndSlide();
        }

        private void MoveAndSlide()
        {
            Vector2 deltaMove = Velocity * GetPhysicsProcessDeltaTime();
            GlobalPosition += deltaMove;
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

        public override void Attack()
        {
            GD.Print($"Monster {EntityName} attacks with {Attributes?.TotalAtk} ATK!");
        }
    }
}