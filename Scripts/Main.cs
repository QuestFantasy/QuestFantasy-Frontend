using Godot;

using QuestFantasy.Characters;
public class Main : Node2D
{

    [Export] public string BackendBaseUrl = "http://127.0.0.1:8000";

    private AuthFlowController _authFlowController;
    private SidebarMenu _sidebarMenu;
    private Map _map;
    private Player _player;
    private PlayerHUD _playerHUD;
    private DeathScreenUI _deathScreen;
    private readonly Godot.Collections.Array<Monster> _spawnedMonsters = new Godot.Collections.Array<Monster>();


    public override void _Ready()
    {
        SetupSidebarMenu();
        SetupAuthFlowController();
        SetupDeathScreen();
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
        _player.OnPlayerDied += () => _deathScreen?.SetVisible(true);

        _playerHUD = new PlayerHUD();
        AddChild(_playerHUD);
        _playerHUD.Initialize(_player);

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

    private void SetupDeathScreen()
    {
        _deathScreen = new DeathScreenUI();
        AddChild(_deathScreen);
        _deathScreen.OnRespawnClicked += () =>
        {
            _deathScreen.SetVisible(false);
            _player?.Respawn();
        };
        _deathScreen.OnExitClicked += () =>
        {
            _deathScreen.SetVisible(false);
            OnLogoutPressed();
        };
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

        if (Godot.Object.IsInstanceValid(_playerHUD))
        {
            _playerHUD.QueueFree();
        }
        _playerHUD = null;

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
}