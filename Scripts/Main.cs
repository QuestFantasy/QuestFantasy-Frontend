using System;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Data.Items;
using QuestFantasy.Prototype;
using QuestFantasy.UI;

public class Main : Node2D
{

    [Export] public string BackendBaseUrl = "http://127.0.0.1:8000";
    [Export] public bool EnableMobileInputUI = true; // Force enable virtual D-pad for testing

    private AuthFlowController _authFlowController;
    private AuthApiClient _playerDataApiClient;
    private SidebarMenu _sidebarMenu;
    private PlayerHud _playerHud;
    private BackpackUI _backpackUi;
    private DeathScreenUI _deathScreen;
    private ProgressSyncIndicator _progressIndicator;
    private Map _map;
    private Player _player;
    private MobileInputUI _mobileInputUI;
    private MiniMapUI _miniMapUi;
    private readonly EquipmentManager _equipManagerRef = new EquipmentManager();
    private readonly TreasureChest _chestRef = new TreasureChest();
    private readonly Godot.Collections.Array<Monster> _spawnedMonsters = new Godot.Collections.Array<Monster>();
    private readonly string _syncSessionId = Guid.NewGuid().ToString("N");
    private int _syncSequence = 0;
    private float _checkpointElapsed = 0f;
    private Timer _profileFetchTimeoutTimer;
    private bool _isProfileFetchPending = false;
    private int _activeProfileFetchId = 0;
    private PlayerProfileSnapshot _pendingProfileSnapshot;
    private LobbyManager _lobbyManager;
    private bool _gameLoadedAlready = false;  // Guard against loading twice
    private const float ProfileFetchTimeoutSeconds = 12f;


    public override void _Ready()
    {
        GD.Print("遊戲開始了，正在讀取登入畫面...");
        SetProcess(true);
        GetTree().Connect("node_added", this, nameof(OnSceneNodeAdded));
        SetupMobileInputUI();
        SetupSidebarMenu();
        SetupProgressIndicator();
        SetupPlayerDataClient();
        SetupAuthFlowController();
        SetupDeathScreen();
        GD.Print("登入成功...");
    }

    public override void _ExitTree()
    {
        if (GetTree() != null && GetTree().IsConnected("node_added", this, nameof(OnSceneNodeAdded)))
        {
            GetTree().Disconnect("node_added", this, nameof(OnSceneNodeAdded));
        }
    }

    private void SetupProgressIndicator()
    {
        _progressIndicator = new ProgressSyncIndicator();
        AddChild(_progressIndicator);
        _progressIndicator.SetState(ProgressSyncIndicator.SyncState.Hidden);

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

        // Ensure InteractionButtonUI exists in the scene
        if (InteractionButtonUI.Instance == null)
        {
            var interactBtn = new InteractionButtonUI();
            AddChild(interactBtn);
        }
    }

    public override void _Process(float delta)
    {
        if (_player == null)
        {
            return;
        }

        if (_authFlowController?.CurrentSession == null || !_authFlowController.CurrentSession.IsValid)
        {
            return;
        }

        _checkpointElapsed += delta;
        if (_checkpointElapsed >= 25f)
        {
            _checkpointElapsed = 0f;
            TransmitPlayerProfile("checkpoint");
        }
    }

    private void BuildPlayablePrototype(PlayerProfileSnapshot profileSnapshot)
    {
        GetTree().Paused = false;
        DestroyPlayableWorld();
        _pendingProfileSnapshot = profileSnapshot;
        EnsureLobbyBackpackContext();
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
        _authFlowController.Authenticated += OnAuthenticated;
        _authFlowController.LoggedOut += HandleLoggedOut;
        _authFlowController.AuthViewShown += () => _mobileInputUI?.HideDPad();
    }

    private void SetupPlayerDataClient()
    {
        _playerDataApiClient = new AuthApiClient
        {
            BackendBaseUrl = BackendBaseUrl,
            PauseMode = PauseModeEnum.Process,
        };
        AddChild(_playerDataApiClient);

        _profileFetchTimeoutTimer = new Timer
        {
            OneShot = true,
            WaitTime = ProfileFetchTimeoutSeconds,
            PauseMode = PauseModeEnum.Process,
        };
        AddChild(_profileFetchTimeoutTimer);
        _profileFetchTimeoutTimer.Connect("timeout", this, nameof(OnProfileFetchTimeout));
    }

