using Godot;
using QuestFantasy.Characters;

public class Main : Node2D
{
	private PrototypeMap _map;
	private PrototypePlayer _player;

	public override void _Ready()
	{
		BuildPlayablePrototype();
	}

	private void BuildPlayablePrototype()
	{
		_map = new PrototypeMap();
		_map.TileSize = 24;
		_map.RoomTileSize = 100;
		_map.RoomsX = 2;
		_map.RoomsY = 2;
		AddChild(_map);
		_map.Generate();

		_player = new PrototypePlayer();
		AddChild(_player);
		_player.Position = _map.GetSpawnWorldPosition();
		_player.SetMap(_map);
	}
}
