using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using QuestFantasy.Characters.PlayerSystems;
using QuestFantasy.Core.Data;
using QuestFantasy.Core.Data.Assets;
using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Items;
using QuestFantasy.Core.Data.Skills;

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
        [Export] public string AttackFrame1Path = "res://Assets/Characters/slash.png";
        [Export] public string AttackFrame2Path = "res://Assets/Characters/slash1.png";
        [Export] public string AttackFrame3Path = "res://Assets/Characters/slash2.png";

        // ==================== Character Systems ====================
        public Jobs CurrentJob { get; private set; }

        // Subsystems
        private PlayerCombatSystem _combatSystem;
        private PlayerInventorySystem _inventorySystem;
        private PlayerEquipmentSystem _equipmentSystem;
        private PlayerAnimationConfig _animationConfig;
        private PlayerConfigValidator.PlayerConfig _playerConfig;

        // Death state
        private Texture _deadTexture;
        private bool _isDead = false;

        // Hit state
        private Texture _hitTexture;

        // Invincibility state
        private int _damageCooldownFrames = 0;
        private float _respawnInvincibilityTimer = 0f;

        // Previously exposed properties now delegated to subsystems
        public int Experience => _inventorySystem?.Experience ?? 0;
        public int Gold => _inventorySystem?.Gold ?? 0;
        public Weapon EquippedWeapon => _equipmentSystem?.EquippedWeapon;
        public EquippedItems EquippedItems => _equipmentSystem?.EquippedItems;

        // Events - delegated from subsystems
        public event Action<int> OnExperienceChanged;
        public event Action<int> OnGoldChanged;
        public event Action<Item> OnInventoryChanged;
        public event Action<int> OnLevelChanged;
        public event Action<int, int> OnHpChanged;
        public event Action OnDied;
        public event Action<Vector2, string> OnRoomEntered;

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
        private Vector2 _lastKnownRoomIndex = new Vector2(float.MinValue, float.MinValue);

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
            BindHpEvent();

            // Initialize combat system
            _combatSystem = new PlayerCombatSystem();
            _combatSystem.Initialize();
            _combatSystem.OnAttackPerformed += (skillName) =>
            {
                GD.Print($"[Player] Used skill: {skillName}");
            };

            // Initialize inventory system
            _inventorySystem = new PlayerInventorySystem(initialGold: 0, maxInventorySlots: 0);
            _inventorySystem.OnExperienceChanged += (exp) => OnExperienceChanged?.Invoke(exp);
            _inventorySystem.OnGoldChanged += (gold) => OnGoldChanged?.Invoke(gold);
            _inventorySystem.OnInventoryChanged += (item) => OnInventoryChanged?.Invoke(item);

            // Initialize equipment system
            _equipmentSystem = new PlayerEquipmentSystem();
            _equipmentSystem.OnEquipmentChanged += UpdateAttributes;

            // Initialize controllers
            _animationController = new PlayerAnimationController(_animationSystem, _animationConfig);
            _physicsController = new PlayerPhysicsController(_movementController, _roomTracker, _cameraManager);
            _physicsController.OnRoomChanged += HandleRoomChangedFromPhysics;
            _combatController = new PlayerCombatController(_combatSystem, _inputHandler, _animationController);
            _interactionController = new PlayerInteractionController(_inputHandler, _physicsController);
        }

        /// <summary>
        /// Initialize entity rendering and player systems
        /// </summary>
        private void InitializeEntity()
        {
            _inputHandler.EnsureInteractInputAction();
            _inputHandler.EnsureSkillInputActions();

            // Center the sprite on the player position
            Offset = -GetBodySizePixels() / 2f;

            _cameraManager.Initialize(this, CameraZoom);
            _animationSystem.Initialize(this,
                _animationConfig.StandFrame1Path, _animationConfig.StandFrame2Path,
                _animationConfig.WalkFrame1Path, _animationConfig.WalkFrame2Path,
                _animationConfig.AttackFrame1Path, _animationConfig.AttackFrame2Path, _animationConfig.AttackFrame3Path,
                GetBodySizePixels());

            _deadTexture = GD.Load<Texture>("res://Assets/Characters/down.png");
            _hitTexture = GD.Load<Texture>("res://Assets/Characters/hit.png");

            // Set stats according to requirements
            if (Attributes != null)
            {
                Attributes.TotalAtk = 1;
            }

            Update();
        }

        /// <summary>
        /// Get a read-only list of current skills (for external querying)
        /// </summary>
        public IReadOnlyList<Skills> GetCurrentSkills()
        {
            return _combatSystem?.CurrentSkills ?? new List<Skills>();
        }

        public int GetSelectedSkillIndex()
        {
            return _combatController?.SelectedSkillIndex ?? 0;
        }

        /// <summary>
        /// Get a read-only list of inventory items
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<Item> InventoryItems =>
            _inventorySystem?.Inventory?.Items.AsReadOnly() ?? new System.Collections.Generic.List<Item>().AsReadOnly();

        public System.Collections.Generic.IReadOnlyList<Item> DiscardedItems =>
            _inventorySystem?.Discarded?.Items.AsReadOnly() ?? new System.Collections.Generic.List<Item>().AsReadOnly();

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
                _lastKnownRoomIndex = _roomTracker.CurrentRoomIndex;
            }
        }

        public void ConfigureCameraBounds(Rect2 worldBounds)
        {
            _cameraManager.ConfigureBounds(worldBounds);
        }

        public PlayerPhysicsController GetCharacterController()
        {
            return _physicsController;
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

        public override void TakeDamage(int damage)
        {
            if (_respawnInvincibilityTimer > 0f) return;
            if (_damageCooldownFrames > 0) return;

            _damageCooldownFrames = 6; // 0.1 seconds at 60 FPS processing

            base.TakeDamage(damage);
            if (!_isDead && Attributes?.HP != null && Attributes.HP.IsAlive)
            {
                _animationController?.PlayHitAnimation(_hitTexture, 0.2f);
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_isDead)
            {
                return;
            }

            if (_damageCooldownFrames > 0)
            {
                _damageCooldownFrames--;
            }

            if (_respawnInvincibilityTimer > 0f)
            {
                _respawnInvincibilityTimer -= delta;
                if (_respawnInvincibilityTimer <= 0f)
                {
                    Modulate = new Color(1f, 1f, 1f, 1f);
                }
                else if (_respawnInvincibilityTimer <= 1.0f)
                {
                    // Flashing effect in the last second
                    float flash = Mathf.Sin(_respawnInvincibilityTimer * 30f) * 0.5f + 0.5f;
                    Modulate = new Color(1f, 1f, 0.5f, 0.5f + flash * 0.5f);
                }
                else
                {
                    // Solid golden glow
                    Modulate = new Color(1f, 0.9f, 0.4f, 1f);
                }
            }

            if (Attributes != null && Attributes.HP != null && !Attributes.HP.IsAlive)
            {
                Die();
                return;
            }

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

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            Modulate = new Color(1f, 1f, 1f, 1f);
            GD.Print("[Player] Died");
            _animationController?.PlayDeadAnimation(_deadTexture);
            OnDied?.Invoke();
        }

        public void Respawn()
        {
            _isDead = false;
            int maxHp = Attributes?.HP?.MaxHP ?? 100;
            Attributes.HP.SetMaxHPAndCurrentHP(maxHp, maxHp);
            Position = _map?.GetSpawnWorldPosition() ?? Position;
            _animationController?.Revive();
            _respawnInvincibilityTimer = 3.0f;
            Modulate = new Color(1f, 0.9f, 0.4f, 1f);
            GD.Print("[Player] Respawned");
            Update();
        }

        public void SetLevel(int level)
        {
            int normalized = Mathf.Max(1, level);
            if (Level == normalized)
            {
                return;
            }

            Level = normalized;
            OnLevelChanged?.Invoke(normalized);
        }

        public void ApplyProfile(PlayerProfileSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            SetLevel(snapshot.Level);
            Attributes?.HP?.SetMaxHPAndCurrentHP(snapshot.HpMax, snapshot.HpCurrent);
            _inventorySystem?.SetSnapshot(snapshot.Experience, snapshot.Gold);

            if (snapshot.HasInventoryItemsPayload || snapshot.HasDiscardedItemsPayload)
            {
                _inventorySystem?.ReplaceSnapshot(
                    snapshot.HasInventoryItemsPayload
                        ? PlayerItemSnapshotCodec.DecodeMany(snapshot.InventoryItems)
                        : null,
                    snapshot.HasDiscardedItemsPayload
                        ? PlayerItemSnapshotCodec.DecodeMany(snapshot.DiscardedItems)
                        : null,
                    notify: true,
                    replaceInventory: snapshot.HasInventoryItemsPayload,
                    replaceDiscarded: snapshot.HasDiscardedItemsPayload);
            }

            _combatSystem?.SetSkills(BuildSkillsFromSnapshot(snapshot.Skills));

            // Re-broadcast HP to refresh HUD after profile application.
            if (Attributes?.HP != null)
            {
                OnHpChanged?.Invoke(Attributes.HP.CurrentHP, Attributes.HP.MaxHP);
            }
        }

        public PlayerProfileSnapshot BuildProfileSnapshot()
        {
            var snapshot = new PlayerProfileSnapshot
            {
                Level = (int)Math.Max(1, Level),
                Experience = Experience,
                Gold = Gold,
                HpMax = Attributes?.HP?.MaxHP ?? 100,
                HpCurrent = Attributes?.HP?.CurrentHP ?? 100,
                Skills = GetSkillSnapshots().ToList(),
                InventoryItems = PlayerItemSnapshotCodec.EncodeMany(_inventorySystem?.Inventory?.Items),
                DiscardedItems = PlayerItemSnapshotCodec.EncodeMany(_inventorySystem?.Discarded?.Items),
            };

            return snapshot;
        }

        public IReadOnlyList<PlayerSkillSnapshot> GetSkillSnapshots()
        {
            var result = new List<PlayerSkillSnapshot>();
            var currentSkills = _combatSystem?.CurrentSkills;
            if (currentSkills == null)
            {
                return result;
            }

            for (int i = 0; i < currentSkills.Count; i++)
            {
                var skill = currentSkills[i];
                if (skill == null)
                {
                    continue;
                }

                result.Add(new PlayerSkillSnapshot
                {
                    SkillId = ResolveSkillId(skill),
                    Name = skill.Name,
                    CooldownSeconds = skill.GetCooldownDuration(),
                    RemainingCooldownSeconds = skill.CoolDown.RemainingTime,
                    DisplayOrder = i,
                });
            }

            return result;
        }

        private List<Skills> BuildSkillsFromSnapshot(IReadOnlyList<PlayerSkillSnapshot> snapshots)
        {
            var skills = new List<Skills>();
            if (snapshots == null)
            {
                snapshots = new List<PlayerSkillSnapshot>();
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot == null)
                {
                    continue;
                }

                if (string.Equals(snapshot.SkillId, "basic_attack", StringComparison.OrdinalIgnoreCase))
                {
                    var basicAttack = new BasicAttackSkill
                    {
                        EffectRenderer = new BasicAttackEffectRenderer(),
                    };
                    skills.Add(basicAttack);
                    continue;
                }

                if (string.Equals(snapshot.SkillId, "bow_attack", StringComparison.OrdinalIgnoreCase))
                {
                    skills.Add(new BowAttackSkill());
                    continue;
                }

                if (string.Equals(snapshot.SkillId, "fireball", StringComparison.OrdinalIgnoreCase))
                {
                    skills.Add(new FireballSkill());
                    continue;
                }

                var remoteSkill = new RemoteSkill(
                    snapshot.SkillId,
                    snapshot.Name,
                    snapshot.CooldownSeconds);
                skills.Add(remoteSkill);
            }

            EnsureAdventurerCoreSkills(skills);

            return skills;
        }

        private void BindHpEvent()
        {
            if (Attributes?.HP == null)
            {
                return;
            }

            Attributes.HP.OnChanged -= HandleHpChanged;
            Attributes.HP.OnChanged += HandleHpChanged;
        }

        private void HandleHpChanged(int current, int max)
        {
            OnHpChanged?.Invoke(current, max);
        }

        private static string ResolveSkillId(Skills skill)
        {
            if (skill is BasicAttackSkill)
            {
                return "basic_attack";
            }

            if (skill is BowAttackSkill)
            {
                return "bow_attack";
            }

            if (skill is FireballSkill)
            {
                return "fireball";
            }

            if (skill is RemoteSkill remoteSkill)
            {
                return remoteSkill.SkillId;
            }

            return (skill.Name ?? "skill")
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_");
        }

        private static void EnsureAdventurerCoreSkills(List<Skills> skills)
        {
            bool hasSword = false;
            bool hasBow = false;
            bool hasFireball = false;

            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] is BasicAttackSkill)
                {
                    hasSword = true;
                }
                else if (skills[i] is BowAttackSkill)
                {
                    hasBow = true;
                }
                else if (skills[i] is FireballSkill)
                {
                    hasFireball = true;
                }
            }

            if (!hasSword)
            {
                var basicAttack = new BasicAttackSkill
                {
                    EffectRenderer = new BasicAttackEffectRenderer(),
                };
                skills.Insert(0, basicAttack);
            }

            if (!hasBow)
            {
                skills.Add(new BowAttackSkill());
            }

            if (!hasFireball)
            {
                skills.Add(new FireballSkill());
            }
        }

        private void HandleRoomChangedFromPhysics(Vector2 roomIndex, string reason)
        {
            _lastKnownRoomIndex = roomIndex;
            OnRoomEntered?.Invoke(roomIndex, reason ?? "room_enter");
            GD.Print($"[ProgressSync] Entered room ({roomIndex.x}, {roomIndex.y}), reason={reason}.");
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

        public bool RemoveItemByInstanceId(string instanceId)
        {
            return _inventorySystem?.RemoveItemByInstanceId(instanceId) ?? false;
        }

        public bool DiscardItem(Item item)
        {
            return _inventorySystem?.DiscardItem(item) ?? false;
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