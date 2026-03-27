using System.Collections.Generic;

using Godot;

public class MapGenerator
{
    private RandomNumberGenerator _random;
    private MapGenerationConfig _config;

    public void Generate(
        MapTileData data,
        RandomNumberGenerator random,
        MapGenerationConfig config)
    {
        _random = random;
        _config = config;

        GenerateScenarioGrid(data);
        FillBaseFloor(data);
        BuildRoomWalls(data);
        GenerateRoomStartAndExitTiles(data);
        CarveGuaranteedRandomPaths(data);
        PlaceRoomObjects(data);
        CreatePortalPairs(data);
        EnsureCriticalTiles(data);
    }

    private void GenerateScenarioGrid(MapTileData data)
    {
        for (int roomX = 0; roomX < data.RoomsX; roomX++)
        {
            for (int roomY = 0; roomY < data.RoomsY; roomY++)
            {
                data.RoomScenarios[roomX, roomY] = (MapScenarioType)_random.RandiRange(0, 3);
            }
        }
    }

    private void FillBaseFloor(MapTileData data)
    {
        data.PortalLinks.Clear();
        data.BoxTiles.Clear();
        for (int x = 0; x < data.WorldTileWidth; x++)
        {
            for (int y = 0; y < data.WorldTileHeight; y++)
            {
                data.Tiles[x, y] = MapTileType.Floor;
                data.ProtectedPath[x, y] = false;
                data.OpenedBoxes[x, y] = false;
            }
        }
    }

    private void BuildRoomWalls(MapTileData data)
    {
        int wallThickness = Mathf.Max(1, Mathf.FloorToInt(data.RoomTileSize * _config.BorderWallThicknessRatio));

        for (int roomX = 0; roomX < data.RoomsX; roomX++)
        {
            for (int roomY = 0; roomY < data.RoomsY; roomY++)
            {
                data.GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
                for (int t = 0; t < wallThickness; t++)
                {
                    for (int x = sx; x <= ex; x++)
                    {
                        data.SetTile(x, sy + t, MapTileType.Wall);
                        data.SetTile(x, ey - t, MapTileType.Wall);
                    }
                    for (int y = sy; y <= ey; y++)
                    {
                        data.SetTile(sx + t, y, MapTileType.Wall);
                        data.SetTile(ex - t, y, MapTileType.Wall);
                    }
                }
            }
        }
    }

    private void GenerateRoomStartAndExitTiles(MapTileData data)
    {
        int inset = Mathf.Clamp(_config.StartExitInsetTiles, 3, Mathf.Max(3, data.RoomTileSize / 3));
        for (int roomX = 0; roomX < data.RoomsX; roomX++)
        {
            for (int roomY = 0; roomY < data.RoomsY; roomY++)
            {
                int startSide = _random.RandiRange(0, 3);
                int exitSide = (startSide + _random.RandiRange(1, 3)) % 4;
                // BuildEdgePoint keeps points on room edges (entry/exit feel) while the inset avoids corners/walls.
                Vector2 start = BuildEdgePoint(data, roomX, roomY, startSide, inset);
                Vector2 exit = BuildEdgePoint(data, roomX, roomY, exitSide, inset);
                if (start.DistanceTo(exit) < data.RoomTileSize * 0.35f)
                {
                    // If points are too close, force the exit toward the opposite side for stronger traversal.
                    exit = BuildEdgePoint(data, roomX, roomY, (startSide + 2) % 4, inset);
                }
                data.RoomStartTiles[roomX, roomY] = start;
                data.RoomExitTiles[roomX, roomY] = exit;
            }
        }
    }

    private void CarveGuaranteedRandomPaths(MapTileData data)
    {
        for (int roomX = 0; roomX < data.RoomsX; roomX++)
        {
            for (int roomY = 0; roomY < data.RoomsY; roomY++)
            {
                Vector2 start = data.RoomStartTiles[roomX, roomY];
                Vector2 exit = data.RoomExitTiles[roomX, roomY];
                CarveRandomPathInRoom(data, roomX, roomY, (int)start.x, (int)start.y, (int)exit.x, (int)exit.y, 1);
            }
        }
    }

