using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters.PlayerSystems;
using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Items;
using QuestFantasy.Core.Data.Skills;
using QuestFantasy.Systems.Inventory;

namespace QuestFantasy.Characters
{
    /// <summary>
    /// Main Player character class integrating:
    /// - Movement and collision detection
    /// - Animation system
    /// - Input handling
    /// - Camera management
    /// - Room tracking
    /// - Skill system with basic attack
    /// </summary>
    public class Player : Character
    {
        [Export] public float MoveSpeed = 240f;
        [Export] public Vector2 BodySizeInTiles = new Vector2(1.0f, 1.9f);
        [Export] public Vector2 CollisionBodyScale = new Vector2(0.88f, 0.94f);
        [Export] public Vector2 CameraZoom = new Vector2(0.7f, 0.7f);
        [Export] public float WalkAnimationFps = 10f;
        [Export] public string WalkFrame1Path = "res://Assets/Characters/character_R1.png";
        [Export] public string WalkFrame2Path = "res://Assets/Characters/character_R2.png";
        [Export] public string WalkFrame3Path = "res://Assets/Characters/character_R3.png";
        [Export] public float SpeedMultiplier = 50f; // Legacy: pixels per spd point

        public Jobs Job { get; private set; }
        public int Exp { get; private set; }
        public Jobs CurrentJob { get; private set; }
        public List<Skills> CurrentSkills { get; private set; } = new List<Skills>();
        private Weapon EquippedWeapon { get; set; }
        private EquippedItems Equipped { get; set; }
        public int Gold { get; private set; }

        private readonly Bag Inventory = new Bag();

        // Prototype systems integration
        private readonly PlayerInputHandler _inputHandler = new PlayerInputHandler();
        private readonly PlayerMovementController _movementController = new PlayerMovementController();
        private readonly PlayerAnimationSystem _animationSystem = new PlayerAnimationSystem();
        private readonly PlayerCameraManager _cameraManager = new PlayerCameraManager();
        private readonly PlayerRoomTracker _roomTracker = new PlayerRoomTracker();
        private float _lastFacingX = 1f;

        private Map _map;

        public override void _Ready()
        {
            InitializeEntity();
            InitializePlayerSystems();
            InitializeSkills();
            SetPhysicsProcess(true);
        }

        /// <summary>
        /// Initialize entity and animation systems
        /// </summary>
        private void InitializeEntity()
        {
            _inputHandler.EnsureInteractInputAction();
            _cameraManager.Initialize(this, CameraZoom);
            _animationSystem.Initialize(this, WalkFrame1Path, WalkFrame2Path, WalkFrame3Path, GetBodySizePixels());
            Update();
        }

        /// <summary>
        /// Initialize all player-controlled systems
        /// </summary>
        private void InitializePlayerSystems()
        {
            if (_map != null)
            {
                _animationSystem.RefreshScale(GetBodySizePixels());
                _roomTracker.InitializeFromPosition(_map, Position);
                _cameraManager.LockToRoom(_map, _roomTracker.CurrentRoomIndex);
            }
        }

        /// <summary>
        /// Initialize player skills. Called once during _Ready()
        /// </summary>
        private void InitializeSkills()
        {
            // Add basic attack skill by default
            var basicAttack = new QuestFantasy.Core.Data.Skills.BasicAttackSkill();
            basicAttack.EffectRenderer = new Core.Data.Assets.BasicAttackEffectRenderer();
            CurrentSkills.Add(basicAttack);
        }

        /// <summary>
        /// Set the map reference and initialize room tracking
        /// </summary>
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

