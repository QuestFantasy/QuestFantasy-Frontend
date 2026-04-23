using Godot;

using QuestFantasy.Characters;
public class Main : Node2D
{

    [Export] public string BackendBaseUrl = "http://127.0.0.1:8000";

    private AuthFlowController _authFlowController;
    private SidebarMenu _sidebarMenu;
    private Map _map;
    private Player _player;
    private EquipmentManager _equipManagerRef;
    private TreasureChest _chestRef;
    private readonly Godot.Collections.Array<Monster> _spawnedMonsters = new Godot.Collections.Array<Monster>();


    public override void _Ready()
    {
        SetupSidebarMenu();
        SetupAuthFlowController();
    }

    private void BuildPlayablePrototype()
    {
        GetTree().Paused = false;
        DestroyPlayableWorld();
        _sidebarMenu?.SetMenuVisible(true);

        _map = new Map();
        _map.TileSize = 24;
        _map.RoomTileSize = 100;
        _map.RoomsX = 2;
        _map.RoomsY = 2;
        AddChild(_map);
        _map.RegenerateWithRandomSeed();

        _player = new Player();
        AddChild(_player);
        _player.Position = _map.GetSpawnWorldPosition();
        _player.SetMap(_map);

        // Spawn multiple Monsters (產生多隻怪物)
        var monsterScene = (PackedScene)GD.Load("res://Scenes/Entities/monster.tscn");
        int numMonstersToSpawn = 3; // 可以修改這個數字來控制怪物數量
        for (int i = 0; i < numMonstersToSpawn; i++)
        {
            var monster = (Monster)monsterScene.Instance();
            monster.SetEnvironment(_map, _player);
            AddChild(monster);
            _spawnedMonsters.Add(monster);
        }

        // Add equipment manager and a treasure chest. Chest will spawn items when opened.
        var equipManager = new EquipmentManager();
        AddChild(equipManager);
        _equipManagerRef = equipManager;

        var chest = new TreasureChest();
        chest.EquipmentManagerPath = equipManager.GetPath();
        AddChild(chest);
        _chestRef = chest;

        // Add EquipmentPreview UI singleton
        var preview = new EquipmentPreview();
        AddChild(preview);

        // Connect map BoxOpened signal to spawn drops
        if (_map != null && _chestRef != null && _equipManagerRef != null)
        {
            _map.Connect("BoxOpened", this, nameof(OnBoxOpened));
        }
    }

    private void SetupAuthFlowController()
    {
        _authFlowController = new AuthFlowController
        {
            BackendBaseUrl = BackendBaseUrl,
            PauseMode = PauseModeEnum.Process
        };
        AddChild(_authFlowController);
        _authFlowController.Authenticated += BuildPlayablePrototype;
        _authFlowController.LoggedOut += HandleLoggedOut;
    }

    private void SetupSidebarMenu()
    {
        _sidebarMenu = new SidebarMenu();
        AddChild(_sidebarMenu);
        _sidebarMenu.SetMenuVisible(false);
        _sidebarMenu.AddMenuItem("logout", "Logout", OnLogoutPressed);
    }

    private void OnLogoutPressed()
    {
        _authFlowController?.RequestLogout();
    }

    private void HandleLoggedOut()
    {
        DestroyPlayableWorld();
        _sidebarMenu?.SetMenuVisible(false);
        GetTree().Paused = true;
    }

    private void DestroyPlayableWorld()
    {
        for (int i = 0; i < _spawnedMonsters.Count; i++)
        {
            if (Godot.Object.IsInstanceValid(_spawnedMonsters[i]))
            {
                _spawnedMonsters[i].QueueFree();
            }
        }
        _spawnedMonsters.Clear();

        if (Godot.Object.IsInstanceValid(_player))
        {
            _player.QueueFree();
        }
        _player = null;

        if (Godot.Object.IsInstanceValid(_map))
        {
            _map.QueueFree();
        }
        _map = null;
    }

    // Called when a box opens in the map; spawn equipment pickups at the position
    public void OnBoxOpened(Vector2 worldPosition)
    {
        GD.PrintS($"[Main] Box opened at {worldPosition}");
        if (_chestRef == null || _equipManagerRef == null || _player == null)
        {
            GD.PrintS("[Main] Missing chest or equipment manager or player reference");
            return;
        }

        _chestRef.OpenChest(this, worldPosition, _equipManagerRef, (int)_player.Level);
    }


}