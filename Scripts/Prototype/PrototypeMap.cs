using Godot;
using System.Collections.Generic;

public class PrototypeMap : Node2D
{
	[Export] public int TileSize = 24;
	[Export] public int RoomTileSize = 100;
	[Export] public int RoomsX = 2;
	[Export] public int RoomsY = 2;
	[Export] public int Seed = 0;
	[Export] public int StartExitInsetTiles = 6;
	[Export] public float BorderWallThicknessRatio = 0.08f;
	[Export] public float ObstacleFillRate = 0.14f;
	[Export] public float PortalPairChance = 0.55f;
	[Export] public float ExitTriggerRadius = 24f;
	[Export] public int HazardClusterMinTiles = 15;
	[Export] public int HazardClusterMaxTiles = 120;
	[Export] public string BoxClosedTexturePath = "res://Assets/Box/Box_Closed.png";
	[Export] public string BoxOpenTexturePath = "res://Assets/Box/Box_Open.png";

	private PrototypeTileType[,] _tiles;
	private bool[,] _protectedPath;
	private ScenarioType[,] _roomScenarios;
	private Vector2[,] _roomStartTiles;
	private Vector2[,] _roomExitTiles;
	private readonly Dictionary<string, Vector2> _portalLinks = new Dictionary<string, Vector2>();
	private readonly List<Vector2> _boxTiles = new List<Vector2>();
	private ImageTexture _mapTexture;
	private Texture _boxClosedTexture;
	private Texture _boxOpenTexture;
	private bool[,] _openedBoxes;
	private RandomNumberGenerator _random;
	private static readonly RandomNumberGenerator _seedRandom = new RandomNumberGenerator();
	private static bool _seedRandomInitialized;

	public int WorldTileWidth => RoomsX * RoomTileSize;
	public int WorldTileHeight => RoomsY * RoomTileSize;
	public int WorldPixelWidth => WorldTileWidth * TileSize;
	public int WorldPixelHeight => WorldTileHeight * TileSize;

	public void Generate()
	{
		if (RoomsX <= 0 || RoomsY <= 0 || RoomTileSize <= 0 || TileSize <= 0)
		{
			GD.PushError("PrototypeMap settings must be greater than 0.");
			return;
		}

		if (Seed <= 0)
		{
			Seed = GenerateRandomSeed();
		}

		_random = new RandomNumberGenerator();
		_random.Seed = (ulong)Seed;

		_tiles = new PrototypeTileType[WorldTileWidth, WorldTileHeight];
		_protectedPath = new bool[WorldTileWidth, WorldTileHeight];
		_roomScenarios = new ScenarioType[RoomsX, RoomsY];
		_roomStartTiles = new Vector2[RoomsX, RoomsY];
		_roomExitTiles = new Vector2[RoomsX, RoomsY];
		_openedBoxes = new bool[WorldTileWidth, WorldTileHeight];
		_portalLinks.Clear();
		_boxTiles.Clear();

		GenerateScenarioGrid();
		FillBaseFloor();
		BuildRoomWalls();
		GenerateRoomStartAndExitTiles();
		CarveGuaranteedRandomPaths();
		PlaceRoomObjects();
		CreatePortalPairs();
		EnsureCriticalTiles();

		BuildMapTexture();
		LoadBoxTextures();
		RebuildBoxTileList();
		Update();
	}

	public void RegenerateWithRandomSeed()
	{
		Seed = GenerateRandomSeed();
		Generate();
	}

	public Rect2 GetRoomBoundsPixels(Vector2 roomIndex)
	{
		int roomX = Mathf.Clamp((int)roomIndex.x, 0, RoomsX - 1);
		int roomY = Mathf.Clamp((int)roomIndex.y, 0, RoomsY - 1);

		float localX = roomX * RoomTileSize * TileSize;
		float localY = roomY * RoomTileSize * TileSize;

		// Convert from local map coordinates to global/world coordinates.
		float globalX = GlobalPosition.x + localX;
		float globalY = GlobalPosition.y + localY;

		return new Rect2(
			globalX,
			globalY,
			RoomTileSize * TileSize,
			RoomTileSize * TileSize
		);
	}