        public override void UpdateAttributes()
        {
            Attributes.TotalAtk = (Job?.BaseAbilities?.Atk ?? 0) + (EquippedWeapon?.WeaponAbilities?.Atk ?? 0) + (Equipped?.TotalAtk() ?? 0);
            Attributes.TotalDef = (Job?.BaseAbilities?.Def ?? 0) + (EquippedWeapon?.WeaponAbilities?.Def ?? 0) + (Equipped?.TotalDef() ?? 0);
            Attributes.TotalSpd = (Job?.BaseAbilities?.Spd ?? 0) + (EquippedWeapon?.WeaponAbilities?.Spd ?? 0) + (Equipped?.TotalSpd() ?? 0);
            Attributes.TotalVit = (Job?.BaseAbilities?.Vit ?? 0) + (EquippedWeapon?.WeaponAbilities?.Vit ?? 0) + (Equipped?.TotalVit() ?? 0);
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_map == null)
            {
                return;
            }

            // Update room tracking
            _roomTracker.Tick(delta);

            // Handle respawn input
            if (_inputHandler.IsRespawnPressed())
            {
                RespawnAtCurrentRoomStart();
            }

            // Handle interaction input
            if (_inputHandler.IsInteractPressed())
            {
                if (_map.TryOpenNearbyBox(Position))
                {
                    _roomTracker.InterferPortalWithInteraction();
                }
            }

            // Handle skill input (Space for basic attack, or customizable)
            HandleSkillInput();

            // Get movement input and apply movement
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

            // Handle portal and room transitions
            TryHandlePortalTeleport();
            UpdateRoomStateAndHandleExit();

            // Update skill cooldowns
            foreach (var skill in CurrentSkills)
            {
                skill.CoolDown.Update(delta);
            }
        }

        /// <summary>
        /// Handle skill activation input
        /// </summary>
        private void HandleSkillInput()
        {
            // Use Space key for basic attack (first skill)
            if (Input.IsActionJustPressed("ui_accept") && CurrentSkills.Count > 0)
            {
                // Find nearest enemy within skill range to attack
                Character nearestTarget = FindNearestEnemyInRange(CurrentSkills[0]);
                if (nearestTarget != null)
                {
                    CurrentSkills[0].TryExecute(this, nearestTarget);
                }
            }
        }

        /// <summary>
        /// Find nearest enemy/character within skill range
        /// </summary>
        private Character FindNearestEnemyInRange(Skills skill)
        {
            if (_map == null)
                return null;

            // TODO: Implement based on your game's NPC/Monster tracking system
            // For now, return null
            return null;
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
        }

        private void LockCameraToRoom(Vector2 roomIndex)
        {
            if (_map == null)
            {
                return;
            }

            _cameraManager.LockToRoom(_map, roomIndex);
        }

        private void TryHandlePortalTeleport()
        {
            if (_map == null || !_roomTracker.IsPortalReady)
            {
                return;
            }

            if (_roomTracker.TryHandlePortal(_map, Position, out Vector2 destinationPosition))
            {
                TransitionToNewLocation(destinationPosition);
            }
        }

        private Vector2 GetBodySizePixels()
        {
            float tileSize = (_map != null) ? _map.TileSize : 24f;
            return BodySizeInTiles * tileSize;
        }

        private Vector2 GetCollisionBodySizePixels()
        {
            return GetBodySizePixels() * CollisionBodyScale;
        }

        /// <summary>
        /// Use a specific skill on a target
        /// </summary>
        public bool UseSkill(int skillIndex, Character target)
        {
            if (skillIndex < 0 || skillIndex >= CurrentSkills.Count)
                return false;

            return CurrentSkills[skillIndex].TryExecute(this, target);
        }

        /// <summary>
        /// Add a new skill to the player's skill list
        /// </summary>
        public void LearnSkill(Skills skill)
        {
            if (skill != null && !CurrentSkills.Contains(skill))
            {
                CurrentSkills.Add(skill);
            }
        }

        /// <summary>
        /// Legacy Move implementation for backward compatibility
        /// </summary>
        public void Move(float delta)
        {
            // This is maintained for backward compatibility but is now handled by _PhysicsProcess
            // The prototype movement controller is the main system
        }
    }
}