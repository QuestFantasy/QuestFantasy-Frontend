using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Prototype;

public class Main : Node2D
{

    [Export] public string BackendBaseUrl = "http://127.0.0.1:8000";

    private AuthFlowController _authFlowController;
    private SidebarMenu _sidebarMenu;
    private Map _map;
    private Player _player;
    private readonly EquipmentManager _equipManagerRef = new EquipmentManager();
    private readonly TreasureChest _chestRef = new TreasureChest();
    private readonly Godot.Collections.Array<Monster> _spawnedMonsters = new Godot.Collections.Array<Monster>();
    private LobbyManager _lobbyManager;
    private bool _gameLoadedAlready = false;  // Guard against loading twice


    public override void _Ready()
    {
        SetupSidebarMenu();
        SetupAuthFlowController();

        if (_equipManagerRef.GetParent() == null)
        {
            AddChild(_equipManagerRef);
        }

        if (_chestRef.GetParent() == null)
        {
            AddChild(_chestRef);
        }

        // Ensure EquipmentPreview exists in the scene so pickups can call EquipmentPreview.Instance
        if (EquipmentPreview.Instance == null)
        {
            var preview = new EquipmentPreview();
            AddChild(preview);
        }
    }

    private void BuildPlayablePrototype()
    {

        GetTree().Paused = false;
        DestroyPlayableWorld();
        _sidebarMenu?.SetMenuVisible(true);
        // Build lobby instead of directly loading a game map
        BuildLobby();
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
        _gameLoadedAlready = false;
        _sidebarMenu?.SetMenuVisible(false);
        GetTree().Paused = true;
        _sidebarMenu?.SetMenuVisible(false);

        // Clean up any active gameplay/lobby scenes
        _lobbyManager?.QueueFree();
        _lobbyManager = null;
        _map?.QueueFree();
        _map = null;
        _player = null;

        GD.Print("[Main] Logged out - all game states cleaned");
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
        _gameLoadedAlready = false;

    }
    private void BuildLobby()
    {
        _lobbyManager = new LobbyManager();
        AddChild(_lobbyManager);
        _lobbyManager.DifficultySelected += OnDifficultySelected;
    }

    private void OnDifficultySelected(DifficultyLevel difficulty)
    {
        if (_gameLoadedAlready)
            return;

        _gameLoadedAlready = true;
        _lobbyManager?.QueueFree();
        _lobbyManager = null;
        LoadGameLevel(difficulty);
    }

    private void LoadGameLevel(DifficultyLevel difficulty)
    {
        _map = new Map();
        _map.TileSize = 24;
        _map.RoomTileSize = 100;
        _map.RoomsX = 2;
        _map.RoomsY = 2;
        AddChild(_map);
        _map.RegenerateWithRandomSeed();
        // Connect map's BoxOpened directly to TreasureChest so Main doesn't need to forward.
        _map.Connect("BoxOpened", _chestRef, nameof(TreasureChest.HandleMapBoxOpened));

        _player = new Player();
        AddChild(_player);
        _player.Position = _map.GetSpawnWorldPosition();
        _player.SetMap(_map);

        // Spawn monsters based on difficulty
        int numMonstersToSpawn = ((int)difficulty + 1) * 10;
        var monsterScene = (PackedScene)GD.Load("res://Scenes/Entities/monster.tscn");
        for (int i = 0; i < numMonstersToSpawn; i++)
        {
            var monster = (Monster)monsterScene.Instance();
            monster.SetEnvironment(_map, _player);
            AddChild(monster);
            _spawnedMonsters.Add(monster);
        }

        // Listen for when player reaches the exit to return to lobby
        _player.GetCharacterController().ExitReached += ReturnToLobby;
    }

    private void ReturnToLobby()
    {
        GD.Print("[Main] Player reached exit - returning to lobby");
        DestroyPlayableWorld();

        // Rebuild the lobby for another session
        BuildLobby();
    }

    // Note: Map.BoxOpened is handled directly by TreasureChest.HandleMapBoxOpened now.


}