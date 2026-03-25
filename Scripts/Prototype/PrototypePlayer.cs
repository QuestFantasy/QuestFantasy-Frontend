using Godot;

public class PrototypePlayer : Node2D
{
	[Export] public float MoveSpeed = 240f;
	[Export] public Vector2 BodySize = new Vector2(18, 18);
	[Export] public Vector2 CameraZoom = new Vector2(0.7f, 0.7f);

	private PrototypeMap _map;
	private Camera2D _camera;
	private Rect2 _pendingCameraBounds;
	private bool _hasPendingBounds;
	private Vector2 _currentRoomIndex = Vector2.Zero;
	private bool _levelCompleted;
	private float _portalCooldown;

	public override void _Ready()
	{
		_camera = new Camera2D();
		_camera.Current = true;
		_camera.SmoothingEnabled = true;
		_camera.SmoothingSpeed = 8f;
		_camera.Zoom = CameraZoom;
		AddChild(_camera);

		if (_hasPendingBounds)
		{
			ApplyCameraBounds(_pendingCameraBounds);
		}

		Update();

		SetPhysicsProcess(true);
	}

	public void SetMap(PrototypeMap map)
	{
		_map = map;
		if (_map != null)
		{
			_currentRoomIndex = _map.GetRoomIndexByWorldPosition(Position);
			LockCameraToRoom(_currentRoomIndex);
		}
	}

	public void ConfigureCameraBounds(Rect2 worldBounds)
	{
		_pendingCameraBounds = worldBounds;
		_hasPendingBounds = true;

		if (_camera == null)
		{
			return;
		}

		ApplyCameraBounds(worldBounds);
	}

	private void ApplyCameraBounds(Rect2 worldBounds)
	{
		_camera.LimitLeft = Mathf.RoundToInt(worldBounds.Position.x);
		_camera.LimitTop = Mathf.RoundToInt(worldBounds.Position.y);
		_camera.LimitRight = Mathf.RoundToInt(worldBounds.Position.x + worldBounds.Size.x);
		_camera.LimitBottom = Mathf.RoundToInt(worldBounds.Position.y + worldBounds.Size.y);
	}

	public override void _PhysicsProcess(float delta)
	{
		if (_map == null)
		{
			return;
		}

		if (_portalCooldown > 0f)
		{
			_portalCooldown -= delta;
		}

		if (Input.IsActionJustPressed("ui_cancel"))
		{
			RespawnAtCurrentRoomStart();
		}

		Vector2 input = Vector2.Zero;
		if (Input.IsActionPressed("ui_right"))
		{
			input.x += 1f;
		}
		if (Input.IsActionPressed("ui_left"))
		{
			input.x -= 1f;
		}
		if (Input.IsActionPressed("ui_down"))
		{
			input.y += 1f;
		}
		if (Input.IsActionPressed("ui_up"))
		{
			input.y -= 1f;
		}

		if (input.LengthSquared() > 0)
		{
			Vector2 velocity = input.Normalized() * MoveSpeed;
			TryMove(velocity * delta);
		}

		TryHandlePortalTeleport();
		UpdateRoomStateAndHandleExit();
	}

	public override void _Draw()
	{
		Rect2 rect = new Rect2(-BodySize / 2f, BodySize);
		DrawRect(rect, new Color(0.95f, 0.95f, 0.98f));
		DrawRect(rect.Grow(-2), new Color(0.28f, 0.28f, 0.35f), false, 2f);
	}

	private void TryMove(Vector2 deltaMove)
	{
		Vector2 nextX = new Vector2(Position.x + deltaMove.x, Position.y);
		if (_map.CanMoveTo(GetBodyRect(nextX)))
		{
			Position = nextX;
		}

		Vector2 nextY = new Vector2(Position.x, Position.y + deltaMove.y);
		if (_map.CanMoveTo(GetBodyRect(nextY)))
		{
			Position = nextY;
		}
	}

	private Rect2 GetBodyRect(Vector2 centerPosition)
	{
		return new Rect2(centerPosition - BodySize / 2f, BodySize);
	}

	private void UpdateRoomStateAndHandleExit()
	{
		if (_map == null)
		{
			return;
		}

		Vector2 roomIndex = _map.GetRoomIndexByWorldPosition(Position);
		if (roomIndex != _currentRoomIndex)
		{
			_currentRoomIndex = roomIndex;
			LockCameraToRoom(_currentRoomIndex);
		}

		if (_levelCompleted)
		{
			return;
		}

		if (!_map.IsAtRoomExit(Position, _currentRoomIndex))
		{
			return;
		}

		if (_map.TryGetNextRoomIndex(_currentRoomIndex, out Vector2 nextRoomIndex))
		{
			Position = _map.GetRoomStartWorldPosition(nextRoomIndex);
			_currentRoomIndex = nextRoomIndex;
			LockCameraToRoom(_currentRoomIndex);
			_portalCooldown = 0.2f;
		}
		else
		{
			_levelCompleted = true;
			GD.Print("All generated rooms cleared.");
		}
	}

	private void RespawnAtCurrentRoomStart()
	{
		if (_map == null)
		{
			return;
		}

		Position = _map.GetRoomStartWorldPosition(_currentRoomIndex);
		LockCameraToRoom(_currentRoomIndex);
	}

	private void TryHandlePortalTeleport()
	{
		if (_map == null || _portalCooldown > 0f)
		{
			return;
		}

		if (_map.TryGetPortalDestination(Position, out Vector2 destinationWorld))
		{
			Position = destinationWorld;
			_currentRoomIndex = _map.GetRoomIndexByWorldPosition(Position);
			LockCameraToRoom(_currentRoomIndex);
			_portalCooldown = 0.25f;
		}
	}

	private void LockCameraToRoom(Vector2 roomIndex)
	{
		if (_map == null)
		{
			return;
		}

		ConfigureCameraBounds(_map.GetRoomBoundsPixels(roomIndex));
	}
}
