using Godot;

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
        _gameplaySession?.StartSession();
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
            BackendBaseUrl = BackendBaseUrl
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
    }
}