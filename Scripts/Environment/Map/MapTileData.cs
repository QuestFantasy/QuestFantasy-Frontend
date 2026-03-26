using Godot;
using System.Collections.Generic;

public class MapTileData
{
    public int TileSize { get; }
    public int RoomTileSize { get; }
    public int RoomsX { get; }
    public int RoomsY { get; }

    public int WorldTileWidth => RoomsX * RoomTileSize;
    public int WorldTileHeight => RoomsY * RoomTileSize;
    public int WorldPixelWidth => WorldTileWidth * TileSize;
    public int WorldPixelHeight => WorldTileHeight * TileSize;

    public MapTileType[,] Tiles { get; }
    public bool[,] ProtectedPath { get; }
    public bool[,] OpenedBoxes { get; }
    public MapScenarioType[,] RoomScenarios { get; }
    public Vector2[,] RoomStartTiles { get; }
    public Vector2[,] RoomExitTiles { get; }
    public Dictionary<string, Vector2> PortalLinks { get; } = new Dictionary<string, Vector2>();
    public List<Vector2> BoxTiles { get; } = new List<Vector2>();

    public MapTileData(int tileSize, int roomTileSize, int roomsX, int roomsY)
    {
        TileSize = tileSize;
        RoomTileSize = roomTileSize;
        RoomsX = roomsX;
        RoomsY = roomsY;

        Tiles = new MapTileType[WorldTileWidth, WorldTileHeight];
        ProtectedPath = new bool[WorldTileWidth, WorldTileHeight];
        OpenedBoxes = new bool[WorldTileWidth, WorldTileHeight];
        RoomScenarios = new MapScenarioType[RoomsX, RoomsY];
        RoomStartTiles = new Vector2[RoomsX, RoomsY];
        RoomExitTiles = new Vector2[RoomsX, RoomsY];
    }

    public void GetRoomTileBounds(int roomX, int roomY, out int sx, out int sy, out int ex, out int ey)
    {
        sx = roomX * RoomTileSize;
        sy = roomY * RoomTileSize;
        ex = sx + RoomTileSize - 1;
        ey = sy + RoomTileSize - 1;
    }

    public void SetTile(int x, int y, MapTileType tileType)
    {
        if (x < 0 || y < 0 || x >= WorldTileWidth || y >= WorldTileHeight)
        {
            return;
        }

        Tiles[x, y] = tileType;
    }

    public Vector2 WorldToTile(Vector2 worldPosition)
    {
        int tileX = Mathf.Clamp(Mathf.FloorToInt(worldPosition.x / TileSize), 0, WorldTileWidth - 1);
        int tileY = Mathf.Clamp(Mathf.FloorToInt(worldPosition.y / TileSize), 0, WorldTileHeight - 1);
        return new Vector2(tileX, tileY);
    }

    public Vector2 TileToWorldCenter(int tileX, int tileY)
    {
        return new Vector2((tileX + 0.5f) * TileSize, (tileY + 0.5f) * TileSize);
    }

    public Vector2 GetRoomIndexByWorldPosition(Vector2 worldPosition)
    {
        int tileX = Mathf.Clamp(Mathf.FloorToInt(worldPosition.x / TileSize), 0, WorldTileWidth - 1);
        int tileY = Mathf.Clamp(Mathf.FloorToInt(worldPosition.y / TileSize), 0, WorldTileHeight - 1);
        return new Vector2(tileX / RoomTileSize, tileY / RoomTileSize);
    }

    public string TileKey(int tileX, int tileY)
    {
        return tileX.ToString() + ":" + tileY.ToString();
    }
}
