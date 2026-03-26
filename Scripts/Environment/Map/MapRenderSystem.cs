using Godot;

public class MapRenderSystem
{
    private ImageTexture _mapTexture;
    private Texture _boxClosedTexture;
    private Texture _boxOpenTexture;

    public void Rebuild(MapTileData data, string boxClosedTexturePath, string boxOpenTexturePath)
    {
        BuildMapTexture(data);
        LoadBoxTextures(boxClosedTexturePath, boxOpenTexturePath);
        RebuildBoxTileList(data);
    }

    public void Draw(Node2D node, MapTileData data)
    {
        if (_mapTexture == null)
        {
            return;
        }

        node.DrawTextureRect(_mapTexture, new Rect2(Vector2.Zero, new Vector2(data.WorldPixelWidth, data.WorldPixelHeight)), false);
        DrawBoxes(node, data);
    }

    private void BuildMapTexture(MapTileData data)
    {
        var image = new Image();
        image.Create(data.WorldTileWidth, data.WorldTileHeight, false, Image.Format.Rgba8);
        image.Lock();
        for (int x = 0; x < data.WorldTileWidth; x++)
        {
            for (int y = 0; y < data.WorldTileHeight; y++)
            {
                image.SetPixel(x, y, GetTileColor(data, x, y));
            }
        }
        image.Unlock();
        _mapTexture = new ImageTexture();
        _mapTexture.CreateFromImage(image, 0);
    }

    private Color GetTileColor(MapTileData data, int tileX, int tileY)
    {
        switch (data.Tiles[tileX, tileY])
        {
            case MapTileType.Start: return GameConstants.MapColors.RoomStart;
            case MapTileType.Exit: return GameConstants.MapColors.RoomExit;
            case MapTileType.Wall: return GameConstants.MapColors.Wall;
            case MapTileType.Box: return GameConstants.MapColors.Box;
            case MapTileType.Portal: return GameConstants.MapColors.Portal;
            case MapTileType.Lava: return GameConstants.MapColors.Lava;
            case MapTileType.Water: return GameConstants.MapColors.Water;
            default: return GetScenarioFloorColor(GetScenarioByTile(data, tileX, tileY));
        }
    }

    private Color GetScenarioFloorColor(MapScenarioType scenario)
    {
        switch (scenario)
        {
            case MapScenarioType.Grassland: return GameConstants.MapColors.ScenarioGrassland;
            case MapScenarioType.Mountain: return GameConstants.MapColors.ScenarioMountain;
            case MapScenarioType.Lava: return GameConstants.MapColors.ScenarioLava;
            default: return GameConstants.MapColors.ScenarioSea;
        }
    }

    private MapScenarioType GetScenarioByTile(MapTileData data, int tileX, int tileY)
    {
        int roomX = Mathf.Clamp(tileX / data.RoomTileSize, 0, data.RoomsX - 1);
        int roomY = Mathf.Clamp(tileY / data.RoomTileSize, 0, data.RoomsY - 1);
        return data.RoomScenarios[roomX, roomY];
    }

    private void LoadBoxTextures(string boxClosedTexturePath, string boxOpenTexturePath)
    {
        _boxClosedTexture = GD.Load<Texture>(boxClosedTexturePath);
        _boxOpenTexture = GD.Load<Texture>(boxOpenTexturePath);

        if (_boxClosedTexture == null || _boxOpenTexture == null)
        {
            GD.Print("MapRenderSystem: box textures not found, using fallback box color.");
        }
    }

    private void RebuildBoxTileList(MapTileData data)
    {
        data.BoxTiles.Clear();
        for (int x = 0; x < data.WorldTileWidth; x++)
        {
            for (int y = 0; y < data.WorldTileHeight; y++)
            {
                if (data.Tiles[x, y] == MapTileType.Box)
                {
                    data.BoxTiles.Add(new Vector2(x, y));
                }
            }
        }
    }

    private void DrawBoxes(Node2D node, MapTileData data)
    {
        if (data.BoxTiles.Count == 0 || _boxClosedTexture == null || _boxOpenTexture == null)
        {
            return;
        }

        for (int i = 0; i < data.BoxTiles.Count; i++)
        {
            Vector2 tile = data.BoxTiles[i];
            int tx = (int)tile.x;
            int ty = (int)tile.y;
            Texture texture = data.OpenedBoxes[tx, ty] ? _boxOpenTexture : _boxClosedTexture;
            Rect2 tileRect = new Rect2(tx * data.TileSize, ty * data.TileSize, data.TileSize, data.TileSize);
            node.DrawTextureRect(texture, tileRect, false);
        }
    }
}