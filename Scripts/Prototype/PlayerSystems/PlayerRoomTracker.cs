using Godot;

public class PlayerRoomTracker
{
    private Vector2 _currentRoomIndex = Vector2.Zero;
    private float _portalCooldown;

    public Vector2 CurrentRoomIndex => _currentRoomIndex;
    public bool IsPortalReady => _portalCooldown <= 0f;

    public void InitializeFromPosition(Map map, Vector2 position)
    {
        if (map == null)
        {
            return;
        }

        _currentRoomIndex = map.GetRoomIndexByWorldPosition(position);
    }

    public void Tick(float delta)
    {
        if (_portalCooldown > 0f)
        {
            _portalCooldown -= delta;
        }
    }

    public void SetPortalCooldown(float seconds)
    {
        _portalCooldown = seconds;
    }

    public bool TryUpdateRoomByPosition(Map map, Vector2 position)
    {
        if (map == null)
        {
            return false;
        }

        Vector2 roomIndex = map.GetRoomIndexByWorldPosition(position);
        if (roomIndex == _currentRoomIndex)
        {
            return false;
        }

        _currentRoomIndex = roomIndex;
        return true;
    }

    public bool TryHandleExit(Map map, Vector2 position, out Vector2 nextPosition)
    {
        nextPosition = position;
        if (map == null || !map.IsAtRoomExit(position, _currentRoomIndex))
        {
            return false;
        }

        map.RegenerateWithRandomSeed();
        _currentRoomIndex = Vector2.Zero;
        nextPosition = map.GetSpawnWorldPosition();
        _portalCooldown = 0.5f;
        return true;
    }

    public bool TryRespawnAtCurrentRoomStart(Map map, out Vector2 nextPosition)
    {
        nextPosition = Vector2.Zero;
        if (map == null)
        {
            return false;
        }

        nextPosition = map.GetRoomStartWorldPosition(_currentRoomIndex);
        return true;
    }

    public bool TryHandlePortal(Map map, Vector2 position, out Vector2 destinationPosition)
    {
        destinationPosition = position;
        if (map == null || !IsPortalReady)
        {
            return false;
        }

        if (!map.TryGetPortalDestination(position, out destinationPosition))
        {
            return false;
        }

        _currentRoomIndex = map.GetRoomIndexByWorldPosition(destinationPosition);
        _portalCooldown = 0.5f;
        return true;
    }
}