	public Vector2 GetSpawnWorldPosition()
	{
		return GetRoomStartWorldPosition(Vector2.Zero);
	}

	public Vector2 GetRoomStartWorldPosition(Vector2 roomIndex)
	{
		int roomX = Mathf.Clamp((int)roomIndex.x, 0, RoomsX - 1);
		int roomY = Mathf.Clamp((int)roomIndex.y, 0, RoomsY - 1);
		Vector2 tile = _roomStartTiles[roomX, roomY];
		return TileToWorldCenter((int)tile.x, (int)tile.y);
	}

	public Vector2 GetRoomExitWorldPosition(Vector2 roomIndex)
	{
		int roomX = Mathf.Clamp((int)roomIndex.x, 0, RoomsX - 1);
		int roomY = Mathf.Clamp((int)roomIndex.y, 0, RoomsY - 1);
		Vector2 tile = _roomExitTiles[roomX, roomY];
		return TileToWorldCenter((int)tile.x, (int)tile.y);
	}

	public Vector2 GetRoomIndexByWorldPosition(Vector2 worldPosition)
	{
		int tileX = Mathf.Clamp(Mathf.FloorToInt(worldPosition.x / TileSize), 0, WorldTileWidth - 1);
		int tileY = Mathf.Clamp(Mathf.FloorToInt(worldPosition.y / TileSize), 0, WorldTileHeight - 1);
		return new Vector2(tileX / RoomTileSize, tileY / RoomTileSize);
	}

	public bool TryGetNextRoomIndex(Vector2 currentRoomIndex, out Vector2 nextRoomIndex)
	{
		int roomX = Mathf.Clamp((int)currentRoomIndex.x, 0, RoomsX - 1);
		int roomY = Mathf.Clamp((int)currentRoomIndex.y, 0, RoomsY - 1);
		int linear = roomY * RoomsX + roomX;
		int nextLinear = linear + 1;

		if (nextLinear >= RoomsX * RoomsY)
		{
			nextRoomIndex = currentRoomIndex;
			return false;
		}

		nextRoomIndex = new Vector2(nextLinear % RoomsX, nextLinear / RoomsX);
		return true;
	}

	public bool IsAtRoomExit(Vector2 worldPosition, Vector2 roomIndex)
	{
		Vector2 exitWorld = GetRoomExitWorldPosition(roomIndex);
		return worldPosition.DistanceTo(exitWorld) <= ExitTriggerRadius;
	}

	public bool TryGetPortalDestination(Vector2 worldPosition, out Vector2 destinationWorld)
	{
		Vector2 tile = WorldToTile(worldPosition);
		if (_portalLinks.TryGetValue(TileKey((int)tile.x, (int)tile.y), out Vector2 destinationTile))
		{
			Vector2 arrivalTile = destinationTile;
			if (!TryFindNonPortalArrivalTile((int)destinationTile.x, (int)destinationTile.y, out arrivalTile))
			{
				arrivalTile = destinationTile;
			}

			destinationWorld = TileToWorldCenter((int)arrivalTile.x, (int)arrivalTile.y);

			if (arrivalTile == destinationTile)
			{
				destinationWorld += new Vector2(TileSize * 0.24f, 0f);
			}

			return true;
		}

		destinationWorld = Vector2.Zero;
		return false;
	}

	public bool CanMoveTo(Rect2 worldRect)
	{
		int minTileX = Mathf.FloorToInt(worldRect.Position.x / TileSize);
		int minTileY = Mathf.FloorToInt(worldRect.Position.y / TileSize);
		int maxTileX = Mathf.FloorToInt((worldRect.Position.x + worldRect.Size.x - 1f) / TileSize);
		int maxTileY = Mathf.FloorToInt((worldRect.Position.y + worldRect.Size.y - 1f) / TileSize);

		for (int x = minTileX; x <= maxTileX; x++)
		{
			for (int y = minTileY; y <= maxTileY; y++)
			{
				if (!IsWalkableTile(x, y))
				{
					return false;
				}
			}
		}

		return true;
	}