    private void CarveRandomPathInRoom(MapTileData data, int roomX, int roomY, int startX, int startY, int exitX, int exitY, int halfWidth)
    {
        data.GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
        int minX = sx + 1;
        int minY = sy + 1;
        int maxX = ex - 1;
        int maxY = ey - 1;

        int currentX = startX;
        int currentY = startY;
        var visited = new HashSet<string>();
        int maxSteps = data.RoomTileSize * data.RoomTileSize * 2;

        for (int step = 0; step < maxSteps; step++)
        {
            CarveDisk(data, currentX, currentY, halfWidth);
            visited.Add(data.TileKey(currentX, currentY));
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

            for (int i = 0; i < candidates.Length; i++)
            {
                int nx = (int)candidates[i].x;
                int ny = (int)candidates[i].y;
                if (nx < minX || ny < minY || nx > maxX || ny > maxY)
                {
                    continue;
                }

                float distance = Mathf.Abs(nx - exitX) + Mathf.Abs(ny - exitY);
                float jitter = _random.RandfRange(0f, 4f);
                float revisit = visited.Contains(data.TileKey(nx, ny)) ? 6f : 0f;
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
            CarveCorridor(data, startX, startY, exitX, exitY, halfWidth);
        }

        CarveDisk(data, startX, startY, 2);
        CarveDisk(data, exitX, exitY, 2);
    }

    private void PlaceRoomObjects(MapTileData data)
    {
        for (int roomX = 0; roomX < data.RoomsX; roomX++)
        {
            for (int roomY = 0; roomY < data.RoomsY; roomY++)
            {
                data.GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
                MapScenarioType scenario = data.RoomScenarios[roomX, roomY];

                // First pass: place walls only
                for (int x = sx + 1; x < ex; x++)
                {
                    for (int y = sy + 1; y < ey; y++)
                    {
                        if (data.ProtectedPath[x, y] || data.Tiles[x, y] != MapTileType.Floor)
                        {
                            continue;
                        }
                        if (_random.Randf() > _config.ObstacleFillRate)
                        {
                            continue;
                        }
                        // Only place walls, not boxes
                        MapTileType obstacle = PickSolidObstacleForScenario(scenario);
                        if (obstacle == MapTileType.Wall)
                        {
                            data.Tiles[x, y] = MapTileType.Wall;
                        }
                    }
                }

                // Second pass: place boxes with fixed count
                PlaceBoxesInRoom(data, roomX, roomY);
                PlaceHazardClustersForRoom(data, roomX, roomY, scenario);
            }
        }
    }

    private void PlaceBoxesInRoom(MapTileData data, int roomX, int roomY)
    {
        int boxCount = _random.RandiRange(_config.BoxCountMin, _config.BoxCountMax);
        int placed = 0;
        int attempts = 0;
        int maxAttempts = boxCount * 10; // Prevent infinite loop

        data.GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);

        while (placed < boxCount && attempts < maxAttempts)
        {
            attempts++;
            int x = _random.RandiRange(sx + 1, ex - 1);
            int y = _random.RandiRange(sy + 1, ey - 1);

            // Can only place box on unprotected floor tiles
            if (!data.ProtectedPath[x, y] && data.Tiles[x, y] == MapTileType.Floor)
            {
                data.SetTile(x, y, MapTileType.Box);
                placed++;
            }
        }
    }


    private void PlaceHazardClustersForRoom(MapTileData data, int roomX, int roomY, MapScenarioType scenario)
    {
        int minSize = Mathf.Clamp(_config.HazardClusterMinTiles, 1, Mathf.Max(1, _config.HazardClusterMaxTiles));
        int maxSize = Mathf.Max(minSize, _config.HazardClusterMaxTiles);

        switch (scenario)
        {
            case MapScenarioType.Lava:
                PlaceHazardClusters(data, roomX, roomY, MapTileType.Lava, _random.RandiRange(2, 4), minSize, maxSize);
                break;
            case MapScenarioType.Sea:
                PlaceHazardClusters(data, roomX, roomY, MapTileType.Water, _random.RandiRange(2, 4), minSize, maxSize);
                break;
            case MapScenarioType.Grassland:
                PlaceHazardClusters(data, roomX, roomY, MapTileType.Water, _random.RandiRange(1, 2), minSize, maxSize);
                break;
            case MapScenarioType.Mountain:
            default:
                break;
        }
    }

