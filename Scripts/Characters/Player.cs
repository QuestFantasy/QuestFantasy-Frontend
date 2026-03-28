using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters.PlayerSystems;
using QuestFantasy.Core.Data;
using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Items;

namespace QuestFantasy.Characters
{
    /// <summary>
    /// Main Player character class.
    /// Orchestrates modular systems: combat, inventory, equipment, animation, input, movement, camera, and room tracking.
    /// Delegates specific responsibilities to dedicated subsystems.
    /// </summary>
    public class Player : Character
    {
        // ==================== Configuration ====================
        [Export] public float MoveSpeed = GameConstants.PLAYER_DEFAULT_MOVE_SPEED;
        [Export] public Vector2 BodySizeInTiles = GameConstants.PLAYER_BODY_SIZE_IN_TILES;
        [Export] public Vector2 CollisionBodyScale = GameConstants.PLAYER_COLLISION_SCALE;
        [Export] public Vector2 CameraZoom = GameConstants.PLAYER_CAMERA_DEFAULT_ZOOM;
        [Export] public float SpeedMultiplier = GameConstants.PLAYER_SPEED_TO_PIXELS_MULTIPLIER;

        // ==================== Animation Configuration ====================
        [Export] public string StandFrame1Path = "res://Assets/Characters/stand.png";
        [Export] public string StandFrame2Path = "res://Assets/Characters/stand2.png";
        [Export] public float WalkAnimationFps = GameConstants.PLAYER_WALK_ANIMATION_FPS;
        [Export] public string WalkFrame1Path = "res://Assets/Characters/walk.png";
        [Export] public string WalkFrame2Path = "res://Assets/Characters/walk1.png";
        [Export] public float AttackAnimationFps = GameConstants.PLAYER_ATTACK_ANIMATION_FPS;
        [Export] public string AttackFrame1Path = "res://Assets/Characters/attack.png";
        [Export] public string AttackFrame2Path = "res://Assets/Characters/attack1.png";
        [Export] public string AttackFrame3Path = "res://Assets/Characters/attack2.png";

        // ==================== Character Systems ====================
        public Jobs CurrentJob { get; private set; }
        
        // Subsystems
        private PlayerCombatSystem _combatSystem;
        private PlayerInventorySystem _inventorySystem;
        private PlayerEquipmentSystem _equipmentSystem;
        private PlayerAnimationConfig _animationConfig;
        private PlayerConfigValidator.PlayerConfig _playerConfig;

        // Previously exposed properties now delegated to subsystems
        public int Experience => _inventorySystem?.Experience ?? 0;
        public int Gold => _inventorySystem?.Gold ?? 0;
        public Weapon EquippedWeapon => _equipmentSystem?.EquippedWeapon;
        public EquippedItems EquippedItems => _equipmentSystem?.EquippedItems;

        // Events - delegated from subsystems
        public event Action<int> OnExperienceChanged;
        public event Action<int> OnGoldChanged;
        public event Action<Item> OnInventoryChanged;

        // ==================== Core Controllers ====================
        // Each controller handles a specific aspect of player behavior
        private PlayerPhysicsController _physicsController;
        private PlayerAnimationController _animationController;
        private PlayerCombatController _combatController;
        private PlayerInteractionController _interactionController;

        // ==================== Prototype systems (used by controllers) ====================
        private readonly PlayerInputHandler _inputHandler = new PlayerInputHandler();
        private readonly PlayerMovementController _movementController = new PlayerMovementController();
        private readonly PlayerAnimationSystem _animationSystem = new PlayerAnimationSystem();
        private readonly PlayerCameraManager _cameraManager = new PlayerCameraManager();
        private readonly PlayerRoomTracker _roomTracker = new PlayerRoomTracker();

        private Map _map;

        public override void _Ready()
        {
            // Initialize configuration
            InitializeConfiguration();
            
            // Initialize subsystems
            InitializeSubsystems();
            
            // Initialize entity
            InitializeEntity();
            
            // Set up physics processing
            SetPhysicsProcess(true);
        }

