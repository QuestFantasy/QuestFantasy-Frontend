using Godot;
using System;
using System.Collections.Generic;
using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Characters
{
	public class Monster : Character
	{
		public int ExperienceReward { get; private set; }
		
		private PrototypeMap _map;
		private PrototypePlayer _player;
		
		private Timer _pathTimer;
		private List<Vector2> _currentPath = new List<Vector2>();
		private Random _random = new Random();

		// Slightly smaller than 1x1 to avoid snagging on exact float grid bounds when sliding
		public Vector2 BodySizeInTiles = new Vector2(0.8f, 0.8f);

		public void SetEnvironment(PrototypeMap map, PrototypePlayer player)
		{
			_map = map;
			_player = player;
			FindSafeSpawnLocation();
			UpdatePath();
		}

		private void FindSafeSpawnLocation()
		{
			Vector2 spawnCenter = _map.GetSpawnWorldPosition();
			Position = spawnCenter;
			for (int x = -5; x <= 5; x++)
			{
				for (int y = -5; y <= 5; y++)
				{
					if (Mathf.Abs(x) + Mathf.Abs(y) >= 10) // At least 10 tiles away from player
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
			
			// Periodically refresh the A* path towards player
			_pathTimer = new Timer();
			_pathTimer.WaitTime = 0.5f;
			_pathTimer.Autostart = true;
			_pathTimer.Connect("timeout", this, nameof(UpdatePath));
			AddChild(_pathTimer);
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
			Move();
		}

		public override void Move()
		{
			if (_map == null || _currentPath.Count == 0) return;

			Vector2 target = _currentPath[0];
			Vector2 dir = target - Position;
			float dist = dir.Length();

			if (dist < 2.0f)
			{
				_currentPath.RemoveAt(0);
				if (_currentPath.Count == 0) return;
				target = _currentPath[0];
				dir = target - Position;
			}

			Vector2 velocity = dir.Normalized() * (_player.MoveSpeed * 0.95f);
			TryMove(velocity * GetPhysicsProcessDeltaTime());
		}

		// Mimic PrototypePlayer collision via PrototypeMap walkable bounds
		private void TryMove(Vector2 deltaMove)
		{
			Vector2 nextX = new Vector2(Position.x + deltaMove.x, Position.y);
			if (_map.CanMoveTo(GetBodyRect(nextX)))
			{
				Position = nextX;
			}

			Vector2 nextY = new Vector2(Position.x, Position.y + deltaMove.y);
			if (_map.CanMoveTo(GetBodyRect(nextY)))
			{
				Position = nextY;
			}
		}

		private Rect2 GetBodyRect(Vector2 centerPosition)
		{
			Vector2 bodySize = BodySizeInTiles * (_map?.TileSize ?? 24f);
			return new Rect2(centerPosition - bodySize / 2f, bodySize);
		}

		private void UpdatePath()
		{
			if (_map == null || _player == null) return;
			
			Vector2 startTile = WorldToTile(Position);
			Vector2 targetTile = WorldToTile(_player.Position);

			if (startTile == targetTile)
			{
				_currentPath.Clear();
				return;
			}

			_currentPath = FindAStarPath(startTile, targetTile);
		}

		private Vector2 WorldToTile(Vector2 worldPos)
		{
			int tX = Mathf.Clamp(Mathf.FloorToInt(worldPos.x / _map.TileSize), 0, _map.WorldTileWidth - 1);
			int tY = Mathf.Clamp(Mathf.FloorToInt(worldPos.y / _map.TileSize), 0, _map.WorldTileHeight - 1);
			return new Vector2(tX, tY);
		}

		private Vector2 TileToWorldCenter(Vector2 tile)
		{
			float gX = _map.GlobalPosition.x + tile.x * _map.TileSize + (_map.TileSize / 2f);
			float gY = _map.GlobalPosition.y + tile.y * _map.TileSize + (_map.TileSize / 2f);
			return new Vector2(gX, gY);
		}

		// Simple A* pathfinding onto grid using PrototypeMap.IsWalkableTile()
		private List<Vector2> FindAStarPath(Vector2 start, Vector2 goal)
		{
			var openSet = new List<Vector2> { start };
			var cameFrom = new Dictionary<Vector2, Vector2>();
			
			var gScore = new Dictionary<Vector2, float> { [start] = 0 };
			var fScore = new Dictionary<Vector2, float> { [start] = start.DistanceTo(goal) };

			int maxIterations = 500;
			int iterations = 0;

			while (openSet.Count > 0 && iterations < maxIterations)
			{
				iterations++;
				
				// Fetch lowest fScore
				Vector2 current = openSet[0];
				foreach (var node in openSet)
				{
					float currentF = fScore.ContainsKey(current) ? fScore[current] : float.MaxValue;
					float nodeF = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
					if (nodeF < currentF) current = node;
				}

				if (current == goal)
				{
					return ReconstructPath(cameFrom, current);
				}

				openSet.Remove(current);

				Vector2[] neighbors = new[] {
					current + Vector2.Right,
					current + Vector2.Left,
					current + Vector2.Up,
					current + Vector2.Down
				};

				foreach (var neighbor in neighbors)
				{
					if (!_map.IsWalkableTile((int)neighbor.x, (int)neighbor.y)) continue;

					float tentative_gScore = (gScore.ContainsKey(current) ? gScore[current] : float.MaxValue) + 1;
					float neighbor_gScore = gScore.ContainsKey(neighbor) ? gScore[neighbor] : float.MaxValue;
					
					if (tentative_gScore < neighbor_gScore)
					{
						cameFrom[neighbor] = current;
						gScore[neighbor] = tentative_gScore;
						fScore[neighbor] = gScore[neighbor] + neighbor.DistanceTo(goal);
						
						if (!openSet.Contains(neighbor))
						{
							openSet.Add(neighbor);
						}
					}
				}
			}
			return new List<Vector2>();
		}

		private List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
		{
			var path = new List<Vector2>();
			while (cameFrom.ContainsKey(current))
			{
				path.Add(TileToWorldCenter(current));
				current = cameFrom[current];
			}
			path.Reverse();
			return path;
		}

		public override void Attack()
		{
			// TODO: Implement attack logic
			GD.Print($"Monster {EntityName} attacks with {Attributes?.TotalAtk} ATK!");
		}
	}
}