    private void PlaceHazardClusters(MapTileData data, int roomX, int roomY, MapTileType hazardType, int clusterCount, int minTiles, int maxTiles)
    {
        for (int cluster = 0; cluster < clusterCount; cluster++)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                int targetSize = _random.RandiRange(minTiles, maxTiles);
                if (!TryPickRoomHazardSeedTile(data, roomX, roomY, out Vector2 seed))
                {
                    continue;
                }

                bool lineShape = _random.Randf() < 0.5f;
                if (TryCreateHazardCluster(data, roomX, roomY, hazardType, seed, targetSize, minTiles, lineShape))
                {
                    break;
                }
            }
        }
    }

    private bool TryCreateHazardCluster(MapTileData data, int roomX, int roomY, MapTileType hazardType, Vector2 seed, int targetSize, int minTiles, bool lineShape)
    {
        data.GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);

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

            string key = data.TileKey((int)current.x, (int)current.y);
            if (used.Contains(key))
            {
                continue;
            }
            used.Add(key);

            int x = (int)current.x;
            int y = (int)current.y;
            if (!CanPlaceHazardAt(data, x, y, sx, sy, ex, ey))
            {
                continue;
            }

            clusterTiles.Add(new Vector2(x, y));
            List<Vector2> neighbors = GetGrowthNeighbors(current, lineShape, lineDirection);
            for (int i = 0; i < neighbors.Count; i++)
            {
                frontier.Add(neighbors[i]);
            }
        }

        if (clusterTiles.Count < minTiles)
        {
            return false;
        }

        for (int i = 0; i < clusterTiles.Count; i++)
        {
            Vector2 tile = clusterTiles[i];
            data.Tiles[(int)tile.x, (int)tile.y] = hazardType;
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

    private bool CanPlaceHazardAt(MapTileData data, int x, int y, int sx, int sy, int ex, int ey)
    {
        if (x <= sx + 1 || y <= sy + 1 || x >= ex - 1 || y >= ey - 1)
        {
            return false;
        }

        if (data.ProtectedPath[x, y])
        {
            return false;
        }

        return data.Tiles[x, y] == MapTileType.Floor;
    }

    private void CreatePortalPairs(MapTileData data)
    {
        for (int roomX = 0; roomX < data.RoomsX; roomX++)
        {
            for (int roomY = 0; roomY < data.RoomsY; roomY++)
            {
                if (_random.Randf() > _config.PortalPairChance)
                {
                    continue;
                }

                if (!TryPickRoomWalkableTile(data, roomX, roomY, out Vector2 a) || !TryPickRoomWalkableTile(data, roomX, roomY, out Vector2 b, a))
                {
                    continue;
                }

                data.SetTile((int)a.x, (int)a.y, MapTileType.Portal);
                data.SetTile((int)b.x, (int)b.y, MapTileType.Portal);
                data.PortalLinks[data.TileKey((int)a.x, (int)a.y)] = b;
                data.PortalLinks[data.TileKey((int)b.x, (int)b.y)] = a;
            }
        }
    }

    private void EnsureCriticalTiles(MapTileData data)
    {
        for (int roomX = 0; roomX < data.RoomsX; roomX++)
        {
            for (int roomY = 0; roomY < data.RoomsY; roomY++)
            {
                Vector2 start = data.RoomStartTiles[roomX, roomY];
                Vector2 exit = data.RoomExitTiles[roomX, roomY];
                data.SetTile((int)start.x, (int)start.y, MapTileType.Start);
                data.SetTile((int)exit.x, (int)exit.y, MapTileType.Exit);
            }
        }
    }

    private MapTileType PickSolidObstacleForScenario(MapScenarioType scenario)
    {
        float roll = _random.Randf();
        switch (scenario)
        {
            case MapScenarioType.Grassland:
                return roll < 0.52f ? MapTileType.Box : MapTileType.Wall;
            case MapScenarioType.Mountain:
                return roll < 0.72f ? MapTileType.Wall : MapTileType.Box;
            case MapScenarioType.Lava:
                return roll < 0.65f ? MapTileType.Wall : MapTileType.Box;
            default:
                return roll < 0.48f ? MapTileType.Box : MapTileType.Wall;
        }
    }

    private bool TryPickRoomHazardSeedTile(MapTileData data, int roomX, int roomY, out Vector2 tile)
    {
        data.GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
        for (int attempt = 0; attempt < 120; attempt++)
        {
            int x = _random.RandiRange(sx + 2, ex - 2);
            int y = _random.RandiRange(sy + 2, ey - 2);
            if (CanPlaceHazardAt(data, x, y, sx, sy, ex, ey))
            {
                tile = new Vector2(x, y);
                return true;
            }
        }

        tile = Vector2.Zero;
        return false;
    }

    private bool TryPickRoomWalkableTile(MapTileData data, int roomX, int roomY, out Vector2 tile, Vector2? avoid = null)
    {
        data.GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
        for (int attempt = 0; attempt < 80; attempt++)
        {
            int x = _random.RandiRange(sx + 2, ex - 2);
            int y = _random.RandiRange(sy + 2, ey - 2);
            if (data.ProtectedPath[x, y] || data.Tiles[x, y] != MapTileType.Floor)
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

    private Vector2 BuildEdgePoint(MapTileData data, int roomX, int roomY, int side, int inset)
    {
        // side: 0=left, 1=right, 2=top, 3=bottom; inset keeps points off corners and thick border walls.
        data.GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);
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

    private void CarveDisk(MapTileData data, int centerX, int centerY, int radius)
    {
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (x < 0 || y < 0 || x >= data.WorldTileWidth || y >= data.WorldTileHeight)
                {
                    continue;
                }
                if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= radius * radius)
                {
                    data.Tiles[x, y] = MapTileType.Floor;
                    data.ProtectedPath[x, y] = true;
                }
            }
        }
    }

    private void CarveCorridor(MapTileData data, int fromX, int fromY, int toX, int toY, int halfWidth)
    {
        for (int x = Mathf.Min(fromX, toX); x <= Mathf.Max(fromX, toX); x++)
        {
            for (int y = fromY - halfWidth; y <= fromY + halfWidth; y++)
            {
                CarveDisk(data, x, y, 0);
            }
        }
        for (int y = Mathf.Min(fromY, toY); y <= Mathf.Max(fromY, toY); y++)
        {
            for (int x = toX - halfWidth; x <= toX + halfWidth; x++)
            {
                CarveDisk(data, x, y, 0);
            }
        }
    }
}