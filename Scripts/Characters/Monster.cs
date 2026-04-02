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
		private static readonly List<Vector2> _occupiedSpawnPositions = new List<Vector2>();
		private const float SpawnPositionTolerance = 1.0f;
		private static readonly List<Monster> _activeMonsters = new List<Monster>();

		// Anti-stuck system (NEW)
		private Vector2 _lastPosition;
		private float _stuckTime;
		private Vector2 _lastTargetTile = new Vector2(-999, -999);
		private float _repathCooldown = 0f;
		private const float RepathInterval = 0.3f;
		
		// Optimization: Frame slicing for pathfinding
		private static ulong _lastPathfindingFrame = 0;
		private static int _pathfindingsThisFrame = 0;
		private const int MaxPathsPerFrame = 2; // 每個物理幀最多計算的路徑數量，避免卡頓

		// Slightly smaller than 1x1 to avoid snagging on exact float grid bounds when sliding
		public Vector2 BodySizeInTiles = new Vector2(0.1f, 0.1f);

		public void SetEnvironment(Map map, MapPlayer player)
		{
			_map = map;
			_player = player;
			FindSafeSpawnLocation();
			// RecomputePath(); // 移除初始強制計算，改由 _PhysicsProcess 中分攤計算以防止生成時瞬間卡頓
			_repathCooldown = (float)_random.NextDouble() * 1.0f; // 隨機初始延遲，分散第一次計算時間
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
			if (_player == null)
			{
				return false;
			}

			return position.DistanceTo(_player.GlobalPosition) <= _map.TileSize * 0.75f;
		}

		private bool IsTileOccupiedByOtherMonster(int x, int y)
		{
			foreach (Monster monster in _activeMonsters)
			{
				if (monster == null || monster == this)
				{
					continue;
				}

				// if (!Godot.GD.IsInstanceValid(monster))
				// {
				// 	continue;
				// }

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
						if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
						{
							continue;
						}

						Vector2 checkPos = spawnCenter + new Vector2(x * _map.TileSize, y * _map.TileSize);

						if (!_map.CanMoveTo(GetBodyRect(checkPos)))
						{
							continue;
						}

						if (IsSpawnPositionOccupied(checkPos))
						{
							continue;
						}

						if (IsPlayerSpawnPosition(checkPos))
						{
							continue;
						}

						Vector2 candidateTile = WorldToTile(checkPos);
						if (IsTileOccupiedByOtherMonster((int)candidateTile.x, (int)candidateTile.y))
						{
							continue;
						}

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
			UpdateAttributes();
			SetPhysicsProcess(true);
			_lastPosition = GlobalPosition;
			if (!_activeMonsters.Contains(this))
			{
				_activeMonsters.Add(this);
			}
			GD.Print($"Monster ready at {GlobalPosition}");
		}

		public override void _ExitTree()
		{
			_activeMonsters.Remove(this);
			base._ExitTree();
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

			if (_repathCooldown > 0f)
			{
				_repathCooldown -= delta;
			}

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
			bool isStuck = _stuckTime > 1.0f;

			// Recompute only when needed, and not more often than the cooldown allows.
			if ((targetMoved || isStuck) && _repathCooldown <= 0f)
			{
				// 使用 Engine.GetPhysicsFrames() 來做到分散運算 (Frame-slicing)
				ulong currentFrame = Engine.GetPhysicsFrames();
				if (currentFrame != _lastPathfindingFrame)
				{
					_lastPathfindingFrame = currentFrame;
					_pathfindingsThisFrame = 0;
				}

				// 檢查這個 physics frame 是否還有計算 A* 的額度
				if (_pathfindingsThisFrame < MaxPathsPerFrame)
				{
					_pathfindingsThisFrame++;

					if (isStuck)
					{
						_stuckTime = 0f;
					}

					_lastTargetTile = targetTile;
					RecomputePath();
					// 加入隨機的 Jitter (0.0~0.2秒) 讓這些怪物不要在未來同一時間重新計算
					_repathCooldown = RepathInterval + (float)_random.NextDouble() * 0.2f;
				}
				// 若此幀運算額度已滿，則 _repathCooldown 仍為 0，怪物會在下一個物理幀繼續嘗試排隊計算
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
			if (dist < 12.0f)
			{
				_currentPath.RemoveAt(0);
				if (_currentPath.Count == 0) return;
				nextWaypoint = _currentPath[0];
			}

			float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);

			float speedMultiplier;
			if (distanceToPlayer > 200f)
			{
				speedMultiplier = 1.5f; // far → faster
			}
			else if (distanceToPlayer > 80f)
			{
				speedMultiplier = 1.0f; // medium
			}
			else
			{
				speedMultiplier = 0.8f; // close → slower
			}

			float speed = _player.MoveSpeed * speedMultiplier;

			// Move using requested direction vector approach (NEW)
			Vector2 direction = (nextWaypoint - GlobalPosition).Normalized();
			Velocity = direction * speed;

			MoveAndSlide(); // Calls internal emulator
		}

		// NEW: move via MoveAndSlide logic internally without assigning Position directly, using map collisions. 
		// Emulates Godot physics with Map collision data since we are a Sprite, not a Godot body.
		private void MoveAndSlide()
		{
			// Vector2 deltaMove = Velocity * GetPhysicsProcessDeltaTime();

			// Vector2 nextX = new Vector2(GlobalPosition.x + deltaMove.x, GlobalPosition.y);
			// if (_map.CanMoveTo(GetBodyRect(nextX)))
			// {
			// 	GlobalPosition = nextX;
			// }

			// Vector2 nextY = new Vector2(GlobalPosition.x, GlobalPosition.y + deltaMove.y);
			// if (_map.CanMoveTo(GetBodyRect(nextY)))
			// {
			// 	GlobalPosition = nextY;
			// }
			Vector2 deltaMove = Velocity * GetPhysicsProcessDeltaTime();
			GlobalPosition += deltaMove;
		}

		private Rect2 GetBodyRect(Vector2 centerPosition)
		{
			// Vector2 bodySize = BodySizeInTiles * (_map?.TileSize ?? 2f);
			Vector2 bodySize = new Vector2(0.1f, 0.1f);
			return new Rect2(centerPosition - bodySize / 2f, bodySize);
		}

		// --- PATHFINDING ---
		// Treat obstacles as expanded (inflate obstacles logically by 1 tile when checking walkability) (CHANGED)
		private bool IsWalkableInflated(int x, int y)
		{
			if (!_map.IsWalkableTile(x, y)) return false;

			// // Enforce minimum 1-tile distance buffering from any wall
			// for (int dx = -1; dx <= 1; dx++)
			// {
			// 	for (int dy = -1; dy <= 1; dy++)
			// 	{
			// 		if (dx == 0 && dy == 0) continue;
			// 		if (!_map.IsWalkableTile(x + dx, y + dy)) return false;
			// 	}
			// }
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

			int maxIterations = 500;
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