	public bool IsWalkableTile(int tileX, int tileY)
	{
		if (tileX < 0 || tileY < 0 || tileX >= WorldTileWidth || tileY >= WorldTileHeight)
		{
			return false;
		}

		PrototypeTileType tileType = _tiles[tileX, tileY];
		return tileType == PrototypeTileType.Floor
			|| tileType == PrototypeTileType.Start
			|| tileType == PrototypeTileType.Exit
			|| tileType == PrototypeTileType.Portal;
	}

	public override void _Draw()
	{
		if (_mapTexture == null)
		{
			return;
		}

		DrawTextureRect(_mapTexture, new Rect2(Vector2.Zero, new Vector2(WorldPixelWidth, WorldPixelHeight)), false);
		DrawBoxes();
	}

	public bool TryOpenNearbyBox(Vector2 worldPosition, float maxDistanceTiles = 1.15f)
	{
		if (_tiles == null || _openedBoxes == null)
		{
			return false;
		}

		Vector2 center = WorldToTile(worldPosition);
		int cx = (int)center.x;
		int cy = (int)center.y;
		float maxDistanceSquared = maxDistanceTiles * maxDistanceTiles;
		float bestDistance = float.MaxValue;
		Vector2 bestTile = Vector2.Zero;
		bool found = false;

		for (int x = cx - 1; x <= cx + 1; x++)
		{
			for (int y = cy - 1; y <= cy + 1; y++)
			{
				if (x < 0 || y < 0 || x >= WorldTileWidth || y >= WorldTileHeight)
				{
					continue;
				}

				if (_tiles[x, y] != PrototypeTileType.Box || _openedBoxes[x, y])
				{
					continue;
				}

				float dx = x - cx;
				float dy = y - cy;
				float distanceSquared = dx * dx + dy * dy;
				if (distanceSquared > maxDistanceSquared)
				{
					continue;
				}

				if (distanceSquared < bestDistance)
				{
					bestDistance = distanceSquared;
					bestTile = new Vector2(x, y);
					found = true;
				}
			}
		}

		if (!found)
		{
			return false;
		}

		_openedBoxes[(int)bestTile.x, (int)bestTile.y] = true;
		Update();
		return true;
	}

	private void GenerateScenarioGrid()
	{
		for (int roomX = 0; roomX < RoomsX; roomX++)
		{
			for (int roomY = 0; roomY < RoomsY; roomY++)
			{
				_roomScenarios[roomX, roomY] = (ScenarioType)_random.RandiRange(0, 3);
			}
		}
	}

	private void FillBaseFloor()
	{
		for (int x = 0; x < WorldTileWidth; x++)
		{
			for (int y = 0; y < WorldTileHeight; y++)
			{
				_tiles[x, y] = PrototypeTileType.Floor;
				_protectedPath[x, y] = false;
			}
		}
	}

	private void BuildRoomWalls()
	{
		int wallThickness = Mathf.Max(1, Mathf.FloorToInt(RoomTileSize * BorderWallThicknessRatio));

		for (int roomX = 0; roomX < RoomsX; roomX++)
		{
			for (int roomY = 0; roomY < RoomsY; roomY++)
			{
				GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
				for (int t = 0; t < wallThickness; t++)
				{
					for (int x = sx; x <= ex; x++)
					{
						SetTile(x, sy + t, PrototypeTileType.Wall);
						SetTile(x, ey - t, PrototypeTileType.Wall);
					}
					for (int y = sy; y <= ey; y++)
					{
						SetTile(sx + t, y, PrototypeTileType.Wall);
						SetTile(ex - t, y, PrototypeTileType.Wall);
					}
				}
			}
		}
	}

	private void GenerateRoomStartAndExitTiles()
	{
		int inset = Mathf.Clamp(StartExitInsetTiles, 3, Mathf.Max(3, RoomTileSize / 3));
		for (int roomX = 0; roomX < RoomsX; roomX++)
		{
			for (int roomY = 0; roomY < RoomsY; roomY++)
			{
				int startSide = _random.RandiRange(0, 3);
				int exitSide = (startSide + _random.RandiRange(1, 3)) % 4;
				Vector2 start = BuildEdgePoint(roomX, roomY, startSide, inset);
				Vector2 exit = BuildEdgePoint(roomX, roomY, exitSide, inset);
				if (start.DistanceTo(exit) < RoomTileSize * 0.35f)
				{
					exit = BuildEdgePoint(roomX, roomY, (startSide + 2) % 4, inset);
				}
				_roomStartTiles[roomX, roomY] = start;
				_roomExitTiles[roomX, roomY] = exit;
			}
		}
	}

