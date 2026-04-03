using Godot;

using QuestFantasy.Characters;

/// <summary>
/// Manages the playable prototype session lifecycle (map + player).
/// Keeps session concerns out of Main for better modularity.
/// </summary>
public class PrototypeGameplaySession : Node2D
{
    [Export] public int TileSize = 24;
    [Export] public int RoomTileSize = 100;
    [Export] public int RoomsX = 2;
    [Export] public int RoomsY = 2;

    private Map _map;
    private Player _player;

    public bool IsActive => _map != null && _player != null;

    public void StartSession()
    {
        if (IsActive)
        {
            return;
        }

        StopSession();

        _map = new Map
        {
            TileSize = TileSize,
            RoomTileSize = RoomTileSize,
            RoomsX = RoomsX,
            RoomsY = RoomsY
        };
        AddChild(_map);
        _map.RegenerateWithRandomSeed();

        _player = new Player();
        AddChild(_player);
        _player.Position = _map.GetSpawnWorldPosition();
        _player.SetMap(_map);
    }

    public void StopSession()
    {
        if (_player != null)
        {
            _player.QueueFree();
            _player = null;
        }

        if (_map != null)
        {
            _map.QueueFree();
            _map = null;
        }
    }
}