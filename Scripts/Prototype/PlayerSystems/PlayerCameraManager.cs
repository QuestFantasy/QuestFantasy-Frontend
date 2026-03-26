using Godot;

public class PlayerCameraManager
{
    private Camera2D _camera;
    private Rect2 _pendingBounds;
    private bool _hasPendingBounds;

    public void Initialize(Node2D owner, Vector2 zoom)
    {
        _camera = new Camera2D();
        _camera.Current = true;
        _camera.SmoothingEnabled = true;
        _camera.SmoothingSpeed = 8f;
        _camera.Zoom = zoom;
        owner.AddChild(_camera);

        if (_hasPendingBounds)
        {
            ApplyBounds(_pendingBounds);
        }
    }

    public void ConfigureBounds(Rect2 worldBounds)
    {
        _pendingBounds = worldBounds;
        _hasPendingBounds = true;

        if (_camera == null)
        {
            return;
        }

        ApplyBounds(worldBounds);
    }

    public void LockToRoom(Map map, Vector2 roomIndex)
    {
        if (map == null)
        {
            return;
        }

        ConfigureBounds(map.GetRoomBoundsPixels(roomIndex));
    }

    private void ApplyBounds(Rect2 worldBounds)
    {
        _camera.LimitLeft = Mathf.RoundToInt(worldBounds.Position.x);
        _camera.LimitTop = Mathf.RoundToInt(worldBounds.Position.y);
        _camera.LimitRight = Mathf.RoundToInt(worldBounds.Position.x + worldBounds.Size.x);
        _camera.LimitBottom = Mathf.RoundToInt(worldBounds.Position.y + worldBounds.Size.y);
    }
}