	private void CarveGuaranteedRandomPaths()
	{
		for (int roomX = 0; roomX < RoomsX; roomX++)
		{
			for (int roomY = 0; roomY < RoomsY; roomY++)
			{
				Vector2 start = _roomStartTiles[roomX, roomY];
				Vector2 exit = _roomExitTiles[roomX, roomY];
				CarveRandomPathInRoom(roomX, roomY, (int)start.x, (int)start.y, (int)exit.x, (int)exit.y, 1);
			}
		}
	}

	private void CarveRandomPathInRoom(int roomX, int roomY, int startX, int startY, int exitX, int exitY, int halfWidth)
	{
		GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
		int minX = sx + 1;
		int minY = sy + 1;
		int maxX = ex - 1;
		int maxY = ey - 1;

		int currentX = startX;
		int currentY = startY;
		var visited = new HashSet<string>();
		int maxSteps = RoomTileSize * RoomTileSize * 2;

		for (int step = 0; step < maxSteps; step++)
		{
			CarveDisk(currentX, currentY, halfWidth);
			visited.Add(TileKey(currentX, currentY));
			if (currentX == exitX && currentY == exitY)
			{
				break;
			}

			Vector2[] candidates = new[]
			{
				new Vector2(currentX + 1, currentY),
				new Vector2(currentX - 1, currentY),
				new Vector2(currentX, currentY + 1),
				new Vector2(currentX, currentY - 1)
			};

			float bestScore = float.MaxValue;
			int bestX = currentX;
			int bestY = currentY;

			foreach (Vector2 candidate in candidates)
			{
				int nx = (int)candidate.x;
				int ny = (int)candidate.y;
				if (nx < minX || ny < minY || nx > maxX || ny > maxY)
				{
					continue;
				}

				float distance = Mathf.Abs(nx - exitX) + Mathf.Abs(ny - exitY);
				float jitter = _random.RandfRange(0f, 4f);
				float revisit = visited.Contains(TileKey(nx, ny)) ? 6f : 0f;
				float score = distance + jitter + revisit;
				if (score < bestScore)
				{
					bestScore = score;
					bestX = nx;
					bestY = ny;
				}
			}

			if (bestX == currentX && bestY == currentY)
			{
				break;
			}

			currentX = bestX;
			currentY = bestY;
		}

		if (!(currentX == exitX && currentY == exitY))
		{
			CarveCorridor(startX, startY, exitX, exitY, halfWidth);
		}

		CarveDisk(startX, startY, 2);
		CarveDisk(exitX, exitY, 2);
	}

	private void PlaceRoomObjects()
	{
		for (int roomX = 0; roomX < RoomsX; roomX++)
		{
			for (int roomY = 0; roomY < RoomsY; roomY++)
			{
				GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
				ScenarioType scenario = _roomScenarios[roomX, roomY];

				for (int x = sx + 1; x < ex; x++)
				{
					for (int y = sy + 1; y < ey; y++)
					{
						if (_protectedPath[x, y] || _tiles[x, y] != PrototypeTileType.Floor)
						{
							continue;
						}
						if (_random.Randf() > ObstacleFillRate)
						{
							continue;
						}
						_tiles[x, y] = PickSolidObstacleForScenario(scenario);
					}
				}

				PlaceHazardClustersForRoom(roomX, roomY, scenario);
			}
		}
	}

