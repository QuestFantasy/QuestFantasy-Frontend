using Godot;

public class MapCollisionSystem
{
    public bool CanMoveTo(MapTileData data, Rect2 worldRect)
    {
        int minTileX = Mathf.FloorToInt(worldRect.Position.x / data.TileSize);
        int minTileY = Mathf.FloorToInt(worldRect.Position.y / data.TileSize);
        int maxTileX = Mathf.FloorToInt((worldRect.Position.x + worldRect.Size.x - 1f) / data.TileSize);
        int maxTileY = Mathf.FloorToInt((worldRect.Position.y + worldRect.Size.y - 1f) / data.TileSize);

        for (int x = minTileX; x <= maxTileX; x++)
        {
            for (int y = minTileY; y <= maxTileY; y++)
            {
                if (!IsWalkableTile(data, x, y))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool IsWalkableTile(MapTileData data, int tileX, int tileY)
    {
        if (tileX < 0 || tileY < 0 || tileX >= data.WorldTileWidth || tileY >= data.WorldTileHeight)
        {
            return false;
        }

        MapTileType tileType = data.Tiles[tileX, tileY];
        return tileType == MapTileType.Floor
            || tileType == MapTileType.Start
            || tileType == MapTileType.Exit
            || tileType == MapTileType.Portal;
    }
}
