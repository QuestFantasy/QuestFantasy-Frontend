using Godot;

public class PrototypePlayer : Node2D
{
	[Export] public float MoveSpeed = 240f;
	[Export] public Vector2 BodySizeInTiles = new Vector2(1.0f, 1.9f);
	[Export] public Vector2 CameraZoom = new Vector2(0.7f, 0.7f);
	[Export] public float WalkAnimationFps = 10f;
	[Export] public string WalkFrame1Path = "res://Assets/Characters/character_R1.png";
	[Export] public string WalkFrame2Path = "res://Assets/Characters/character_R2.png";
	[Export] public string WalkFrame3Path = "res://Assets/Characters/character_R3.png";

	private PrototypeMap _map;
	private Camera2D _camera;
	private Sprite _sprite;
	private Texture[] _walkFrames;
	private int _walkFrameIndex = 1;
	private float _walkAnimationTimer;
	private bool _hasWalkFrames;
	private Rect2 _pendingCameraBounds;
	private bool _hasPendingBounds;
	private Vector2 _currentRoomIndex = Vector2.Zero;
	private float _lastFacingX = 1f;
	private float _portalCooldown;

	public override void _Ready()
	{
		EnsureInteractInputAction();

		_camera = new Camera2D();
		_camera.Current = true;
		_camera.SmoothingEnabled = true;
		_camera.SmoothingSpeed = 8f;
		_camera.Zoom = CameraZoom;
		AddChild(_camera);

		CreateVisualNode();
		TryLoadWalkFrames();
		ApplyCurrentFrame();

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
		RefreshVisualScale();
		Update();

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

		if (Input.IsActionJustPressed("interact"))
		{
			if (_map.TryOpenNearbyBox(Position))
			{
				_portalCooldown = 0.05f;
			}
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
			if (Mathf.Abs(input.x) > 0.01f)
			{
				_lastFacingX = input.x;
			}

			Vector2 velocity = input.Normalized() * MoveSpeed;
			TryMove(velocity * delta);
			UpdateWalkAnimation(true, delta);
		}
		else
		{
			UpdateWalkAnimation(false, delta);
		}

		TryHandlePortalTeleport();
		UpdateRoomStateAndHandleExit();
	}

	public override void _Draw()
	{
		if (_hasWalkFrames)
		{
			return;
		}

		Vector2 bodySize = GetBodySizePixels();
		Rect2 rect = new Rect2(-bodySize / 2f, bodySize);
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
		Vector2 bodySize = GetBodySizePixels();
		return new Rect2(centerPosition - bodySize / 2f, bodySize);
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

		if (!_map.IsAtRoomExit(Position, _currentRoomIndex))
		{
			return;
		}

		// Reaching an exit now always builds a fresh map with a fresh random seed.
		_map.RegenerateWithRandomSeed();
		_currentRoomIndex = Vector2.Zero;
		Position = _map.GetSpawnWorldPosition();
		LockCameraToRoom(_currentRoomIndex);
		_portalCooldown = 0.2f;
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

	private void CreateVisualNode()
	{
		_sprite = new Sprite();
		_sprite.Centered = true;
		AddChild(_sprite);
	}

	private void TryLoadWalkFrames()
	{
		Texture frame1 = GD.Load<Texture>(WalkFrame1Path);
		Texture frame2 = GD.Load<Texture>(WalkFrame2Path);
		Texture frame3 = GD.Load<Texture>(WalkFrame3Path);

		_hasWalkFrames = frame1 != null && frame2 != null && frame3 != null;
		if (!_hasWalkFrames)
		{
			GD.Print("PrototypePlayer: walking frames not found, using fallback debug rectangle.");
			return;
		}

		_walkFrames = new[] { frame1, frame2, frame3 };
		RefreshVisualScale();
	}

	private void UpdateWalkAnimation(bool isMoving, float delta)
	{
		if (!_hasWalkFrames)
		{
			return;
		}

		if (isMoving)
		{
			float frameDuration = 1f / Mathf.Max(1f, WalkAnimationFps);
			_walkAnimationTimer += delta;
			if (_walkAnimationTimer >= frameDuration)
			{
				_walkAnimationTimer -= frameDuration;
				_walkFrameIndex = (_walkFrameIndex + 1) % _walkFrames.Length;
			}
		}
		else
		{
			_walkAnimationTimer = 0f;
			_walkFrameIndex = 1;
		}

		ApplyCurrentFrame();
	}

	private void ApplyCurrentFrame()
	{
		if (!_hasWalkFrames)
		{
			return;
		}

		_sprite.Texture = _walkFrames[_walkFrameIndex];
		_sprite.FlipH = _lastFacingX < 0f;
	}

	private Vector2 GetBodySizePixels()
	{
		return BodySizeInTiles * GetTileSizePixels();
	}

	private float GetTileSizePixels()
	{
		return _map != null ? _map.TileSize : 24f;
	}

	private void RefreshVisualScale()
	{
		if (!_hasWalkFrames || _walkFrames == null || _walkFrames.Length == 0)
		{
			return;
		}

		Vector2 textureSize = _walkFrames[0].GetSize();
		if (textureSize.x <= 0f || textureSize.y <= 0f)
		{
			return;
		}

		Vector2 bodySize = GetBodySizePixels();
		_sprite.Scale = new Vector2(bodySize.x / textureSize.x, bodySize.y / textureSize.y);
	}

	private void EnsureInteractInputAction()
	{
		if (!InputMap.HasAction("interact"))
		{
			InputMap.AddAction("interact");
		}

		if (InputMap.GetActionList("interact").Count > 0)
		{
			return;
		}

		var interactKey = new InputEventKey();
		interactKey.Scancode = (uint)KeyList.F;
		InputMap.ActionAddEvent("interact", interactKey);
	}
}
