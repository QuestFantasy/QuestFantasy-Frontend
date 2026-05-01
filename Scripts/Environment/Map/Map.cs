using Godot;

public class Map : Node2D
{
    [Signal]
    public delegate void BoxOpened(Vector2 worldPosition);

    [Export] public MapGenerationConfig GenerationConfig = new MapGenerationConfig();

    // Compatibility accessors let existing callers use map.TileSize and similar properties.
    // Parameter meaning/default rationale is documented in MapGenerationConfig.
    public int TileSize { get => GenerationConfig.TileSize; set => GenerationConfig.TileSize = value; }
    public int RoomTileSize { get => GenerationConfig.RoomTileSize; set => GenerationConfig.RoomTileSize = value; }
    public int RoomsX { get => GenerationConfig.RoomsX; set => GenerationConfig.RoomsX = value; }
    public int RoomsY { get => GenerationConfig.RoomsY; set => GenerationConfig.RoomsY = value; }
    public int Seed { get => GenerationConfig.Seed; set => GenerationConfig.Seed = value; }
    public int StartExitInsetTiles { get => GenerationConfig.StartExitInsetTiles; set => GenerationConfig.StartExitInsetTiles = value; }
    public float BorderWallThicknessRatio { get => GenerationConfig.BorderWallThicknessRatio; set => GenerationConfig.BorderWallThicknessRatio = value; }
    public float ObstacleFillRate { get => GenerationConfig.ObstacleFillRate; set => GenerationConfig.ObstacleFillRate = value; }
    public float PortalPairChance { get => GenerationConfig.PortalPairChance; set => GenerationConfig.PortalPairChance = value; }
    public float ExitTriggerRadius { get => GenerationConfig.ExitTriggerRadius; set => GenerationConfig.ExitTriggerRadius = value; }
    public int HazardClusterMinTiles { get => GenerationConfig.HazardClusterMinTiles; set => GenerationConfig.HazardClusterMinTiles = value; }
    public int HazardClusterMaxTiles { get => GenerationConfig.HazardClusterMaxTiles; set => GenerationConfig.HazardClusterMaxTiles = value; }
    public int BoxCountMin { get => GenerationConfig.BoxCountMin; set => GenerationConfig.BoxCountMin = value; }
    public int BoxCountMax { get => GenerationConfig.BoxCountMax; set => GenerationConfig.BoxCountMax = value; }
    public string BoxClosedTexturePath { get => GenerationConfig.BoxClosedTexturePath; set => GenerationConfig.BoxClosedTexturePath = value; }
    public string BoxOpenTexturePath { get => GenerationConfig.BoxOpenTexturePath; set => GenerationConfig.BoxOpenTexturePath = value; }

    private MapTileData _data;
    private readonly MapGenerator _generator = new MapGenerator();
    private readonly MapCollisionSystem _collisionSystem = new MapCollisionSystem();
    private readonly MapInteractionSystem _interactionSystem = new MapInteractionSystem();
    private readonly MapRenderSystem _renderSystem = new MapRenderSystem();
    private RandomNumberGenerator _random;
    private static readonly RandomNumberGenerator _seedRandom = new RandomNumberGenerator();
    private static bool _seedRandomInitialized;
    public bool DisableRoomExits { get; set; } = false;  // Flag for lobby: disables room exit transitions

    public int WorldTileWidth => _data != null ? _data.WorldTileWidth : RoomsX * RoomTileSize;
    public int WorldTileHeight => _data != null ? _data.WorldTileHeight : RoomsY * RoomTileSize;
    public int WorldPixelWidth => _data != null ? _data.WorldPixelWidth : WorldTileWidth * TileSize;
    public int WorldPixelHeight => _data != null ? _data.WorldPixelHeight : WorldTileHeight * TileSize;

    public void Generate()
    {
        if (GenerationConfig == null)
        {
            GenerationConfig = new MapGenerationConfig();
        }

        MapGenerationConfig config = BuildGenerationConfig();
        if (!config.Validate(out string validationError))
        {
            GD.PushError("Map settings are invalid: " + validationError);
            return;
        }

        if (config.Seed <= 0)
        {
            config.Seed = GenerateRandomSeed();
            GenerationConfig.Seed = config.Seed;
        }

        _random = new RandomNumberGenerator();
        _random.Seed = (ulong)config.Seed;
        _data = new MapTileData(config.TileSize, config.RoomTileSize, config.RoomsX, config.RoomsY);

        _generator.Generate(
            _data,
            _random,
            config);

        _renderSystem.Rebuild(_data, config.BoxClosedTexturePath, config.BoxOpenTexturePath);
        Update();
    }

    public void RegenerateWithRandomSeed()
    {
        if (GenerationConfig == null)
        {
            GenerationConfig = new MapGenerationConfig();
        }

        GenerationConfig.Seed = GenerateRandomSeed();
        Generate();
    }

