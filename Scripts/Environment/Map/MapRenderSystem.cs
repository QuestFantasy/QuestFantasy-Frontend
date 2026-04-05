using Godot;

public class MapRenderSystem
{
    private ImageTexture _mapTexture;
    private Texture _boxClosedTexture;
    private Texture _boxOpenTexture;
    private Texture _exitInTexture;
    private Texture _exitOutTexture;
    private Texture _teleportInTexture;
    private Texture _teleportOutTexture;
    private MapTileData _cachedData; // Cache to prevent unnecessary rebuilds
    private string _cachedClosedPath;
    private string _cachedOpenPath;

    public void Rebuild(MapTileData data, string boxClosedTexturePath, string boxOpenTexturePath)
    {
        // Check if we can skip rebuild
        if (_cachedData == data &&
            _cachedClosedPath == boxClosedTexturePath &&
            _cachedOpenPath == boxOpenTexturePath &&
            _mapTexture != null)
        {
            return; // Already built with same parameters
        }

        _cachedData = data;
        _cachedClosedPath = boxClosedTexturePath;
        _cachedOpenPath = boxOpenTexturePath;

        BuildMapTexture(data);
        LoadBoxTextures(boxClosedTexturePath, boxOpenTexturePath);
        LoadExitAndTeleportTextures();
        RebuildBoxTileList(data);
    }

    public void Draw(Node2D node, MapTileData data)
    {
        if (_mapTexture == null)
        {
            return;
        }

        node.DrawTextureRect(_mapTexture, new Rect2(Vector2.Zero, new Vector2(data.WorldPixelWidth, data.WorldPixelHeight)), false);
        DrawPortals(node, data);
        DrawBoxes(node, data);
        DrawStartAndExitTiles(node, data);
    }

    private void BuildMapTexture(MapTileData data)
    {
        // Use Image.Format.RGBA8 for better performance than unlocked pixel access
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

        // Recreate texture with mipmap filter for better performance
        if (_mapTexture == null)
        {
            _mapTexture = new ImageTexture();
        }

        _mapTexture.CreateFromImage(image, (int)ImageTexture.FlagsEnum.Mipmaps);
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

    private void LoadExitAndTeleportTextures()
    {
        // Load exit textures
        _exitInTexture = GD.Load<Texture>("res://Assets/Teleport/exit_in.png");
        _exitOutTexture = GD.Load<Texture>("res://Assets/Teleport/exit_out.png");

        // Load teleport (portal) textures
        _teleportInTexture = GD.Load<Texture>("res://Assets/Teleport/teleport_in.png");
        _teleportOutTexture = GD.Load<Texture>("res://Assets/Teleport/teleport_out.png");

        if ((_exitInTexture == null || _exitOutTexture == null) && (_teleportInTexture == null || _teleportOutTexture == null))
        {
            GD.Print("MapRenderSystem: some teleport/exit textures not found, falling back to colors.");
        }
    }

    private void DrawPortals(Node2D node, MapTileData data)
    {
        if (_teleportInTexture == null || _teleportOutTexture == null)
        {
            return; // textures missing, rely on color rendering
        }

        var drawn = new System.Collections.Generic.HashSet<string>();

        foreach (var kvp in data.PortalLinks)
        {
            // kvp.Key is "x:y"
            var parts = kvp.Key.Split(':');
            if (parts.Length != 2)
            {
                continue;
            }
            if (!int.TryParse(parts[0], out int sx) || !int.TryParse(parts[1], out int sy))
            {
                continue;
            }
            Vector2 dest = kvp.Value;
            int dx = (int)dest.x;
            int dy = (int)dest.y;

            string sKey = sx + ":" + sy;
            string dKey = dx + ":" + dy;

            if (!drawn.Contains(sKey))
            {
                Rect2 srcRect = new Rect2(new Vector2(sx * data.TileSize, sy * data.TileSize), new Vector2(data.TileSize, data.TileSize));
                node.DrawTextureRect(_teleportOutTexture, srcRect, false);
                drawn.Add(sKey);
            }

            if (!drawn.Contains(dKey))
            {
                Rect2 dstRect = new Rect2(new Vector2(dx * data.TileSize, dy * data.TileSize), new Vector2(data.TileSize, data.TileSize));
                node.DrawTextureRect(_teleportInTexture, dstRect, false);
                drawn.Add(dKey);
            }
        }
    }

    private void DrawStartAndExitTiles(Node2D node, MapTileData data)
    {
        if ((_exitInTexture == null || _exitOutTexture == null))
        {
            return; // textures missing, keep color-based rendering
        }

        for (int x = 0; x < data.WorldTileWidth; x++)
        {
            for (int y = 0; y < data.WorldTileHeight; y++)
            {
                MapTileType tile = data.Tiles[x, y];
                if (tile != MapTileType.Start && tile != MapTileType.Exit)
                {
                    continue;
                }

                Texture tex = tile == MapTileType.Start ? _exitOutTexture : _exitInTexture;
                Vector2 worldPos = new Vector2(x * data.TileSize, y * data.TileSize);
                Rect2 destRect = new Rect2(worldPos, new Vector2(data.TileSize, data.TileSize));
                node.DrawTextureRect(tex, destRect, false);
            }
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
        if (_boxClosedTexture == null || _boxOpenTexture == null)
        {
            DrawBoxesFallback(node, data);
            return;
        }

        foreach (Vector2 boxTile in data.BoxTiles)
        {
            int tileX = (int)boxTile.x;
            int tileY = (int)boxTile.y;

            Texture textureToUse = data.OpenedBoxes[tileX, tileY] ? _boxOpenTexture : _boxClosedTexture;

            // Calculate world position (top-left of tile)
            Vector2 worldPos = new Vector2(tileX * data.TileSize, tileY * data.TileSize);

            // Scale texture to fill the entire tile
            Vector2 boxSize = new Vector2(data.TileSize, data.TileSize);
            Rect2 destRect = new Rect2(worldPos, boxSize);

            node.DrawTextureRect(textureToUse, destRect, false);
        }
    }

    private void DrawBoxesFallback(Node2D node, MapTileData data)
    {
        foreach (Vector2 boxTile in data.BoxTiles)
        {
            int tileX = (int)boxTile.x;
            int tileY = (int)boxTile.y;

            Color boxColor = data.OpenedBoxes[tileX, tileY]
                ? new Color(0.7f, 0.7f, 0.7f)  // Gray for opened boxes
                : GameConstants.MapColors.Box;

            // Draw a small box in the tile center
            Vector2 worldCenter = data.TileToWorldCenter(tileX, tileY);
            Vector2 boxSize = new Vector2(data.TileSize * 0.8f, data.TileSize * 0.8f);
            Rect2 boxRect = new Rect2(worldCenter - boxSize / 2f, boxSize);

            node.DrawRect(boxRect, boxColor);
            node.DrawRect(boxRect.Grow(-1), new Color(0, 0, 0, 0.5f), false, 1f);
        }
    }
}