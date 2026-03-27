using Godot;

/// <summary>
/// Tracks the player's room location and portal state.
/// 
/// Responsibilities:
/// - Maintains the current room index
/// - Manages portal cooldown mechanics
/// - Handles room transitions and exit-room logic
/// </summary>
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

    /// <summary>
    /// Sets the portal cooldown duration (in seconds).
    /// Uses Mathf.Max to prevent accidental cooldown reduction if one already exists.
    /// </summary>
    public void SetPortalCooldown(float seconds)
    {
        _portalCooldown = Mathf.Max(_portalCooldown, seconds);
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
        SetPortalCooldown(GameConstants.PORTAL_TELEPORT_COOLDOWN);
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
        SetPortalCooldown(GameConstants.PORTAL_TELEPORT_COOLDOWN);
        return true;
    }

    /// <summary>
    /// Applies interference cooldown to portals, typically called when the player performs other interactions (such as opening boxes).
    /// This prevents accidental portal activation during other gameplay actions.
    /// </summary>
    public void InterferPortalWithInteraction()
    {
        SetPortalCooldown(GameConstants.INTERACTION_PORTAL_INTERFERENCE);
    }
}