using Godot;

namespace QuestFantasy.Environment
{
    /// <summary>
    /// A simple static lobby map - a calm hub area for players to wait and select difficulty.
    /// Completely manual creation - NO procedural generation. Just floor and border walls.
    /// </summary>
    public class LobbyMap : Map
    {
        private const int LOBBY_SIZE = 30;  // 30x30 tiles = 720x720 pixels (24px per tile)
        private const int BORDER_THICKNESS = 3;  // 3-tile thick walls around the edge

        public override void _Ready()
        {
            GD.Print("[LobbyMap] Creating static lobby...");

            // Set static configuration
            TileSize = 24;
            RoomTileSize = LOBBY_SIZE;
            RoomsX = 1;
            RoomsY = 1;
            DisableRoomExits = true;  // Prevent any auto-teleportation

            // Manually create the lobby (no procedural generation)
            CreateStaticLobby();

            GD.Print("[LobbyMap] Static lobby ready: " + WorldPixelWidth + "x" + WorldPixelHeight + "px");
        }

        private void CreateStaticLobby()
        {
            // Create tile data manually - 30x30 tiles
            var tileData = new MapTileData(TileSize, RoomTileSize, RoomsX, RoomsY);

            // Set lobby scenario to Grassland (nice green floor)
            tileData.RoomScenarios[0, 0] = MapScenarioType.Grassland;

            // Fill entire map with floor tiles, then add walls at borders
            for (int x = 0; x < LOBBY_SIZE; x++)
            {
                for (int y = 0; y < LOBBY_SIZE; y++)
                {
                    // Walls at borders
                    if (x < BORDER_THICKNESS || x >= LOBBY_SIZE - BORDER_THICKNESS ||
                        y < BORDER_THICKNESS || y >= LOBBY_SIZE - BORDER_THICKNESS)
                    {
                        tileData.Tiles[x, y] = MapTileType.Wall;
                    }
                    else
                    {
                        // Everything else is walkable floor
                        tileData.Tiles[x, y] = MapTileType.Floor;
                    }
                }
            }

            // Add decorative elements with variety
            // Corner pillars in muted gray (Portal tiles)
            AddDecorativeColumn(tileData, 5, 5, 2, MapTileType.Portal);
            AddDecorativeColumn(tileData, LOBBY_SIZE - 6, 5, 2, MapTileType.Portal);
            AddDecorativeColumn(tileData, 5, LOBBY_SIZE - 6, 2, MapTileType.Portal);
            AddDecorativeColumn(tileData, LOBBY_SIZE - 6, LOBBY_SIZE - 6, 2, MapTileType.Portal);

            // Decorative water features (blue tiles)
            AddDecorativeWallSection(tileData, 8, 3, 4, 1, MapTileType.Water);
            AddDecorativeWallSection(tileData, LOBBY_SIZE - 12, 3, 4, 1, MapTileType.Water);
            AddDecorativeWallSection(tileData, 8, LOBBY_SIZE - 4, 4, 1, MapTileType.Water);
            AddDecorativeWallSection(tileData, LOBBY_SIZE - 12, LOBBY_SIZE - 4, 4, 1, MapTileType.Water);

            // Decorative lava features (orange tiles) for variety
            AddDecorativeWallSection(tileData, 15, 3, 2, 1, MapTileType.Lava);
            AddDecorativeWallSection(tileData, LOBBY_SIZE - 17, 3, 2, 1, MapTileType.Lava);
            AddDecorativeWallSection(tileData, 15, LOBBY_SIZE - 4, 2, 1, MapTileType.Lava);
            AddDecorativeWallSection(tileData, LOBBY_SIZE - 17, LOBBY_SIZE - 4, 2, 1, MapTileType.Lava);

            // Add some mid-wall decorations
            AddDecorativeColumn(tileData, 3, 15, 1, MapTileType.Water);
            AddDecorativeColumn(tileData, LOBBY_SIZE - 4, 15, 1, MapTileType.Water);
            AddDecorativeColumn(tileData, 15, 3, 1, MapTileType.Lava);
            AddDecorativeColumn(tileData, 15, LOBBY_SIZE - 4, 1, MapTileType.Lava);

            // Set spawn point at the center of the lobby
            Vector2 centerTile = new Vector2(LOBBY_SIZE / 2, LOBBY_SIZE / 2);
            tileData.RoomStartTiles[0, 0] = centerTile;
            tileData.RoomExitTiles[0, 0] = centerTile;

            // Set the tile data directly on the parent Map class using reflection
            var fieldInfo = typeof(Map).GetField("_data",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo?.SetValue(this, tileData);

            // Rebuild the render system to display the colors
            var renderFieldInfo = typeof(Map).GetField("_renderSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (renderFieldInfo != null)
            {
                var renderSystem = renderFieldInfo.GetValue(this);
                if (renderSystem != null)
                {
                    var rebuildMethod = renderSystem.GetType().GetMethod("Rebuild");
                    rebuildMethod?.Invoke(renderSystem, new object[] { tileData, "res://Assets/Box/Box_Closed.png", "res://Assets/Box/Box_Open.png" });
                }
            }

            GD.Print("[LobbyMap] Static lobby created: " + LOBBY_SIZE + "x" + LOBBY_SIZE + " tiles, spawn at " + centerTile);
            Update();  // Trigger redraw
        }

        private void AddDecorativeColumn(MapTileData tileData, int centerX, int centerY, int radius, MapTileType tileType)
        {
            // Create a decorative pillar/column using specified tile type
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x >= 0 && x < LOBBY_SIZE && y >= 0 && y < LOBBY_SIZE)
                    {
                        tileData.Tiles[x, y] = tileType;
                    }
                }
            }
        }

        private void AddDecorativeWallSection(MapTileData tileData, int startX, int startY, int width, int height, MapTileType tileType)
        {
            // Add a decorative wall section using specified tile type
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    if (x >= 0 && x < LOBBY_SIZE && y >= 0 && y < LOBBY_SIZE)
                    {
                        tileData.Tiles[x, y] = tileType;
                    }
                }
            }
        }
    }
}