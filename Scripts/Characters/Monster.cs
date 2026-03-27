using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Characters
{
    public class Monster : Character
    {
        public int ExperienceReward { get; private set; }
        public Vector2 Velocity { get; private set; } // NEW: Used for MoveAndSlide

        private Map _map;
        private MapPlayer _player;

        private List<Vector2> _currentPath = new List<Vector2>();
        private readonly Random _random = new Random();

        // Anti-stuck system (NEW)
        private Vector2 _lastPosition;
        private float _stuckTime;
        private Vector2 _lastTargetTile = new Vector2(-999, -999);

        // Slightly smaller than 1x1 to avoid snagging on exact float grid bounds when sliding
        public Vector2 BodySizeInTiles = new Vector2(0.8f, 0.8f);

        public void SetEnvironment(Map map, MapPlayer player)
        {
            _map = map;
            _player = player;
            FindSafeSpawnLocation();
            RecomputePath();
        }

        private void FindSafeSpawnLocation()
        {
            Vector2 spawnCenter = _map.GetSpawnWorldPosition();
            Position = spawnCenter;
            for (int x = -5; x <= 5; x++)
            {
                for (int y = -5; y <= 5; y++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) >= 10)
                    {
                        Vector2 checkPos = spawnCenter + new Vector2(x * _map.TileSize, y * _map.TileSize);
                        if (_map.CanMoveTo(new Rect2(checkPos - new Vector2(10, 10), new Vector2(20, 20))))
                        {
                            Position = checkPos;
                            return;
                        }
                    }
                }
            }
        }

        public override void _Ready()
        {
            UpdateAttributes();
            SetPhysicsProcess(true);
            _lastPosition = GlobalPosition;
        }

        public override void UpdateAttributes()
        {
            if (Attributes == null)
            {
                Attributes = new Attributes();
            }
            int levelMultiplier = (int)(Level <= 0 ? 1 : Level);
            Attributes.TotalAtk = _random.Next(5, 15) * levelMultiplier;
            Attributes.TotalDef = _random.Next(1, 10) * levelMultiplier;
            Attributes.TotalSpd = _random.Next(10, 30);
            Attributes.TotalVit = _random.Next(20, 50) * levelMultiplier;
            ExperienceReward = _random.Next(10, 25) * levelMultiplier;
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_map == null || _player == null) return;

            // Anti-stuck and Target changed path recomputing logic (NEW)
            CheckPathflowAndStuck(delta);

            // Keep Pathfinding separate from Movement logic (NEW)
            MoveProcess(delta);
        }

        // --- NEW: ANTI STUCK & WAYPOINT MANAGEMENT ---
        private void CheckPathflowAndStuck(float delta)
        {
            float distMoved = _lastPosition.DistanceTo(GlobalPosition);

            if (distMoved < 0.5f)
            {
                _stuckTime += delta;
            }
            else
            {
                _stuckTime = 0;
            }

            _lastPosition = GlobalPosition;

            Vector2 targetTile = WorldToTile(_player.Position);
            bool targetMoved = targetTile != _lastTargetTile;
            bool isStuck = _stuckTime > 0.5f;

            // Only recompute when target changes OR when stuck (NEW logic, no recomputing every frame/timeout)
            if (targetMoved || isStuck)
            {
                if (isStuck) _stuckTime = 0; // Reset stuck timer
                _lastTargetTile = targetTile;
                RecomputePath();
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

            // NEW: Try finding a path with expanded obstacle clearance first
            _currentPath = FindAStarPath(startTile, targetTile, inflateObstacles: true);

            // If the player is in a narrow corridor making the inflated path impossible, fallback to direct path
            if (_currentPath.Count == 0)
            {
                _currentPath = FindAStarPath(startTile, targetTile, inflateObstacles: false);
            }
        }

        // --- NEW: MOVEMENT LOGIC RESTRUCTURED ---
        private void MoveProcess(float delta)
        {
            if (_currentPath.Count == 0) return;

            Vector2 nextWaypoint = _currentPath[0];
            float dist = GlobalPosition.DistanceTo(nextWaypoint);

            // Waypoint tolerance (CRITICAL): Check if within 8 pixels
            if (dist < 8.0f)
            {
                _currentPath.RemoveAt(0);
                if (_currentPath.Count == 0) return;
                nextWaypoint = _currentPath[0];
            }

            float speed = _player.MoveSpeed * 0.90f;

            // Move using requested direction vector approach (NEW)
            Vector2 direction = (nextWaypoint - GlobalPosition).Normalized();
            Velocity = direction * speed;

            MoveAndSlide(); // Calls internal emulator
        }

        // NEW: move via MoveAndSlide logic internally without assigning Position directly, using map collisions. 
        // Emulates Godot physics with Map collision data since we are a Sprite, not a Godot body.
        private void MoveAndSlide()
        {
            Vector2 deltaMove = Velocity * GetPhysicsProcessDeltaTime();

            Vector2 nextX = new Vector2(GlobalPosition.x + deltaMove.x, GlobalPosition.y);
            if (_map.CanMoveTo(GetBodyRect(nextX)))
            {
                GlobalPosition = nextX;
            }

            Vector2 nextY = new Vector2(GlobalPosition.x, GlobalPosition.y + deltaMove.y);
            if (_map.CanMoveTo(GetBodyRect(nextY)))
            {
                GlobalPosition = nextY;
            }
        }

        private Rect2 GetBodyRect(Vector2 centerPosition)
        {
            Vector2 bodySize = BodySizeInTiles * (_map?.TileSize ?? 24f);
            return new Rect2(centerPosition - bodySize / 2f, bodySize);
        }

        // --- PATHFINDING ---
        // Treat obstacles as expanded (inflate obstacles logically by 1 tile when checking walkability) (CHANGED)
        private bool IsWalkableInflated(int x, int y)
        {
            if (!_map.IsWalkableTile(x, y)) return false;

            // Enforce minimum 1-tile distance buffering from any wall
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (!_map.IsWalkableTile(x + dx, y + dy)) return false;
                }
            }
            return true;
        }

        // Use integer positions (C# tuple) & Manhattan distance & ClosedSet (CHANGED)
        private List<Vector2> FindAStarPath(Vector2 startVec, Vector2 goalVec, bool inflateObstacles)
        {
            (int x, int y) start = (Mathf.RoundToInt(startVec.x), Mathf.RoundToInt(startVec.y));
            (int x, int y) goal = (Mathf.RoundToInt(goalVec.x), Mathf.RoundToInt(goalVec.y));

            var openSet = new List<(int x, int y)> { start };
            var closedSet = new HashSet<(int x, int y)>();
            var cameFrom = new Dictionary<(int x, int y), (int x, int y)>();

            var gScore = new Dictionary<(int x, int y), float> { [start] = 0 };
            var fScore = new Dictionary<(int x, int y), float> { [start] = ManhattanDistance(start, goal) };

            int maxIterations = 5000;
            int iterations = 0;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                // Keep openSet but improve readability
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
                closedSet.Add(current);  // Add closedSet to avoid re-processing nodes (NEW)

                var neighbors = new (int x, int y)[] {
                    (current.x + 1, current.y),
                    (current.x - 1, current.y),
                    (current.x, current.y + 1),
                    (current.x, current.y - 1)
                };

                foreach (var neighbor in neighbors)
                {
                    if (closedSet.Contains(neighbor)) continue;

                    // Expanded Walkability Check Note: Goal is always walkble visually or it's unreachable entirely.
                    bool isSafe = (neighbor.x == goal.x && neighbor.y == goal.y) ? _map.IsWalkableTile(neighbor.x, neighbor.y) :
                        (inflateObstacles ? IsWalkableInflated(neighbor.x, neighbor.y) : _map.IsWalkableTile(neighbor.x, neighbor.y));

                    if (!isSafe) continue;

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

        // Replace Euclidean heuristic with Manhattan distance (CHANGED)
        private float ManhattanDistance((int x, int y) a, (int x, int y) b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        private List<Vector2> ReconstructPath(Dictionary<(int x, int y), (int x, int y)> cameFrom, (int x, int y) current)
        {
            var path = new List<Vector2>();
            while (cameFrom.ContainsKey(current))
            {
                // Ensure path nodes are centered in tiles (avoid edge positions) (CHANGED)
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