    public Rect2 GetRoomBoundsPixels(Vector2 roomIndex)
    {
        int roomX = Mathf.Clamp((int)roomIndex.x, 0, RoomsX - 1);
        int roomY = Mathf.Clamp((int)roomIndex.y, 0, RoomsY - 1);

        float localX = roomX * RoomTileSize * TileSize;
        float localY = roomY * RoomTileSize * TileSize;
        float globalX = GlobalPosition.x + localX;
        float globalY = GlobalPosition.y + localY;

        return new Rect2(globalX, globalY, RoomTileSize * TileSize, RoomTileSize * TileSize);
    }

    public Vector2 GetSpawnWorldPosition()
    {
        return GetRoomStartWorldPosition(Vector2.Zero);
    }

    public Vector2 GetRoomStartWorldPosition(Vector2 roomIndex)
    {
        if (_data == null)
        {
            return Vector2.Zero;
        }

        int roomX = Mathf.Clamp((int)roomIndex.x, 0, RoomsX - 1);
        int roomY = Mathf.Clamp((int)roomIndex.y, 0, RoomsY - 1);
        Vector2 tile = _data.RoomStartTiles[roomX, roomY];
        return _data.TileToWorldCenter((int)tile.x, (int)tile.y);
    }

    public Vector2 GetRoomExitWorldPosition(Vector2 roomIndex)
    {
        if (_data == null)
        {
            return Vector2.Zero;
        }

        int roomX = Mathf.Clamp((int)roomIndex.x, 0, RoomsX - 1);
        int roomY = Mathf.Clamp((int)roomIndex.y, 0, RoomsY - 1);
        Vector2 tile = _data.RoomExitTiles[roomX, roomY];
        return _data.TileToWorldCenter((int)tile.x, (int)tile.y);
    }

    public Vector2 GetRoomIndexByWorldPosition(Vector2 worldPosition)
    {
        if (_data == null)
        {
            return Vector2.Zero;
        }

        return _data.GetRoomIndexByWorldPosition(worldPosition);
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
        if (_data == null)
        {
            return false;
        }

        return _interactionSystem.IsAtRoomExit(_data, worldPosition, roomIndex, ExitTriggerRadius);
    }

    public bool TryGetPortalDestination(Vector2 worldPosition, out Vector2 destinationWorld)
    {
        if (_data == null)
        {
            destinationWorld = Vector2.Zero;
            return false;
        }

        return _interactionSystem.TryGetPortalDestination(_data, worldPosition, out destinationWorld);
    }

    public bool CanMoveTo(Rect2 worldRect)
    {
        if (_data == null)
        {
            return false;
        }

        return _collisionSystem.CanMoveTo(_data, worldRect);
    }

    public bool IsWalkableTile(int tileX, int tileY)
    {
        if (_data == null)
        {
            return false;
        }

        return _collisionSystem.IsWalkableTile(_data, tileX, tileY);
    }

    public bool HasNearbyBox(Vector2 worldPosition, out Vector2 boxWorld, float maxDistanceTiles = 1.15f)
    {
        if (_data == null)
        {
            boxWorld = Vector2.Zero;
            return false;
        }

        return _interactionSystem.HasNearbyBox(_data, worldPosition, out boxWorld, maxDistanceTiles);
    }

    public bool TryOpenNearbyBox(Vector2 worldPosition, float maxDistanceTiles = 1.15f)
    {
        if (_data == null)
        {
            return false;
        }

        if (_interactionSystem.TryOpenNearbyBox(_data, worldPosition, out Vector2 openedWorld, maxDistanceTiles))
        {
            // notify listeners that a box opened at this world position
            EmitSignal("BoxOpened", openedWorld);
            Update();
            return true;
        }

        return false;
    }

    public override void _Draw()
    {
        if (_data == null)
        {
            return;
        }

        _renderSystem.Draw(this, _data);
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

    private MapGenerationConfig BuildGenerationConfig()
    {
        return new MapGenerationConfig
        {
            TileSize = GenerationConfig.TileSize,
            RoomTileSize = GenerationConfig.RoomTileSize,
            RoomsX = GenerationConfig.RoomsX,
            RoomsY = GenerationConfig.RoomsY,
            Seed = GenerationConfig.Seed,
            StartExitInsetTiles = GenerationConfig.StartExitInsetTiles,
            BorderWallThicknessRatio = GenerationConfig.BorderWallThicknessRatio,
            ObstacleFillRate = GenerationConfig.ObstacleFillRate,
            PortalPairChance = GenerationConfig.PortalPairChance,
            ExitTriggerRadius = GenerationConfig.ExitTriggerRadius,
            HazardClusterMinTiles = GenerationConfig.HazardClusterMinTiles,
            HazardClusterMaxTiles = GenerationConfig.HazardClusterMaxTiles,
            BoxCountMin = GenerationConfig.BoxCountMin,
            BoxCountMax = GenerationConfig.BoxCountMax,
            BoxClosedTexturePath = GenerationConfig.BoxClosedTexturePath,
            BoxOpenTexturePath = GenerationConfig.BoxOpenTexturePath
        };
    }
}