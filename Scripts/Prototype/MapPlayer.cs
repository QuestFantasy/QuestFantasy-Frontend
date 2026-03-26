using Godot;

/// <summary>
/// Main controller class for the player character.
/// 
/// Responsibilities:
/// - Input handling
/// - Movement and animation updates
/// - Camera management
/// - Room and portal state logic
/// 
/// System Components: InputHandler, MovementController, AnimationSystem, CameraManager, RoomTracker
/// </summary>
public class MapPlayer : Node2D
{
    [Export] public float MoveSpeed = 240f;
    [Export] public Vector2 BodySizeInTiles = new Vector2(1.0f, 1.9f);
    // Collision-only scale; keeps visuals unchanged while allowing tighter movement through gaps.
    [Export] public Vector2 CollisionBodyScale = new Vector2(0.88f, 0.94f);
    [Export] public Vector2 CameraZoom = new Vector2(0.7f, 0.7f);
    [Export] public float WalkAnimationFps = 10f;
    [Export] public string WalkFrame1Path = "res://Assets/Characters/character_R1.png";
    [Export] public string WalkFrame2Path = "res://Assets/Characters/character_R2.png";
    [Export] public string WalkFrame3Path = "res://Assets/Characters/character_R3.png";

    private Map _map;
    private readonly PlayerInputHandler _inputHandler = new PlayerInputHandler();
    private readonly PlayerMovementController _movementController = new PlayerMovementController();
    private readonly PlayerAnimationSystem _animationSystem = new PlayerAnimationSystem();
    private readonly PlayerCameraManager _cameraManager = new PlayerCameraManager();
    private readonly PlayerRoomTracker _roomTracker = new PlayerRoomTracker();
    private float _lastFacingX = 1f;

    public override void _Ready()
    {
        _inputHandler.EnsureInteractInputAction();
        _cameraManager.Initialize(this, CameraZoom);
        _animationSystem.Initialize(this, WalkFrame1Path, WalkFrame2Path, WalkFrame3Path, GetBodySizePixels());

        Update();

        SetPhysicsProcess(true);
    }

    public void SetMap(Map map)
    {
        _map = map;
        _animationSystem.RefreshScale(GetBodySizePixels());
        Update();

        if (_map != null)
        {
            _roomTracker.InitializeFromPosition(_map, Position);
            _cameraManager.LockToRoom(_map, _roomTracker.CurrentRoomIndex);
        }
    }

    public void ConfigureCameraBounds(Rect2 worldBounds)
    {
        _cameraManager.ConfigureBounds(worldBounds);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (_map == null)
        {
            return;
        }

        _roomTracker.Tick(delta);

        if (_inputHandler.IsRespawnPressed())
        {
            RespawnAtCurrentRoomStart();
        }

        if (_inputHandler.IsInteractPressed())
        {
            if (_map.TryOpenNearbyBox(Position))
            {
                _roomTracker.InterferPortalWithInteraction();
            }
        }

        Vector2 input = _inputHandler.GetMovementInput();

        if (input.LengthSquared() > 0)
        {
            if (Mathf.Abs(input.x) > 0.01f)
            {
                _lastFacingX = input.x;
            }

            Vector2 velocity = input.Normalized() * MoveSpeed;
            _movementController.TryMove(this, _map, velocity * delta, GetCollisionBodySizePixels());
            _animationSystem.UpdateWalkAnimation(true, delta, WalkAnimationFps, _lastFacingX);
        }
        else
        {
            _animationSystem.UpdateWalkAnimation(false, delta, WalkAnimationFps, _lastFacingX);
        }

        TryHandlePortalTeleport();
        UpdateRoomStateAndHandleExit();
    }

    public override void _Draw()
    {
        _animationSystem.DrawFallback(this, GetBodySizePixels());
    }

    private void UpdateRoomStateAndHandleExit()
    {
        if (_map == null)
        {
            return;
        }

        // Prioritize handling room exit (completion)
        if (_roomTracker.TryHandleExit(_map, Position, out Vector2 exitNextPosition))
        {
            TransitionToNewLocation(exitNextPosition);
            return;  // Prevent simultaneous room transitions within the same frame
        }

        // Then handle intra-room transitions
        if (_roomTracker.TryUpdateRoomByPosition(_map, Position))
        {
            LockCameraToRoom(_roomTracker.CurrentRoomIndex);
        }
    }

    /// <summary>
    /// Atomically transitions to a new location, ensuring position, room index, and camera bounds are updated synchronously.
    /// </summary>
    private void TransitionToNewLocation(Vector2 newWorldPosition)
    {
        Position = newWorldPosition;
        _roomTracker.InitializeFromPosition(_map, Position);
        LockCameraToRoom(_roomTracker.CurrentRoomIndex);
    }

    private void RespawnAtCurrentRoomStart()
    {
        if (!_roomTracker.TryRespawnAtCurrentRoomStart(_map, out Vector2 nextPosition))
        {
            return;
        }

        Position = nextPosition;
        LockCameraToRoom(_roomTracker.CurrentRoomIndex);
    }

    private void TryHandlePortalTeleport()
    {
        if (_roomTracker.TryHandlePortal(_map, Position, out Vector2 destinationWorld))
        {
            TransitionToNewLocation(destinationWorld);
        }
    }

    private void LockCameraToRoom(Vector2 roomIndex)
    {
        _cameraManager.LockToRoom(_map, roomIndex);
    }

    private Vector2 GetBodySizePixels()
    {
        return BodySizeInTiles * GetTileSizePixels();
    }

    private float GetTileSizePixels()
    {
        return _map != null ? _map.TileSize : 24f;
    }

    private Vector2 GetCollisionBodySizePixels()
    {
        Vector2 collisionScale = new Vector2(
            Mathf.Clamp(CollisionBodyScale.x, 0.5f, 1f),
            Mathf.Clamp(CollisionBodyScale.y, 0.5f, 1f));
        return GetBodySizePixels() * collisionScale;
    }

    private void RefreshVisualScale()
    {
        _animationSystem.RefreshScale(GetBodySizePixels());
    }
}