	private void PlaceHazardClustersForRoom(int roomX, int roomY, ScenarioType scenario)
	{
		int minSize = Mathf.Clamp(HazardClusterMinTiles, 1, Mathf.Max(1, HazardClusterMaxTiles));
		int maxSize = Mathf.Max(minSize, HazardClusterMaxTiles);

		switch (scenario)
		{
			case ScenarioType.Lava:
				PlaceHazardClusters(roomX, roomY, PrototypeTileType.Lava, _random.RandiRange(2, 4), minSize, maxSize);
				break;
			case ScenarioType.Sea:
				PlaceHazardClusters(roomX, roomY, PrototypeTileType.Water, _random.RandiRange(2, 4), minSize, maxSize);
				break;
			case ScenarioType.Grassland:
				PlaceHazardClusters(roomX, roomY, PrototypeTileType.Water, _random.RandiRange(1, 2), minSize, maxSize);
				break;
			case ScenarioType.Mountain:
			default:
				break;
		}
	}

	private void PlaceHazardClusters(int roomX, int roomY, PrototypeTileType hazardType, int clusterCount, int minTiles, int maxTiles)
	{
		for (int cluster = 0; cluster < clusterCount; cluster++)
		{
			bool placed = false;
			for (int attempt = 0; attempt < 8; attempt++)
			{
				int targetSize = _random.RandiRange(minTiles, maxTiles);
				if (!TryPickRoomHazardSeedTile(roomX, roomY, out Vector2 seed))
				{
					continue;
				}

				bool lineShape = _random.Randf() < 0.5f;
				if (TryCreateHazardCluster(roomX, roomY, hazardType, seed, targetSize, minTiles, lineShape))
				{
					placed = true;
					break;
				}
			}

			if (!placed)
			{
				continue;
			}
		}
	}

	private bool TryCreateHazardCluster(int roomX, int roomY, PrototypeTileType hazardType, Vector2 seed, int targetSize, int minTiles, bool lineShape)
	{
		GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);

		var frontier = new List<Vector2> { seed };
		var used = new HashSet<string>();
		var clusterTiles = new List<Vector2>();
		Vector2 lineDirection = new Vector2(_random.RandiRange(-1, 1), _random.RandiRange(-1, 1));
		if (lineDirection == Vector2.Zero)
		{
			lineDirection = new Vector2(1, 0);
		}

		while (frontier.Count > 0 && clusterTiles.Count < targetSize)
		{
			int idx = _random.RandiRange(0, frontier.Count - 1);
			Vector2 current = frontier[idx];
			frontier.RemoveAt(idx);

			string key = TileKey((int)current.x, (int)current.y);
			if (used.Contains(key))
			{
				continue;
			}
			used.Add(key);

			int x = (int)current.x;
			int y = (int)current.y;
			if (!CanPlaceHazardAt(x, y, sx, sy, ex, ey))
			{
				continue;
			}

			clusterTiles.Add(new Vector2(x, y));

			foreach (Vector2 next in GetGrowthNeighbors(current, lineShape, lineDirection))
			{
				frontier.Add(next);
			}
		}

		if (clusterTiles.Count < minTiles)
		{
			return false;
		}

		for (int i = 0; i < clusterTiles.Count; i++)
		{
			Vector2 tile = clusterTiles[i];
			_tiles[(int)tile.x, (int)tile.y] = hazardType;
		}

