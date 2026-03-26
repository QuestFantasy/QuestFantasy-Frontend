using Godot;

using QuestFantasy.Characters;

public class Main : Node2D
{
    private Node2D _player;
    private PackedScene _playerScene;
	private PrototypeMap _map;
	private PrototypePlayer _player;

    public override void _Ready()
    {
        _playerScene = (PackedScene)GD.Load("res://Scenes/Entities/player.tscn");
        SpawnPlayer();
        SetProcess(true);
    }
	public override void _Ready()
	{
		BuildPlayablePrototype();
	}

    public override void _Process(float delta)
    {
        if (_player == null)
            return;

        var rect = GetViewport().GetVisibleRect();
        var pos = _player.Position;
        if (pos.x < 0 || pos.y < 0 || pos.x > rect.Size.x || pos.y > rect.Size.y)
        {
            _player.QueueFree();
            _player = null;
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        if (_playerScene == null)
            return;

        var inst = (Node2D)_playerScene.Instance();
        AddChild(inst);
        var rect = GetViewport().GetVisibleRect();
        inst.Position = rect.Size / 2;
        _player = inst;
    }
	private void BuildPlayablePrototype()
	{
		_map = new PrototypeMap();
		_map.TileSize = 24;
		_map.RoomTileSize = 100;
		_map.RoomsX = 2;
		_map.RoomsY = 2;
		AddChild(_map);
		_map.RegenerateWithRandomSeed();

		_player = new PrototypePlayer();
		AddChild(_player);
		_player.Position = _map.GetSpawnWorldPosition();
		_player.SetMap(_map);
	}
}