        /// <summary>
        /// Initialize player configuration from exported fields
        /// </summary>
        private void InitializeConfiguration()
        {
            // Create config object from exported fields
            _playerConfig = new PlayerConfigValidator.PlayerConfig
            {
                MoveSpeed = MoveSpeed,
                BodySizeInTiles = BodySizeInTiles,
                CollisionBodyScale = CollisionBodyScale,
                CameraZoom = CameraZoom,
                SpeedMultiplier = SpeedMultiplier
            };

            // Validate all parameters
            PlayerConfigValidator.ValidateAll(_playerConfig);

            // Apply validated values back
            MoveSpeed = _playerConfig.MoveSpeed;
            BodySizeInTiles = _playerConfig.BodySizeInTiles;
            CollisionBodyScale = _playerConfig.CollisionBodyScale;
            CameraZoom = _playerConfig.CameraZoom;
            SpeedMultiplier = _playerConfig.SpeedMultiplier;

            // Initialize animation config
            _animationConfig = new PlayerAnimationConfig
            {
                StandFrame1Path = StandFrame1Path,
                StandFrame2Path = StandFrame2Path,
                WalkAnimationFps = WalkAnimationFps,
                WalkFrame1Path = WalkFrame1Path,
                WalkFrame2Path = WalkFrame2Path,
                AttackAnimationFps = AttackAnimationFps,
                AttackFrame1Path = AttackFrame1Path,
                AttackFrame2Path = AttackFrame2Path,
                AttackFrame3Path = AttackFrame3Path
            };
            
            _animationConfig.Validate();
        }

        /// <summary>
        /// Initialize all player subsystems
        /// </summary>
        private void InitializeSubsystems()
        {
            // Initialize character base
            InitializeCharacter();

            // Initialize combat system
            _combatSystem = new PlayerCombatSystem();
            _combatSystem.Initialize();
            _combatSystem.OnAttackPerformed += (skillName) => 
            {
                GD.Print($"[Player] Used skill: {skillName}");
            };

            // Initialize inventory system
            _inventorySystem = new PlayerInventorySystem(initialGold: 0, maxInventorySlots: 20);
            _inventorySystem.OnExperienceChanged += (exp) => OnExperienceChanged?.Invoke(exp);
            _inventorySystem.OnGoldChanged += (gold) => OnGoldChanged?.Invoke(gold);
            _inventorySystem.OnInventoryChanged += (item) => OnInventoryChanged?.Invoke(item);

            // Initialize equipment system
            _equipmentSystem = new PlayerEquipmentSystem();
            _equipmentSystem.OnEquipmentChanged += UpdateAttributes;

            // Initialize controllers
            _animationController = new PlayerAnimationController(_animationSystem, _animationConfig);
            _physicsController = new PlayerPhysicsController(_movementController, _roomTracker, _cameraManager);
            _combatController = new PlayerCombatController(_combatSystem, _inputHandler, _animationController);
            _interactionController = new PlayerInteractionController(_inputHandler, _physicsController);
        }

        /// <summary>
        /// Initialize entity rendering and player systems
        /// </summary>
        private void InitializeEntity()
        {
            _inputHandler.EnsureInteractInputAction();
            _cameraManager.Initialize(this, CameraZoom);
            _animationSystem.Initialize(this, 
                _animationConfig.StandFrame1Path, _animationConfig.StandFrame2Path,
                _animationConfig.WalkFrame1Path, _animationConfig.WalkFrame2Path,
                _animationConfig.AttackFrame1Path, _animationConfig.AttackFrame2Path, _animationConfig.AttackFrame3Path,
                GetBodySizePixels());
            Update();
        }

        /// <summary>
        /// Get a read-only list of current skills (for external querying)
        /// </summary>
        public IReadOnlyList<Skills> GetCurrentSkills()
        {
            return _combatSystem?.CurrentSkills ?? new List<Skills>();
        }

