using Godot;

using QuestFantasy.Characters;

public class MiniMapUI : CanvasLayer
{
    private const int MaxMiniMapSize = 360;
    private const int MinMiniMapSize = 220;
    private const float PanelPadding = 10f;
    private const float PanelTopOffset = 14f;

    private Map _map;
    private Player _player;

    private Control _root;
    private PanelContainer _panel;
    private TextureRect _mapTexture;
    private Control _mapContainer;
    private ColorRect _playerMarker;
    private Label _title;

    private Vector2 _lastTileIndex = new Vector2(float.MinValue, float.MinValue);
    private Vector2 _currentRoomIndex = new Vector2(float.MinValue, float.MinValue);
    private Vector2 _roomOriginTile = Vector2.Zero;
    private int _roomTileWidth;
    private int _roomTileHeight;
    private int _miniMapWidth;
    private int _miniMapHeight;
    private int _miniMapScale = 1;
    private float _displayScale = 1f;
    private int _displayWidth;
    private int _displayHeight;

    public override void _Ready()
    {
        EnsureUiReady();
    }

    public override void _Process(float delta)
    {
        if (_panel == null)
        {
            return;
        }

        if (Input.IsActionJustPressed("toggle_minimap"))
        {
            SetPanelVisible(!_panel.Visible);
        }

        if (!_panel.Visible || _map == null || _player == null)
        {
            return;
        }

        UpdatePlayerMarkerIfNeeded(false);
    }

    public void Initialize(Map map, Player player)
    {
        EnsureUiReady();

        _map = map;
        _player = player;

        if (_map != null)
        {
            _map.MapGenerated += HandleMapGenerated;
            if (_map.TileData != null)
            {
                BuildMiniMapForCurrentRoom();
            }
        }

        if (_player != null)
        {
            _player.OnRoomEntered += HandleRoomEntered;
        }

        UpdatePlayerMarkerIfNeeded(true);
    }

    public override void _ExitTree()
    {
        if (_map != null)
        {
            _map.MapGenerated -= HandleMapGenerated;
        }

        if (_player != null)
        {
            _player.OnRoomEntered -= HandleRoomEntered;
        }
    }

    private void EnsureUiReady()
    {
        if (_root != null)
        {
            return;
        }

        EnsureToggleInputAction();
        BuildUi();
        SetPanelVisible(false);
    }

    private void EnsureToggleInputAction()
    {
        if (!InputMap.HasAction("toggle_minimap"))
        {
            InputMap.AddAction("toggle_minimap");
        }

        if (InputMap.GetActionList("toggle_minimap").Count > 0)
        {
            return;
        }

        var key = new InputEventKey { Scancode = (uint)KeyList.M };
        InputMap.ActionAddEvent("toggle_minimap", key);
    }

    private void BuildUi()
    {
        _root = new Control
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_root);

