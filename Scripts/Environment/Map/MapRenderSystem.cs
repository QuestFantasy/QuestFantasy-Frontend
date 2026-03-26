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
            case MapTileType.Start: return new Color(0.95f, 0.88f, 0.24f);
            case MapTileType.Exit: return new Color(0.62f, 0.24f, 0.87f);
            case MapTileType.Wall: return new Color(0.12f, 0.12f, 0.15f);
            case MapTileType.Box: return new Color(0.54f, 0.34f, 0.15f);
            case MapTileType.Portal: return new Color(0.88f, 0.26f, 0.80f);
            case MapTileType.Lava: return new Color(0.88f, 0.34f, 0.12f);
            case MapTileType.Water: return new Color(0.20f, 0.50f, 0.86f);
            default: return GetScenarioFloorColor(GetScenarioByTile(data, tileX, tileY));
        }
    }

    private Color GetScenarioFloorColor(MapScenarioType scenario)
    {
        switch (scenario)
        {
            case MapScenarioType.Grassland: return new Color(0.40f, 0.66f, 0.36f);
            case MapScenarioType.Mountain: return new Color(0.50f, 0.52f, 0.55f);
            case MapScenarioType.Lava: return new Color(0.46f, 0.28f, 0.18f);
            default: return new Color(0.34f, 0.56f, 0.74f);
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
