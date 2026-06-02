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
        private const int BORDER_THICKNESS = 1;  // 1-tile thick walls around the edge

        public override void _Ready()
        {
            GD.Print("[LobbyMap] Creating static lobby...");

            // Set static configuration
            TileSize = 24;
            RoomTileSize = LOBBY_SIZE;
            RoomsX = 1;
            RoomsY = 1;
            DisableRoomExits = true;  // Prevent any auto-teleportation
            CombatEnabled = false;

            // Manually create the lobby (no procedural generation)
            CreateStaticLobby();

            GD.Print("[LobbyMap] Static lobby ready: " + WorldPixelWidth + "x" + WorldPixelHeight + "px");
        }

        private void CreateStaticLobby()
        {
            // Create tile data manually - 30x30 tiles
            var tileData = new MapTileData(TileSize, RoomTileSize, RoomsX, RoomsY);

            // Set lobby scenario to Lobby
            tileData.RoomScenarios[0, 0] = MapScenarioType.Lobby;

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

            // Corner pillars in muted gray (Portal tiles) for the carpet background
            AddDecorativeColumn(tileData, 5, 5, 2, MapTileType.Portal);
            AddDecorativeColumn(tileData, LOBBY_SIZE - 6, 5, 2, MapTileType.Portal);
            AddDecorativeColumn(tileData, 5, LOBBY_SIZE - 6, 2, MapTileType.Portal);
            AddDecorativeColumn(tileData, LOBBY_SIZE - 6, LOBBY_SIZE - 6, 2, MapTileType.Portal);

            // NPC floors (3x3 blocks = radius 1)
            AddDecorativeColumn(tileData, 7, 11, 1, MapTileType.NPCFloor);   // Previous Hero
            AddDecorativeColumn(tileData, 15, 11, 1, MapTileType.NPCFloor);  // Trader
            AddDecorativeColumn(tileData, 23, 11, 1, MapTileType.NPCFloor);  // Poet
            AddDecorativeColumn(tileData, 15, 23, 1, MapTileType.NPCFloor);  // Blacksmith

            // Decorative box features (originally water)
            AddDecorativeWallSection(tileData, 8, 3, 4, 1, MapTileType.Wall);
            AddDecorativeWallSection(tileData, LOBBY_SIZE - 12, 3, 4, 1, MapTileType.Wall);
            AddDecorativeWallSection(tileData, 8, LOBBY_SIZE - 4, 4, 1, MapTileType.Wall);
            AddDecorativeWallSection(tileData, LOBBY_SIZE - 12, LOBBY_SIZE - 4, 4, 1, MapTileType.Wall);

            // Add some mid-wall box decorations
            AddDecorativeColumn(tileData, 3, 15, 1, MapTileType.Wall);
            AddDecorativeColumn(tileData, LOBBY_SIZE - 4, 15, 1, MapTileType.Wall);

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

            // Add single carpet sprites to fill the corner areas
            AddCarpetSprites();

            // Add desks and mark their tiles as solid
            AddDeskSprites(tileData);

            GD.Print("[LobbyMap] Static lobby created: " + LOBBY_SIZE + "x" + LOBBY_SIZE + " tiles, spawn at " + centerTile);
            Update();  // Trigger redraw
        }

        private void AddCarpetSprites()
        {
            AddCarpetSprite(5, 5);
            AddCarpetSprite(LOBBY_SIZE - 6, 5);
            AddCarpetSprite(5, LOBBY_SIZE - 6);
            AddCarpetSprite(LOBBY_SIZE - 6, LOBBY_SIZE - 6);
        }

        private void AddCarpetSprite(int tileX, int tileY)
        {
            var carpet = new Sprite
            {
                Texture = GD.Load<Texture>("res://Assets/Lobby/lobby-carpet.png")
            };
            // 5x5 tiles = 120x120 pixels. Texture is 256x256.
            // Scale = 120 / 256 = 0.46875
            carpet.Scale = new Vector2(0.46875f, 0.46875f);
            carpet.Position = new Vector2(tileX * TileSize + TileSize / 2f, tileY * TileSize + TileSize / 2f);
            AddChild(carpet);
        }

        private void AddDeskSprites(MapTileData tileData)
        {
            AddDeskSprite(tileData, 7, 18);
            AddDeskSprite(tileData, 23, 18);
        }

        private void AddDeskSprite(MapTileData tileData, int tileX, int tileY)
        {
            var desk = new Sprite
            {
                Texture = GD.Load<Texture>("res://Assets/Lobby/lobby-desk.png")
            };
            // Desk texture might be 256x256, scale it appropriately.
            // A 3x3 tile size is ~72px, so scale = 72 / 256 = 0.28125
            desk.Scale = new Vector2(0.28f, 0.28f);
            desk.Position = new Vector2(tileX * TileSize + TileSize / 2f, tileY * TileSize + TileSize / 2f);
            AddChild(desk);

            // Mark the bottom area under the desk as solid (half height collision)
            for (int x = tileX - 1; x <= tileX + 1; x++)
            {
                for (int y = tileY; y <= tileY + 1; y++)
                {
                    if (x >= 0 && x < LOBBY_SIZE && y >= 0 && y < LOBBY_SIZE)
                    {
                        tileData.Tiles[x, y] = MapTileType.Solid;
                    }
                }
            }
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