        _panel = new PanelContainer
        {
            AnchorLeft = 1f,
            AnchorRight = 1f,
            AnchorTop = 0f,
            AnchorBottom = 0f,
            RectPosition = new Vector2(-260f, PanelTopOffset),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _root.AddChild(_panel);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.1f, 0.14f, 0.78f),
            BorderColor = new Color(0.25f, 0.7f, 0.5f, 0.8f),
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = PanelPadding,
            ContentMarginRight = PanelPadding,
            ContentMarginTop = PanelPadding,
            ContentMarginBottom = PanelPadding,
        };
        _panel.AddStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddConstantOverride("separation", 6);
        _panel.AddChild(vbox);

        _title = new Label
        {
            Text = "Map (M)",
            Align = Label.AlignEnum.Center,
        };
        vbox.AddChild(_title);

        _mapContainer = new Control
        {
            RectMinSize = new Vector2(MinMiniMapSize, MinMiniMapSize),
        };
        vbox.AddChild(_mapContainer);

        _mapTexture = new TextureRect
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Expand = true,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _mapContainer.AddChild(_mapTexture);

        _playerMarker = new ColorRect
        {
            Color = new Color(0.98f, 0.98f, 0.98f, 0.9f),
            RectMinSize = new Vector2(4f, 4f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _mapContainer.AddChild(_playerMarker);
    }

    private void SetPanelVisible(bool visible)
    {
        if (_panel != null)
        {
            _panel.Visible = visible;
        }
    }

    private void HandleMapGenerated(MapTileData data)
    {
        BuildMiniMapForCurrentRoom();
        UpdatePlayerMarkerIfNeeded(true);
    }

    private void HandleRoomEntered(Vector2 roomIndex, string reason)
    {
        _currentRoomIndex = roomIndex;
        BuildMiniMapForCurrentRoom();
        UpdatePlayerMarkerIfNeeded(true);
    }

    private void BuildMiniMapForCurrentRoom()
    {
        if (_map == null || _map.TileData == null || _player == null)
        {
            return;
        }

        _currentRoomIndex = GetCurrentRoomIndex();
        BuildMiniMap(_map.TileData, _currentRoomIndex);
    }

    private Vector2 GetCurrentRoomIndex()
    {
        if (_map == null || _player == null)
        {
            return Vector2.Zero;
        }

        return _map.GetRoomIndexByWorldPosition(_player.Position);
    }

    private void BuildMiniMap(MapTileData data, Vector2 roomIndex)
    {
        if (data == null || _mapTexture == null)
        {
            return;
        }

        int roomX = Mathf.Clamp((int)roomIndex.x, 0, data.RoomsX - 1);
        int roomY = Mathf.Clamp((int)roomIndex.y, 0, data.RoomsY - 1);
        data.GetRoomTileBounds(roomX, roomY, out int sx, out int sy, out int ex, out int ey);

        _roomOriginTile = new Vector2(sx, sy);
        _roomTileWidth = ex - sx + 1;
        _roomTileHeight = ey - sy + 1;

        int maxRoomSize = Mathf.Max(_roomTileWidth, _roomTileHeight);
        int targetSize = Mathf.Clamp(maxRoomSize, MinMiniMapSize, MaxMiniMapSize);
        _miniMapScale = maxRoomSize > targetSize
            ? Mathf.CeilToInt((float)maxRoomSize / targetSize)
            : 1;

        _miniMapWidth = Mathf.CeilToInt((float)_roomTileWidth / _miniMapScale);
        _miniMapHeight = Mathf.CeilToInt((float)_roomTileHeight / _miniMapScale);
        int maxMiniMapSize = Mathf.Max(_miniMapWidth, _miniMapHeight);
        _displayScale = maxMiniMapSize > 0 ? (float)targetSize / maxMiniMapSize : 1f;
        _displayWidth = Mathf.CeilToInt(_miniMapWidth * _displayScale);
        _displayHeight = Mathf.CeilToInt(_miniMapHeight * _displayScale);

        var image = new Image();
        image.Create(_miniMapWidth, _miniMapHeight, false, Image.Format.Rgba8);
        image.Lock();

        for (int x = 0; x < _miniMapWidth; x++)
        {
            for (int y = 0; y < _miniMapHeight; y++)
            {
                int tileX = Mathf.Clamp(sx + x * _miniMapScale, sx, ex);
                int tileY = Mathf.Clamp(sy + y * _miniMapScale, sy, ey);
                image.SetPixel(x, y, GetTileColor(data, tileX, tileY));
            }
        }

        image.Unlock();

        var texture = new ImageTexture();
        texture.CreateFromImage(image, (int)ImageTexture.FlagsEnum.Filter);
        _mapTexture.Texture = texture;

        if (_mapContainer != null)
        {
            _mapContainer.RectMinSize = new Vector2(_displayWidth, _displayHeight);
        }

        if (_panel != null)
        {
            _panel.RectMinSize = new Vector2(_displayWidth + PanelPadding * 2f, _displayHeight + PanelPadding * 2f + 22f);
        }
    }

    private void UpdatePlayerMarkerIfNeeded(bool force)
    {
        if (_playerMarker == null || _map == null || _player == null)
        {
            return;
        }

        if (_map.TileData == null)
        {
            return;
        }

        Vector2 tileIndex = _map.TileData.WorldToTile(_player.Position);
        if (!force && tileIndex == _lastTileIndex)
        {
            return;
        }

        _lastTileIndex = tileIndex;
        int tileX = Mathf.Clamp((int)tileIndex.x, 0, _map.TileData.WorldTileWidth - 1);
        int tileY = Mathf.Clamp((int)tileIndex.y, 0, _map.TileData.WorldTileHeight - 1);

        float localX = tileX - _roomOriginTile.x;
        float localY = tileY - _roomOriginTile.y;
        float markerX = (localX / _miniMapScale) * _displayScale;
        float markerY = (localY / _miniMapScale) * _displayScale;
        _playerMarker.RectPosition = new Vector2(markerX, markerY);
        _playerMarker.RectSize = new Vector2(4f, 4f);
    }

    private static Color GetTileColor(MapTileData data, int tileX, int tileY)
    {
        switch (data.Tiles[tileX, tileY])
        {
            case MapTileType.Start:
                return GameConstants.MapColors.RoomStart;
            case MapTileType.Exit:
                return GameConstants.MapColors.RoomExit;
            case MapTileType.Wall:
                return GameConstants.MapColors.Wall;
            case MapTileType.Box:
                return GameConstants.MapColors.Box;
            case MapTileType.Portal:
                return GameConstants.MapColors.Portal;
            case MapTileType.Lava:
                return GameConstants.MapColors.Lava;
            case MapTileType.Water:
                return GameConstants.MapColors.Water;
            default:
                return GetScenarioColor(GetScenarioByTile(data, tileX, tileY));
        }
    }

    private static MapScenarioType GetScenarioByTile(MapTileData data, int tileX, int tileY)
    {
        int roomX = Mathf.Clamp(tileX / data.RoomTileSize, 0, data.RoomsX - 1);
        int roomY = Mathf.Clamp(tileY / data.RoomTileSize, 0, data.RoomsY - 1);
        return data.RoomScenarios[roomX, roomY];
    }

    private static Color GetScenarioColor(MapScenarioType scenario)
    {
        switch (scenario)
        {
            case MapScenarioType.Grassland:
                return GameConstants.MapColors.ScenarioGrassland;
            case MapScenarioType.Mountain:
                return GameConstants.MapColors.ScenarioMountain;
            case MapScenarioType.Lava:
                return GameConstants.MapColors.ScenarioLava;
            default:
                return new Color(0.86f, 0.76f, 0.52f);
        }
    }
}