		return true;
	}

	private List<Vector2> GetGrowthNeighbors(Vector2 origin, bool lineShape, Vector2 lineDirection)
	{
		var neighbors = new List<Vector2>();
		if (lineShape)
		{
			neighbors.Add(origin + lineDirection);
			neighbors.Add(origin - lineDirection);
			if (_random.Randf() < 0.25f)
			{
				neighbors.Add(origin + new Vector2(lineDirection.y, lineDirection.x));
			}
		}
		else
		{
			neighbors.Add(origin + Vector2.Right);
			neighbors.Add(origin + Vector2.Left);
			neighbors.Add(origin + Vector2.Up);
			neighbors.Add(origin + Vector2.Down);
			if (_random.Randf() < 0.35f)
			{
				neighbors.Add(origin + Vector2.Up + Vector2.Left);
			}
			if (_random.Randf() < 0.35f)
			{
				neighbors.Add(origin + Vector2.Up + Vector2.Right);
			}
			if (_random.Randf() < 0.35f)
			{
				neighbors.Add(origin + Vector2.Down + Vector2.Left);
			}
			if (_random.Randf() < 0.35f)
			{
				neighbors.Add(origin + Vector2.Down + Vector2.Right);
			}
		}

		return neighbors;
	}

	private bool CanPlaceHazardAt(int x, int y, int sx, int sy, int ex, int ey)
	{
		if (x <= sx + 1 || y <= sy + 1 || x >= ex - 1 || y >= ey - 1)
		{
			return false;
		}

		if (_protectedPath[x, y])
		{
			return false;
		}

		return _tiles[x, y] == PrototypeTileType.Floor;
	}

	private void CreatePortalPairs()
	{
		for (int roomX = 0; roomX < RoomsX; roomX++)
		{
			for (int roomY = 0; roomY < RoomsY; roomY++)
			{
				if (_random.Randf() > PortalPairChance)
				{
					continue;
				}

				if (!TryPickRoomWalkableTile(roomX, roomY, out Vector2 a) || !TryPickRoomWalkableTile(roomX, roomY, out Vector2 b, a))
				{
					continue;
				}

				SetTile((int)a.x, (int)a.y, PrototypeTileType.Portal);
				SetTile((int)b.x, (int)b.y, PrototypeTileType.Portal);
				_portalLinks[TileKey((int)a.x, (int)a.y)] = b;
				_portalLinks[TileKey((int)b.x, (int)b.y)] = a;
			}
		}
	}

	private void EnsureCriticalTiles()
	{
		for (int roomX = 0; roomX < RoomsX; roomX++)
		{
			for (int roomY = 0; roomY < RoomsY; roomY++)
			{
				Vector2 start = _roomStartTiles[roomX, roomY];
				Vector2 exit = _roomExitTiles[roomX, roomY];
				SetTile((int)start.x, (int)start.y, PrototypeTileType.Start);
				SetTile((int)exit.x, (int)exit.y, PrototypeTileType.Exit);
			}
		}
	}

	private PrototypeTileType PickSolidObstacleForScenario(ScenarioType scenario)
	{
		float roll = _random.Randf();
		switch (scenario)
		{
			case ScenarioType.Grassland:
				return roll < 0.52f ? PrototypeTileType.Box : PrototypeTileType.Wall;
			case ScenarioType.Mountain:
				return roll < 0.72f ? PrototypeTileType.Wall : PrototypeTileType.Box;
			case ScenarioType.Lava:
				return roll < 0.65f ? PrototypeTileType.Wall : PrototypeTileType.Box;
			default:
				return roll < 0.48f ? PrototypeTileType.Box : PrototypeTileType.Wall;
		}
	}

	private bool TryPickRoomHazardSeedTile(int roomX, int roomY, out Vector2 tile)
	{
		GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
		for (int attempt = 0; attempt < 120; attempt++)
		{
			int x = _random.RandiRange(sx + 2, ex - 2);
			int y = _random.RandiRange(sy + 2, ey - 2);
			if (CanPlaceHazardAt(x, y, sx, sy, ex, ey))
			{
				tile = new Vector2(x, y);
				return true;
			}
		}

		tile = Vector2.Zero;
		return false;
	}

	private bool TryFindNonPortalArrivalTile(int portalX, int portalY, out Vector2 arrivalTile)
	{
		Vector2[] candidates = new[]
		{
			new Vector2(portalX + 1, portalY),
			new Vector2(portalX - 1, portalY),
			new Vector2(portalX, portalY + 1),
			new Vector2(portalX, portalY - 1),
			new Vector2(portalX + 1, portalY + 1),
			new Vector2(portalX - 1, portalY + 1),
			new Vector2(portalX + 1, portalY - 1),
			new Vector2(portalX - 1, portalY - 1)
		};

		for (int i = 0; i < candidates.Length; i++)
		{
			int x = (int)candidates[i].x;
			int y = (int)candidates[i].y;
			if (x < 0 || y < 0 || x >= WorldTileWidth || y >= WorldTileHeight)
			{
				continue;
			}

			PrototypeTileType tileType = _tiles[x, y];
			if (tileType == PrototypeTileType.Floor || tileType == PrototypeTileType.Start || tileType == PrototypeTileType.Exit)
			{
				arrivalTile = new Vector2(x, y);
				return true;
			}
		}

		arrivalTile = Vector2.Zero;
		return false;
	}

	private bool TryPickRoomWalkableTile(int roomX, int roomY, out Vector2 tile, Vector2? avoid = null)
	{
		GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
		for (int attempt = 0; attempt < 80; attempt++)
		{
			int x = _random.RandiRange(sx + 2, ex - 2);
			int y = _random.RandiRange(sy + 2, ey - 2);
			if (_protectedPath[x, y] || _tiles[x, y] != PrototypeTileType.Floor)
			{
				continue;
			}
			if (avoid.HasValue && Mathf.Abs((float)avoid.Value.x - x) + Mathf.Abs((float)avoid.Value.y - y) < 12)
			{
				continue;
			}
			tile = new Vector2(x, y);
			return true;
		}
		tile = Vector2.Zero;
		return false;
	}

	private void BuildMapTexture()
	{
		var image = new Image();
		image.Create(WorldTileWidth, WorldTileHeight, false, Image.Format.Rgba8);
		image.Lock();
		for (int x = 0; x < WorldTileWidth; x++)
		{
			for (int y = 0; y < WorldTileHeight; y++)
			{
				image.SetPixel(x, y, GetTileColor(x, y));
			}
		}
		image.Unlock();
		_mapTexture = new ImageTexture();
		_mapTexture.CreateFromImage(image, 0);
	}

	private Color GetTileColor(int tileX, int tileY)
	{
		switch (_tiles[tileX, tileY])
		{
			case PrototypeTileType.Start: return new Color(0.95f, 0.88f, 0.24f);
			case PrototypeTileType.Exit: return new Color(0.62f, 0.24f, 0.87f);
			case PrototypeTileType.Wall: return new Color(0.12f, 0.12f, 0.15f);
			case PrototypeTileType.Box: return new Color(0.54f, 0.34f, 0.15f);
			case PrototypeTileType.Portal: return new Color(0.88f, 0.26f, 0.80f);
			case PrototypeTileType.Lava: return new Color(0.88f, 0.34f, 0.12f);
			case PrototypeTileType.Water: return new Color(0.20f, 0.50f, 0.86f);
			default: return GetScenarioFloorColor(GetScenarioByTile(tileX, tileY));
		}
	}

	private Color GetScenarioFloorColor(ScenarioType scenario)
	{
		switch (scenario)
		{
			case ScenarioType.Grassland: return new Color(0.40f, 0.66f, 0.36f);
			case ScenarioType.Mountain: return new Color(0.50f, 0.52f, 0.55f);
			case ScenarioType.Lava: return new Color(0.46f, 0.28f, 0.18f);
			default: return new Color(0.34f, 0.56f, 0.74f);
		}
	}

	private ScenarioType GetScenarioByTile(int tileX, int tileY)
	{
		int roomX = Mathf.Clamp(tileX / RoomTileSize, 0, RoomsX - 1);
		int roomY = Mathf.Clamp(tileY / RoomTileSize, 0, RoomsY - 1);
		return _roomScenarios[roomX, roomY];
	}

	private Vector2 BuildEdgePoint(int roomX, int roomY, int side, int inset)
	{
		GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
		int randomX = _random.RandiRange(sx + inset, ex - inset);
		int randomY = _random.RandiRange(sy + inset, ey - inset);
		switch (side)
		{
			case 0: return new Vector2(sx + inset, randomY);
			case 1: return new Vector2(ex - inset, randomY);
			case 2: return new Vector2(randomX, sy + inset);
			default: return new Vector2(randomX, ey - inset);
		}
	}

	private void GetRoomTileBounds(int roomX, int roomY, out int sx, out int sy, out int ex, out int ey)
	{
		sx = roomX * RoomTileSize;
		sy = roomY * RoomTileSize;
		ex = sx + RoomTileSize - 1;
		ey = sy + RoomTileSize - 1;
	}

	private Vector2 WorldToTile(Vector2 worldPosition)
	{
		int tileX = Mathf.Clamp(Mathf.FloorToInt(worldPosition.x / TileSize), 0, WorldTileWidth - 1);
		int tileY = Mathf.Clamp(Mathf.FloorToInt(worldPosition.y / TileSize), 0, WorldTileHeight - 1);
		return new Vector2(tileX, tileY);
	}

	private Vector2 TileToWorldCenter(int tileX, int tileY)
	{
		return new Vector2((tileX + 0.5f) * TileSize, (tileY + 0.5f) * TileSize);
	}

	private void SetTile(int x, int y, PrototypeTileType tileType)
	{
		if (x < 0 || y < 0 || x >= WorldTileWidth || y >= WorldTileHeight)
		{
			return;
		}
		_tiles[x, y] = tileType;
	}

	private void CarveDisk(int centerX, int centerY, int radius)
	{
		for (int x = centerX - radius; x <= centerX + radius; x++)
		{
			for (int y = centerY - radius; y <= centerY + radius; y++)
			{
				if (x < 0 || y < 0 || x >= WorldTileWidth || y >= WorldTileHeight)
				{
					continue;
				}
				if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= radius * radius)
				{
					_tiles[x, y] = PrototypeTileType.Floor;
					_protectedPath[x, y] = true;
				}
			}
		}
	}

	private void CarveCorridor(int fromX, int fromY, int toX, int toY, int halfWidth)
	{
		for (int x = Mathf.Min(fromX, toX); x <= Mathf.Max(fromX, toX); x++)
		{
			for (int y = fromY - halfWidth; y <= fromY + halfWidth; y++)
			{
				CarveDisk(x, y, 0);
			}
		}
		for (int y = Mathf.Min(fromY, toY); y <= Mathf.Max(fromY, toY); y++)
		{
			for (int x = toX - halfWidth; x <= toX + halfWidth; x++)
			{
				CarveDisk(x, y, 0);
			}
		}
	}

	// Cache for tile key strings to avoid allocating a new string for each coordinate pair.
	private static readonly Dictionary<int, string> _tileKeyCache = new Dictionary<int, string>();

	// Allocation-free integer key for a tile coordinate pair.
	private int TileKeyInt(int tileX, int tileY)
	{
		return tileX + tileY * WorldTileWidth;
	}

	private string TileKey(int tileX, int tileY)
	{
		int key = TileKeyInt(tileX, tileY);

		if (_tileKeyCache.TryGetValue(key, out string cached))
		{
			return cached;
		}

		// Create the string once for this coordinate pair and cache it.
		string value = tileX.ToString() + ":" + tileY.ToString();
		_tileKeyCache[key] = value;
		return value;
	}

	private static int GenerateRandomSeed()
	{
		if (!_seedRandomInitialized)
		{
			_seedRandom.Randomize();
			_seedRandomInitialized = true;
		}

		return _seedRandom.RandiRange(1, int.MaxValue);
	}

	private void LoadBoxTextures()
	{
		_boxClosedTexture = GD.Load<Texture>(BoxClosedTexturePath);
		_boxOpenTexture = GD.Load<Texture>(BoxOpenTexturePath);

		if (_boxClosedTexture == null || _boxOpenTexture == null)
		{
			GD.Print("PrototypeMap: box textures not found, using fallback box color.");
		}
	}

	private void RebuildBoxTileList()
	{
		_boxTiles.Clear();
		for (int x = 0; x < WorldTileWidth; x++)
		{
			for (int y = 0; y < WorldTileHeight; y++)
			{
				if (_tiles[x, y] == PrototypeTileType.Box)
				{
					_boxTiles.Add(new Vector2(x, y));
				}
			}
		}
	}

	private void DrawBoxes()
	{
		if (_boxTiles.Count == 0 || _boxClosedTexture == null || _boxOpenTexture == null)
		{
			return;
		}

		for (int i = 0; i < _boxTiles.Count; i++)
		{
			Vector2 tile = _boxTiles[i];
			int tx = (int)tile.x;
			int ty = (int)tile.y;
			Texture texture = _openedBoxes[tx, ty] ? _boxOpenTexture : _boxClosedTexture;
			Rect2 tileRect = new Rect2(tx * TileSize, ty * TileSize, TileSize, TileSize);
			DrawTextureRect(texture, tileRect, false);
		}
	}
}