        /// <summary>
        /// Get a read-only list of inventory items
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<Item> InventoryItems => 
            _inventorySystem?.Inventory?.Items.AsReadOnly() ?? new System.Collections.Generic.List<Item>().AsReadOnly();

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
            if (Attributes == null)
            {
                GD.PrintErr("[Player] Attributes not initialized");
                return;
            }
            
            var jobBonuses = CurrentJob?.BaseAbilities ?? new Abilities();
            var equipmentBonuses = _equipmentSystem?.GetAllEquipmentBonuses() ?? new Abilities();

            Attributes.TotalAtk = jobBonuses.Atk + equipmentBonuses.Atk;
            Attributes.TotalDef = jobBonuses.Def + equipmentBonuses.Def;
            Attributes.TotalSpd = jobBonuses.Spd + equipmentBonuses.Spd;
            Attributes.TotalVit = jobBonuses.Vit + equipmentBonuses.Vit;
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_map == null)
                return;

            // Get current movement input
            Vector2 movementInput = _inputHandler.GetMovementInput();

            // 1. Handle physics and movement
            _physicsController.Update(
                this,
                _map,
                movementInput,
                GetCollisionBodySizePixels(),
                MoveSpeed,
                delta);

            // 2. Handle animations
            _animationController.Update(movementInput, delta);

            // 3. Handle combat and skills
            _combatController.HandleSkillInput(this, _map);

            // 4. Handle environmental interactions
            _interactionController.HandleRespawnInput(this, _map);
            _interactionController.HandleInteractionInput(_map, Position);

            // 5. Update drawing
            Update();
        }

        public override void _Draw()
        {
            _animationSystem.DrawFallback(this, GetBodySizePixels());
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

        // ==================== Helper Properties ====================
        /// <summary>
        /// Check if player is currently attacking
        /// </summary>
        public bool IsAttacking => _animationController?.IsAttacking ?? false;

        /// <summary>
        /// Update skill cooldowns (called by PhysicsController)
        /// </summary>
        public void UpdateSkillCooldowns(float delta)
        {
            _combatSystem?.UpdateSkillCooldowns(delta);
        }

        // ==================== Skill System ====================
        /// <summary>
        /// Use a specific skill
        /// </summary>
        public bool UseSkill(int skillIndex, Character target)
        {
            return _combatSystem?.UseSkill(skillIndex, this, target) ?? false;
        }

        /// <summary>
        /// Learn a new skill
        /// </summary>
        public void LearnSkill(Skills skill)
        {
            _combatSystem?.LearnSkill(skill);
        }

        // ==================== Inventory System ====================
        /// <summary>
        /// Gain experience points
        /// </summary>
        public void GainExperience(int amount)
        {
            _inventorySystem?.GainExperience(amount);
        }

        /// <summary>
        /// Add gold to inventory
        /// </summary>
        public void AddGold(int amount)
        {
            _inventorySystem?.AddGold(amount);
        }

        /// <summary>
        /// Spend gold from inventory
        /// </summary>
        public bool SpendGold(int amount)
        {
            return _inventorySystem?.SpendGold(amount) ?? false;
        }

        /// <summary>
        /// Add item to inventory
        /// </summary>
        public bool AddItem(Item item)
        {
            return _inventorySystem?.AddItem(item) ?? false;
        }

        /// <summary>
        /// Remove item from inventory
        /// </summary>
        public bool RemoveItem(Item item)
        {
            return _inventorySystem?.RemoveItem(item) ?? false;
        }

        // ==================== Equipment System ====================
        /// <summary>
        /// Equip a weapon
        /// </summary>
        public void EquipWeapon(Weapon weapon)
        {
            _equipmentSystem?.EquipWeapon(weapon);
            UpdateAttributes();
        }

        /// <summary>
        /// Unequip current weapon
        /// </summary>
        public void UnequipWeapon()
        {
            _equipmentSystem?.UnequipWeapon();
            UpdateAttributes();
        }
    }
}