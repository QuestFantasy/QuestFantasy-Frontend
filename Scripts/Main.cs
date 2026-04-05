using Godot;

using QuestFantasy.Characters;
public class Main : Node2D
{

    [Export] public string BackendBaseUrl = "http://127.0.0.1:8000";

    private PrototypeGameplaySession _gameplaySession;
    private AuthFlowController _authFlowController;
    private SidebarMenu _sidebarMenu;
    private Map _map;
    private Player _player;


    public override void _Ready()
    {
        SetupGameplaySession();
        SetupSidebarMenu();
        SetupAuthFlowController();
    }

    private void BuildPlayablePrototype()
    {
        GetTree().Paused = false;
        _gameplaySession?.StartSession();
        _sidebarMenu?.SetMenuVisible(true);

        if (_map != null)
        {
            _map.QueueFree();
            _map = null;
        }
        if (_player != null)
        {
            _player.QueueFree();
            _player = null;
        }

        // Clean up existing monsters
        foreach (Node child in GetChildren())
        {
            if (child is Monster monster)
            {
                monster.QueueFree();
            }
        }

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
        }
    }

    private void SetupGameplaySession()
    {
        _gameplaySession = new PrototypeGameplaySession();
        AddChild(_gameplaySession);
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
        _gameplaySession?.StopSession();
        _sidebarMenu?.SetMenuVisible(false);
        GetTree().Paused = true;
    }
}