    private void OnAuthenticated()
    {
        // If logout paused the tree, profile HTTP callbacks can stall forever.
        GetTree().Paused = false;

        string token = _authFlowController?.CurrentSession?.Token;
        if (string.IsNullOrWhiteSpace(token) || _playerDataApiClient == null)
        {
            GD.Print("[ProgressSync] No token/client available. Building world with local defaults.");
            BuildPlayablePrototype(null);
            return;
        }

        GD.Print("[ProgressSync] Fetching player profile before entering level...");
        _progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Loading);
        _activeProfileFetchId += 1;
        int requestId = _activeProfileFetchId;
        _isProfileFetchPending = true;
        _profileFetchTimeoutTimer?.Start(ProfileFetchTimeoutSeconds);

        if (!_playerDataApiClient.FetchPlayerProfile(token, result => OnPlayerProfileFetched(requestId, result)))
        {
            GD.PrintErr("[Main] Failed to fetch player profile. Starting with local defaults.");
            GD.Print("[ProgressSync] Profile fetch request failed to send. Using local defaults.");
            _isProfileFetchPending = false;
            _profileFetchTimeoutTimer?.Stop();
            _progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Hidden);
            BuildPlayablePrototype(null);
        }
    }

    private void OnPlayerProfileFetched(int requestId, AuthApiResult result)
    {
        if (!_isProfileFetchPending || requestId != _activeProfileFetchId)
        {
            GD.Print($"[ProgressSync] Ignored stale profile response. requestId={requestId}, active={_activeProfileFetchId}, pending={_isProfileFetchPending}.");
            return;
        }

        _isProfileFetchPending = false;
        _profileFetchTimeoutTimer?.Stop();
        _progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Hidden);
        if (!result.NetworkOk || !result.IsSuccessStatus(200))
        {
            GD.PrintErr("[Main] Player profile fetch failed. Starting with local defaults.");
            GD.Print($"[ProgressSync] Profile fetch failed. NetworkOk={result.NetworkOk}, Code={result.ResponseCode}.");
            BuildPlayablePrototype(null);
            return;
        }

        var snapshot = PlayerProfileSnapshot.FromDictionary(result.Data);
        GD.Print($"[ProgressSync] Profile loaded. Level={snapshot.Level}, HP={snapshot.HpCurrent}/{snapshot.HpMax}, EXP={snapshot.Experience}, Gold={snapshot.Gold}, Skills={snapshot.Skills.Count}.");
        BuildPlayablePrototype(snapshot);
    }

    private void OnProfileFetchTimeout()
    {
        if (!_isProfileFetchPending)
        {
            return;
        }

        _isProfileFetchPending = false;
        GD.PrintErr($"[ProgressSync] Profile fetch timed out after {ProfileFetchTimeoutSeconds}s. Using local defaults.");
        _progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Hidden);
        BuildPlayablePrototype(null);
    }

    private void SetupSidebarMenu()
    {
        _sidebarMenu = new SidebarMenu();
        AddChild(_sidebarMenu);
        _sidebarMenu.SetMenuVisible(false);
        _sidebarMenu.AddMenuItem("logout", "Logout", OnLogoutPressed);
    }

    private void SetupMobileInputUI()
    {
        // Create mobile input UI once for the entire game lifetime
        bool shouldEnableMobileUI = EnableMobileInputUI || OS.HasTouchscreenUiHint();
        if (shouldEnableMobileUI)
        {
            _mobileInputUI = new MobileInputUI();
            AddChild(_mobileInputUI);
            _mobileInputUI.HideDPad();
            GD.Print("[Main] Mobile input UI enabled globally for touch controls");
        }
    }

    private void SetupDeathScreen()
    {
        _deathScreen = new DeathScreenUI();
        AddChild(_deathScreen);
        _deathScreen.OnRespawnClicked += () =>
        {
            _deathScreen.SetVisible(false);
            if (_playerHud != null) _playerHud.SetVisible(true);
            _player?.Respawn();
        };
        _deathScreen.OnExitClicked += () =>
        {
            _deathScreen.SetVisible(false);
            ReturnToLobby();
        };
    }

    private void OnLogoutPressed()
    {
        TransmitPlayerProfile("logout");
        _authFlowController?.RequestLogout();
    }

    private void OnPlayerDied()
    {
        TransmitPlayerProfile("die");
        if (_playerHud != null) _playerHud.SetVisible(false);
        _deathScreen?.SetVisible(true);
    }

    private void OnPlayerEnteredRoom(Vector2 roomIndex, string reason)
    {
        string syncReason = string.IsNullOrWhiteSpace(reason) ? "room_enter" : reason;
        GD.Print($"[ProgressSync] Room entry autosave trigger at ({roomIndex.x}, {roomIndex.y}), reason={syncReason}.");
        TransmitPlayerProfile(syncReason);
    }

    private void HandleLoggedOut()
    {
        _isProfileFetchPending = false;
        _profileFetchTimeoutTimer?.Stop();
        _progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Hidden);
        _mobileInputUI?.HideDPad();
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

    private void TransmitPlayerProfile(string reason)
    {
        if (_player == null || _playerDataApiClient == null || _playerDataApiClient.IsBusy)
        {
            GD.Print($"[ProgressSync] Skipped send. reason={reason}, playerReady={_player != null}, clientReady={_playerDataApiClient != null}, clientBusy={_playerDataApiClient?.IsBusy}.");
            return;
        }

        string token = _authFlowController?.CurrentSession?.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            GD.Print($"[ProgressSync] Skipped send. reason={reason}, missing auth token.");
            return;
        }

        _syncSequence += 1;
        PlayerProfileSnapshot snapshot = _player.BuildProfileSnapshot();
        var payload = snapshot.ToUpdatePayload(_syncSessionId, _syncSequence);
        payload["reason"] = reason;
        GD.Print($"[ProgressSync] Sending profile. reason={reason}, seq={_syncSequence}, level={snapshot.Level}, hp={snapshot.HpCurrent}/{snapshot.HpMax}, exp={snapshot.Experience}, gold={snapshot.Gold}.");
        _progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Saving);

        if (!_playerDataApiClient.UpdatePlayerProfile(token, payload, OnPlayerProfileSynced))
        {
            GD.PrintErr($"[ProgressSync] Send failed to start. reason={reason}, seq={_syncSequence}.");
            _progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Hidden);
            _syncSequence -= 1;
        }
    }

    private void OnPlayerProfileSynced(AuthApiResult result)
    {
        _progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Hidden);
        if (!result.NetworkOk || !result.IsSuccessStatus(200) || _player == null)
        {
            GD.PrintErr($"[ProgressSync] Sync failed. NetworkOk={result.NetworkOk}, Code={result.ResponseCode}, playerReady={_player != null}.");
            return;
        }

        PlayerProfileSnapshot snapshot = PlayerProfileSnapshot.FromDictionary(result.Data);
        if (!snapshot.Ignored)
        {
            GD.Print($"[ProgressSync] Sync applied. Level={snapshot.Level}, HP={snapshot.HpCurrent}/{snapshot.HpMax}, EXP={snapshot.Experience}, Gold={snapshot.Gold}.");
            _player.ApplyProfile(snapshot);
            return;
        }

        GD.Print($"[ProgressSync] Sync ignored by server. reason={snapshot.IgnoreReason}.");
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

        if (Godot.Object.IsInstanceValid(_miniMapUi))
        {
            _miniMapUi.QueueFree();
        }
        _miniMapUi = null;

        if (Godot.Object.IsInstanceValid(_player))
        {
            _player.OnDied -= OnPlayerDied;
            _player.OnRoomEntered -= OnPlayerEnteredRoom;
            _player.QueueFree();
        }
        _player = null;

        if (Godot.Object.IsInstanceValid(_playerHud))
        {
            _playerHud.QueueFree();
        }
        _playerHud = null;

        if (Godot.Object.IsInstanceValid(_backpackUi))
        {
            _backpackUi.DropRequested -= OnBackpackDropRequested;
            _backpackUi.SyncRequested -= OnInventorySyncRequested;
            _backpackUi.QueueFree();
        }
        _backpackUi = null;

        // DO NOT destroy _mobileInputUI - it should persist throughout the game
        // Hide it when leaving gameplay; it will be re-shown only when entering a game level
        _mobileInputUI?.HideDPad();

        if (Godot.Object.IsInstanceValid(_map))
        {
            _map.QueueFree();
        }
        _map = null;
        _gameLoadedAlready = false;

    }
    private void BuildLobby()
    {
        EnsureLobbyBackpackContext();

        _lobbyManager = new LobbyManager();
        _lobbyManager.Initialize(_player, _playerDataApiClient, _authFlowController?.CurrentSession?.Token);
        AddChild(_lobbyManager);
        _lobbyManager.DifficultySelected += OnDifficultySelected;

        // Show D-pad in the lobby, but hide map button
        _mobileInputUI?.ShowDPad();
        _mobileInputUI?.ShowMapButton(false);
        _lobbyManager.SyncRequested += OnInventorySyncRequested;
    }

    private void EnsureLobbyBackpackContext()
    {
        if (!Godot.Object.IsInstanceValid(_player))
        {
            _player = new Player();
            AddChild(_player);
        }

        _player.Visible = false;

        if (_pendingProfileSnapshot != null)
        {
            _player.ApplyProfile(_pendingProfileSnapshot);
        }

        if (!Godot.Object.IsInstanceValid(_backpackUi))
        {
            _backpackUi = new BackpackUI();
            AddChild(_backpackUi);
            _backpackUi.DropRequested += OnBackpackDropRequested;
            _backpackUi.SyncRequested += OnInventorySyncRequested;
        }

        _backpackUi.Initialize(_player);
        _backpackUi.SetGameplayVisible(true);
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
        DestroyPlayableWorld();

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
        _player.OnDied += OnPlayerDied;
        _player.OnRoomEntered += OnPlayerEnteredRoom;

        if (_pendingProfileSnapshot != null)
        {
            _player.ApplyProfile(_pendingProfileSnapshot);
            _pendingProfileSnapshot = null;
        }

        _playerHud = new PlayerHud();
        AddChild(_playerHud);
        _playerHud.Initialize(_player, _map);
        _playerHud.OnSkillSlotPressed += (skillIndex) =>
        {
            _player?.InputHandler?.RequestSkillActivation(skillIndex);
        };

        _miniMapUi = new MiniMapUI();
        AddChild(_miniMapUi);
        _miniMapUi.Initialize(_map, _player);

        _backpackUi = new BackpackUI();
        AddChild(_backpackUi);
        _backpackUi.Initialize(_player);
        _backpackUi.SetGameplayVisible(true);
        _backpackUi.DropRequested += OnBackpackDropRequested;

        // Show D-pad only during actual gameplay
        _mobileInputUI?.ShowDPad();
        _mobileInputUI?.ShowMapButton(true);
        _backpackUi.SyncRequested += OnInventorySyncRequested;

        // Spawn monsters based on difficulty
        int numMonstersToSpawn = ((int)difficulty + 1) * 100;
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
        _pendingProfileSnapshot = _player?.BuildProfileSnapshot();
        DestroyPlayableWorld();

        // Rebuild the lobby for another session
        BuildLobby();
    }

    // Note: Map.BoxOpened is handled directly by TreasureChest.HandleMapBoxOpened now.

    private void OnSceneNodeAdded(Node node)
    {
        if (node is EquipmentPickup pickup)
        {
            AttachPickupSignal(pickup);
        }
    }

    private void AttachPickupSignal(EquipmentPickup pickup)
    {
        if (pickup == null)
        {
            return;
        }

        if (!pickup.IsConnected(nameof(EquipmentPickup.PickupRequested), this, nameof(OnPickupRequested)))
        {
            pickup.Connect(nameof(EquipmentPickup.PickupRequested), this, nameof(OnPickupRequested));
        }
    }

    private void OnPickupRequested(EquipmentPickup pickup)
    {
        if (_player == null || pickup == null || !Godot.Object.IsInstanceValid(pickup))
        {
            return;
        }

        if (!(pickup.ItemData is Item item))
        {
            GD.Print("[Backpack] Pickup ignored: item data missing or invalid.");
            return;
        }

        if (!_player.AddItem(item))
        {
            GD.Print("[Backpack] Pickup failed: inventory add rejected.");
            return;
        }

        EquipmentPreview.Instance?.HidePreview();
        pickup.QueueFree();
        TransmitPlayerProfile("pickup_item");
    }

    private void OnBackpackDropRequested(Item item)
    {
        if (_player == null || item == null)
        {
            return;
        }

        if (!_player.DiscardItem(item))
        {
            GD.Print("[Backpack] Drop failed: item not in inventory.");
            return;
        }

        var droppedPickup = new EquipmentPickup
        {
            ItemData = item,
            SpriteScale = _equipManagerRef?.PickupSpriteScale ?? 0.5f,
        };

        var rng = new RandomNumberGenerator();
        rng.Randomize();
        droppedPickup.Position = _player.Position + new Vector2(rng.RandfRange(-36f, 36f), rng.RandfRange(-24f, 24f));
        AddChild(droppedPickup);

        TransmitPlayerProfile("discard_item");
    }

    private void OnInventorySyncRequested()
    {
        TransmitPlayerProfile("inventory_sync");
    }


}