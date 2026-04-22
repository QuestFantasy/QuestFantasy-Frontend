using System;

using Godot;

using QuestFantasy.Characters;
public class Main : Node2D
{

	[Export] public string BackendBaseUrl = "http://127.0.0.1:8000";

	private AuthFlowController _authFlowController;
	private AuthApiClient _playerDataApiClient;
	private SidebarMenu _sidebarMenu;
	private PlayerHud _playerHud;
	private ProgressSyncIndicator _progressIndicator;
	private Map _map;
	private Player _player;
	private readonly Godot.Collections.Array<Monster> _spawnedMonsters = new Godot.Collections.Array<Monster>();
	private readonly string _syncSessionId = Guid.NewGuid().ToString("N");
	private int _syncSequence = 0;
	private float _checkpointElapsed = 0f;


	public override void _Ready()
	{
		GD.Print("遊戲開始了，正在讀取登入畫面...");
		SetProcess(true);
		SetupSidebarMenu();
		SetupProgressIndicator();
		SetupPlayerDataClient();
		SetupAuthFlowController();
		GD.Print("登入成功...");
	}

	private void SetupProgressIndicator()
	{
		_progressIndicator = new ProgressSyncIndicator();
		AddChild(_progressIndicator);
		_progressIndicator.SetState(ProgressSyncIndicator.SyncState.Hidden);
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
		_player.OnDied += OnPlayerDied;
		_player.OnRoomEntered += OnPlayerEnteredRoom;

		if (profileSnapshot != null)
		{
			_player.ApplyProfile(profileSnapshot);
		}

		_playerHud = new PlayerHud();
		AddChild(_playerHud);
		_playerHud.Initialize(_player);

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
		_authFlowController.Authenticated += OnAuthenticated;
		_authFlowController.LoggedOut += HandleLoggedOut;
	}

	private void SetupPlayerDataClient()
	{
		_playerDataApiClient = new AuthApiClient
		{
			BackendBaseUrl = BackendBaseUrl,
		};
		AddChild(_playerDataApiClient);
	}

	private void OnAuthenticated()
	{
		string token = _authFlowController?.CurrentSession?.Token;
		if (string.IsNullOrWhiteSpace(token) || _playerDataApiClient == null)
		{
			GD.Print("[ProgressSync] No token/client available. Building world with local defaults.");
			BuildPlayablePrototype(null);
			return;
		}

		GD.Print("[ProgressSync] Fetching player profile before entering level...");
		_progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Loading);
		if (!_playerDataApiClient.FetchPlayerProfile(token, OnPlayerProfileFetched))
		{
			GD.PrintErr("[Main] Failed to fetch player profile. Starting with local defaults.");
			GD.Print("[ProgressSync] Profile fetch request failed to send. Using local defaults.");
			_progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Hidden);
			BuildPlayablePrototype(null);
		}
	}

	private void OnPlayerProfileFetched(AuthApiResult result)
	{
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

	private void SetupSidebarMenu()
	{
		_sidebarMenu = new SidebarMenu();
		AddChild(_sidebarMenu);
		_sidebarMenu.SetMenuVisible(false);
		_sidebarMenu.AddMenuItem("logout", "Logout", OnLogoutPressed);
	}

	private void OnLogoutPressed()
	{
		TransmitPlayerProfile("logout");
		_authFlowController?.RequestLogout();
	}

	private void OnPlayerDied()
	{
		TransmitPlayerProfile("die");
	}

	private void OnPlayerEnteredRoom(Vector2 roomIndex, string reason)
	{
		string syncReason = string.IsNullOrWhiteSpace(reason) ? "room_enter" : reason;
		GD.Print($"[ProgressSync] Room entry autosave trigger at ({roomIndex.x}, {roomIndex.y}), reason={syncReason}.");
		TransmitPlayerProfile(syncReason);
	}

	private void HandleLoggedOut()
	{
		_progressIndicator?.SetState(ProgressSyncIndicator.SyncState.Hidden);
		DestroyPlayableWorld();
		_sidebarMenu?.SetMenuVisible(false);
		GetTree().Paused = true;
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

		if (Godot.Object.IsInstanceValid(_map))
		{
			_map.QueueFree();
		}
		_map = null;
	}
}
