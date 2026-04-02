using Godot;

using QuestFantasy.Characters;

public class Main : Node2D
{
	private Map _map;
	private MapPlayer _player;

	public override void _Ready()
	{
		BuildPlayablePrototype();
	}

	private void BuildPlayablePrototype()
	{
		_map = new Map();
		_map.TileSize = 24;
		_map.RoomTileSize = 100;
		_map.RoomsX = 2;
		_map.RoomsY = 2;
		AddChild(_map);
		_map.RegenerateWithRandomSeed();

		_player = new MapPlayer();
		AddChild(_player);
		_player.Position = _map.GetSpawnWorldPosition();
		_player.SetMap(_map);

		// Spawn multiple Monsters (產生多隻怪物)
		var monsterScene = (PackedScene)GD.Load("res://Scenes/Entities/monster.tscn");
		int numMonstersToSpawn = 10; // 可以修改這個數字來控制怪物數量
		for (int i = 0; i < numMonstersToSpawn; i++)
		{
			var monster = (Monster)monsterScene.Instance();
			monster.SetEnvironment(_map, _player);
			AddChild(monster);
		}
	}
}
