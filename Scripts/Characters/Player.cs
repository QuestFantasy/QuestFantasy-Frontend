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
using QuestFantasy.Core.Systems.StatusEffects;
using QuestFantasy.UI;

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
        [Export] public string StandFrame1Path = "res://Assets/Characters/adventurer/stand.png";
        [Export] public string StandFrame2Path = "res://Assets/Characters/adventurer/stand2.png";
        [Export] public float WalkAnimationFps = GameConstants.PLAYER_WALK_ANIMATION_FPS;
        [Export] public string WalkFrame1Path = "res://Assets/Characters/adventurer/walk.png";
        [Export] public string WalkFrame2Path = "res://Assets/Characters/adventurer/walk1.png";
        [Export] public float AttackAnimationFps = GameConstants.PLAYER_ATTACK_ANIMATION_FPS;
        [Export] public string AttackFrame1Path = "res://Assets/Characters/adventurer/slash.png";
        [Export] public string AttackFrame2Path = "res://Assets/Characters/adventurer/slash1.png";
        [Export] public string AttackFrame3Path = "res://Assets/Characters/adventurer/slash2.png";

        // ==================== Character Systems ====================
        public Jobs CurrentJob { get; private set; }

        /// <summary>The player's currently active class. Defaults to Adventurer.</summary>
        public PlayerClass PlayerClass { get; private set; } = PlayerClass.Adventurer;

        /// <summary>Fired whenever the class changes, e.g. so the HUD can update skill slots.</summary>
        public event Action<PlayerClass> OnClassChanged;

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
        private float _defenseStanceTimer = 0f;

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
        public PlayerInputHandler InputHandler => _inputHandler;
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

            _deadTexture = GD.Load<Texture>("res://Assets/Characters/adventurer/down.png");
            _hitTexture = GD.Load<Texture>("res://Assets/Characters/adventurer/hit.png");

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

        public void ActivateDefenseStance(float duration)
        {
            _defenseStanceTimer = duration;
            _animationController?.PlayDefenseAnimation();
            GD.Print($"[Player] Defense Stance activated for {duration} seconds.");
        }

        /// <summary>
        /// Set the map reference and initialize room tracking
        /// </summary>
        public void SetMap(Map map)
        {
            _map = map;
            float multiplier = PlayerClassData.GetSpritePaths(PlayerClass).ScaleMultiplier;
            _animationSystem.RefreshScale(GetBodySizePixels() * multiplier);
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

        public override void TakeDamage(int damage, Character source = null)
        {
            if (_defenseStanceTimer > 0f)
            {
                if (source != null && source.Attributes?.HP?.IsAlive == true)
                {
                    source.TakeDamage(Attributes.EffectiveAtk);
                    _animationController?.PlayDefenseCounterAnimation(0.2f);
                }
                return;
            }

            if (_respawnInvincibilityTimer > 0f) return;
            if (_damageCooldownFrames > 0) return;

            _damageCooldownFrames = 6; // 0.1 seconds at 60 FPS processing

            base.TakeDamage(damage, source);
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

            // Tick all active status effects (Burn, Bleed, Stun, etc.)
            EffectManager?.Update(this, delta);

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
            else
            {
                // Apply status effect color overlay when not in respawn invincibility
                Modulate = EffectManager?.GetModulateColor() ?? new Color(1f, 1f, 1f, 1f);
            }

            if (Attributes != null && Attributes.HP != null && !Attributes.HP.IsAlive)
            {
                Die();
                return;
            }

            if (_map == null)
                return;

            if (_defenseStanceTimer > 0f)
            {
                _defenseStanceTimer -= delta;
                if (_defenseStanceTimer <= 0f)
                {
                    _animationController?.StopDefenseAnimation();
                }
            }

            // Get current movement input
            Vector2 movementInput = _defenseStanceTimer > 0f ? Vector2.Zero : _inputHandler.GetMovementInput();

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
            if (_map.HasNearbyBox(Position, out Vector2 boxWorld))
            {
                InteractionButtonUI.Instance?.Show("🔓 Open", boxWorld);
            }

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

            if (snapshot.HpCurrent > 0)
            {
                _isDead = false;
                _animationController?.Revive();
            }

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

            // Restore equipped items from snapshot
            if (snapshot.HasEquippedItemsPayload && snapshot.EquippedItemsPayload != null)
            {
                RestoreEquippedItems(snapshot.EquippedItemsPayload);
            }

            // Apply class BEFORE restoring skills so restrictions are already set
            PlayerClass restoredClass = PlayerClassData.Deserialize(snapshot.ClassName);
            SetClass(restoredClass, rebuildSkills: false);

            var newSkills = BuildSkillsFromSnapshot(snapshot.Skills, restoredClass);

            // Check if the new list of skills matches our current skills in ID and order exactly.
            // If they match, keep the current live skill instances to avoid resetting their local cooldowns.
            var currentSkills = _combatSystem?.CurrentSkills;
            bool skillsMatch = false;
            if (currentSkills != null && currentSkills.Count == newSkills.Count)
            {
                skillsMatch = true;
                for (int i = 0; i < currentSkills.Count; i++)
                {
                    if (ResolveSkillId(currentSkills[i]) != ResolveSkillId(newSkills[i]))
                    {
                        skillsMatch = false;
                        break;
                    }
                }
            }

            if (!skillsMatch)
            {
                _combatSystem?.SetSkills(newSkills);
            }

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
                EquippedItemsPayload = BuildEquippedItemsPayload(),
                HasInventoryItemsPayload = true,
                HasDiscardedItemsPayload = true,
                HasEquippedItemsPayload = true,
                ClassName = PlayerClassData.Serialize(PlayerClass),
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

        private List<Skills> BuildSkillsFromSnapshot(IReadOnlyList<PlayerSkillSnapshot> snapshots, PlayerClass cls = PlayerClass.Adventurer)
        {
            var skills = new List<Skills>();
            if (snapshots == null)
            {
                snapshots = new List<PlayerSkillSnapshot>();
            }

            var allowed = PlayerClassData.GetAllowedSkillIds(cls);

            for (int i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot == null)
                {
                    continue;
                }

                // Skip skills the class is not permitted to use
                if (!allowed.Contains(snapshot.SkillId?.ToLowerInvariant() ?? string.Empty))
                {
                    continue;
                }

                var skill = CreateSkillFromId(snapshot.SkillId);
                if (skill == null)
                {
                    // Fall back to remote skill mapping if it's a remote/external skill not recognized locally
                    skill = new RemoteSkill(
                        snapshot.SkillId,
                        snapshot.Name,
                        snapshot.CooldownSeconds);
                }

                if (skill != null)
                {
                    skills.Add(skill);
                }
            }

            EnsureClassCoreSkills(skills, cls);

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

            if (skill is TripleFireballSkill)
            {
                return "triple_fireball";
            }

            if (skill is GiantFireballSkill)
            {
                return "giant_fireball";
            }

            if (skill is TripleArrowSkill)
            {
                return "triple_arrow";
            }

            if (skill is RicochetArrowSkill)
            {
                return "ricochet_arrow";
            }

            if (skill is FlyingSwordSkill)
            {
                return "flying_sword";
            }

            if (skill is DefenseStanceSkill)
            {
                return "defense_stance";
            }

            if (skill is MagicSlashSkill)
            {
                return "magic_slash";
            }

            if (skill is IceSpearSkill)
            {
                return "ice_spear";
            }

            if (skill is DigitArrowSkill)
            {
                return "digit_arrow";
            }

            if (skill is SuperArrowSkill)
            {
                return "super_arrow";
            }

            if (skill is RoundhouseSlashSkill)
            {
                return "roundhouse_slash";
            }

            if (skill is KnightExploseSkill)
            {
                return "knight_explose";
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

        /// <summary>
        /// Ensures the skills list contains exactly the skills the given class is allowed to use.
        /// Adds any missing class-allowed skills, strips any disallowed ones.
        /// </summary>
        private static void EnsureClassCoreSkills(List<Skills> skills, PlayerClass cls)
        {
            if (cls == PlayerClass.Adventurer)
            {
                EnsureAdventurerCoreSkills(skills);
                return;
            }

            var allowed = PlayerClassData.GetAllowedSkillIds(cls);

            // Remove skills that are not allowed for this class
            skills.RemoveAll(s => !allowed.Contains(ResolveSkillId(s)));

            // If the player currently has no skills (newly initialized or class changed),
            // populate with the class's default 3-skill loadout.
            if (skills.Count == 0)
            {
                var defaults = PlayerClassData.GetDefaultSkillLoadout(cls);
                foreach (var skillId in defaults)
                {
                    var skill = CreateSkillFromId(skillId);
                    if (skill != null)
                    {
                        skills.Add(skill);
                    }
                }
            }
        }

        // ── Class system ──────────────────────────────────────────────────

        /// <summary>
        /// Applies the chosen class: updates sprites and skill restrictions.
        /// </summary>
        public void SetClass(PlayerClass cls, bool rebuildSkills = true)
        {
            PlayerClass = cls;
            GD.Print($"[Player] Class changed to {PlayerClassData.GetDisplayName(cls)}");

            ApplyClassSprites(cls);

            if (rebuildSkills)
            {
                ApplyClassSkills(cls);
            }

            OnClassChanged?.Invoke(cls);
            Update();
        }

        /// <summary>
        /// Rebuilds the combat skill list to match the allowed skills for <paramref name="cls"/>.
        /// </summary>
        private void ApplyClassSkills(PlayerClass cls)
        {
            if (_combatSystem == null)
            {
                return;
            }

            var defaultSkills = new List<Skills>();
            var defaults = PlayerClassData.GetDefaultSkillLoadout(cls);
            foreach (var skillId in defaults)
            {
                var skill = CreateSkillFromId(skillId);
                if (skill != null)
                {
                    defaultSkills.Add(skill);
                }
            }

            _combatSystem.SetSkills(defaultSkills);
            GD.Print($"[Player] Skills rebuilt for class {cls}: {string.Join(", ", defaultSkills.Select(ResolveSkillId))}");
        }

        /// <summary>
        /// Reloads animation frame textures from class-specific asset paths.
        /// Falls back to the shared (Adventurer) paths when a file cannot be loaded.
        /// </summary>
        private void ApplyClassSprites(PlayerClass cls)
        {
            if (_animationSystem == null)
            {
                return;
            }

            ClassSpritePaths paths = PlayerClassData.GetSpritePaths(cls);
            ClassSpritePaths fallback = PlayerClassData.GetSharedSpritePaths();

            // Resolve: use the class-specific path if the file actually exists, otherwise fall back.
            string Resolve(string primary, string fb) =>
                !string.IsNullOrEmpty(primary) && GD.Load<Texture>(primary) != null ? primary : fb;

            string stand1 = Resolve(paths.StandFrame1, fallback.StandFrame1);
            string stand2 = Resolve(paths.StandFrame2, fallback.StandFrame2);
            string walk1 = Resolve(paths.WalkFrame1, fallback.WalkFrame1);
            string walk2 = Resolve(paths.WalkFrame2, fallback.WalkFrame2);
            string attack1 = Resolve(paths.AttackFrame1, fallback.AttackFrame1);
            string attack2 = Resolve(paths.AttackFrame2, fallback.AttackFrame2);
            string attack3 = Resolve(paths.AttackFrame3, fallback.AttackFrame3);

            Vector2 bodySize = _map != null
                ? BodySizeInTiles * _map.TileSize
                : BodySizeInTiles * 24f;

            // Apply the per-class visual scale (does not affect the physics body).
            bodySize *= paths.ScaleMultiplier;

            // Re-initialize the animation system (reuses the existing Sprite node).
            _animationSystem.Initialize(
                this,
                stand1, stand2,
                walk1, walk2,
                attack1, attack2, attack3,
                bodySize);


            // Update per-skill-style attack frames in the animation controller so the
            // correct art plays when the player uses bow/sword/fireball skills.
            _animationController?.UpdateClassFrames(
                paths.SwordAttackPaths,
                paths.BowAttackPaths,
                paths.FireballAttackPaths);

            // Reload class-specific hit / dead textures (with shared fallback).
            string hitPath = Resolve(paths.HitFrame, fallback.HitFrame);
            string deadPath = Resolve(paths.DeadFrame, fallback.DeadFrame);
            string defPath = Resolve(paths.DefenseFrame, fallback.DefenseFrame);
            string atkPath = Resolve(paths.SkillAttackFrame, fallback.SkillAttackFrame);

            _hitTexture = GD.Load<Texture>(hitPath);
            _deadTexture = GD.Load<Texture>(deadPath);

            _animationController?.LoadDefenseTextures(defPath, atkPath);

            string bowFrame = paths.BowAttackPaths != null ? paths.BowAttackPaths[0] : "default";
            GD.Print($"[Player] Sprites updated for class {cls}. Stand={stand1}, Attack={attack1}, Bow={bowFrame}, Hit={hitPath}, Dead={deadPath}");
        }


        private void HandleRoomChangedFromPhysics(Vector2 roomIndex, string reason)
        {
            _lastKnownRoomIndex = roomIndex;
            OnRoomEntered?.Invoke(roomIndex, reason ?? "room_enter");
            GD.Print($"[ProgressSync] Entered room ({roomIndex.x}, {roomIndex.y}), reason={reason}.");
        }

        private Godot.Collections.Dictionary BuildEquippedItemsPayload()
        {
            var dict = new Godot.Collections.Dictionary();

            var slotNames = new[] { "head", "body", "arms", "legs", "shoes" };
            var slotTypes = new[] { EquipmentType.Head, EquipmentType.Body, EquipmentType.Arms, EquipmentType.Legs, EquipmentType.Shoes };

            for (int i = 0; i < slotNames.Length; i++)
            {
                Equipment eq = _equipmentSystem?.GetEquippedArmor(slotTypes[i]);
                if (eq != null)
                {
                    dict[slotNames[i]] = PlayerItemSnapshotCodec.Encode(eq);
                }
            }

            Weapon weapon = _equipmentSystem?.EquippedWeapon;
            if (weapon != null)
            {
                dict["weapon"] = PlayerItemSnapshotCodec.Encode(weapon);
            }

            return dict;
        }

        private void RestoreEquippedItems(Godot.Collections.Dictionary data)
        {
            if (data == null || _equipmentSystem == null)
            {
                return;
            }

            var slotNames = new[] { "head", "body", "arms", "legs", "shoes" };
            var slotTypes = new[] { EquipmentType.Head, EquipmentType.Body, EquipmentType.Arms, EquipmentType.Legs, EquipmentType.Shoes };

            for (int i = 0; i < slotNames.Length; i++)
            {
                if (data.Contains(slotNames[i]) && data[slotNames[i]] is Godot.Collections.Dictionary slotDict)
                {
                    Item item = PlayerItemSnapshotCodec.Decode(slotDict);
                    if (item is Equipment eq)
                    {
                        eq.EquipmentType = slotTypes[i];
                        _equipmentSystem.EquipArmor(eq);
                    }
                }
            }

            if (data.Contains("weapon") && data["weapon"] is Godot.Collections.Dictionary weaponDict)
            {
                Item item = PlayerItemSnapshotCodec.Decode(weaponDict);
                if (item is Weapon w)
                {
                    _equipmentSystem.EquipWeapon(w);
                }
            }

            UpdateAttributes();
        }

        // ==================== Helper Properties ====================
        /// <summary>
        /// Check if player is currently attacking
        /// </summary>
        public bool IsAttacking => _animationController?.IsAttacking ?? false;

        /// <summary>
        /// Expose player's facing direction (1.0f for right, -1.0f for left).
        /// </summary>
        public float FacingDirection => _animationController?.GetFacingDirection() ?? 1f;

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

        /// <summary>
        /// Replace the player's equipped skill list with the given ordered list of skill IDs,
        /// enforcing class restrictions. Called from the skill-equip UI after the player
        /// saves their loadout. Invalid / disallowed IDs are silently skipped.
        /// </summary>
        public void SetEquippedSkills(System.Collections.Generic.List<string> orderedSkillIds)
        {
            if (_combatSystem == null) return;

            var builtSkills = BuildSkillsFromIds(orderedSkillIds, PlayerClass);
            _combatSystem.SetSkills(builtSkills);
            GD.Print($"[Player] Skill loadout updated: {string.Join(", ", orderedSkillIds)}");
        }

        /// <summary>
        /// Returns the ordered list of skill IDs currently equipped by the player.
        /// </summary>
        public System.Collections.Generic.List<string> GetEquippedSkillIds()
        {
            var ids = new System.Collections.Generic.List<string>();
            var skills = _combatSystem?.CurrentSkills;
            if (skills == null) return ids;
            foreach (var s in skills)
            {
                if (s != null) ids.Add(ResolveSkillId(s));
            }
            return ids;
        }

        /// <summary>
        /// Creates a Skills instance based on the unique skill ID string.
        /// </summary>
        private static Skills CreateSkillFromId(string key)
        {
            string cleanKey = (key ?? string.Empty).ToLowerInvariant();
            if (cleanKey == "basic_attack")
            {
                return new BasicAttackSkill { EffectRenderer = new BasicAttackEffectRenderer() };
            }
            if (cleanKey == "bow_attack")
            {
                return new BowAttackSkill();
            }
            if (cleanKey == "triple_arrow")
            {
                return new TripleArrowSkill();
            }
            if (cleanKey == "ricochet_arrow")
            {
                return new RicochetArrowSkill();
            }
            if (cleanKey == "fireball")
            {
                return new FireballSkill();
            }
            if (cleanKey == "triple_fireball")
            {
                return new TripleFireballSkill();
            }
            if (cleanKey == "giant_fireball")
            {
                return new GiantFireballSkill();
            }
            if (cleanKey == "flying_sword")
            {
                return new FlyingSwordSkill();
            }
            if (cleanKey == "defense_stance")
            {
                return new DefenseStanceSkill();
            }
            if (cleanKey == "magic_slash")
            {
                return new MagicSlashSkill();
            }
            if (cleanKey == "ice_spear")
            {
                return new IceSpearSkill();
            }
            if (cleanKey == "digit_arrow")
            {
                return new DigitArrowSkill();
            }
            if (cleanKey == "super_arrow")
            {
                return new SuperArrowSkill();
            }
            if (cleanKey == "roundhouse_slash")
            {
                return new RoundhouseSlashSkill();
            }
            if (cleanKey == "knight_explose")
            {
                return new KnightExploseSkill();
            }
            return null;
        }

        /// <summary>
        /// Translates an ordered list of skill IDs into live skill instances,
        /// filtering out any IDs not allowed for the given class.
        /// </summary>
        private static System.Collections.Generic.List<Skills> BuildSkillsFromIds(
            System.Collections.Generic.List<string> orderedIds,
            PlayerClass cls)
        {
            var allowed = PlayerClassData.GetAllowedSkillIds(cls);
            var skills = new System.Collections.Generic.List<Skills>();

            if (orderedIds != null)
            {
                foreach (var id in orderedIds)
                {
                    string key = (id ?? string.Empty).ToLowerInvariant();
                    if (!allowed.Contains(key)) continue;

                    var skill = CreateSkillFromId(key);
                    if (skill != null)
                    {
                        skills.Add(skill);
                    }
                }
            }

            return skills;
        }

        // ==================== Inventory System ====================
        /// <summary>
        /// Gain experience points
        /// </summary>
        public void GainExperience(int amount)
        {
            _inventorySystem?.GainExperience(amount);
            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            if (_inventorySystem == null) return;

            while (Level < 100)
            {
                int requiredExp = 100 + 10 * (int)Level * ((int)Level - 1);
                if (_inventorySystem.Experience >= requiredExp)
                {
                    _inventorySystem.SetSnapshot(_inventorySystem.Experience - requiredExp, _inventorySystem.Gold, true);
                    SetLevel((int)Level + 1);

                    if (Attributes?.HP != null)
                    {
                        Attributes.HP.SetMaxHPAndCurrentHP(Attributes.HP.MaxHP, Attributes.HP.MaxHP);
                        OnHpChanged?.Invoke(Attributes.HP.CurrentHP, Attributes.HP.MaxHP);
                    }
                    GD.Print($"[Player] Leveled up to {Level}!");
                }
                else
                {
                    break;
                }
            }
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

        /// <summary>
        /// Equip an armor piece to its slot. Returns the previously equipped item or null.
        /// </summary>
        public Equipment EquipArmor(Equipment equipment)
        {
            Equipment old = _equipmentSystem?.EquipArmor(equipment);
            UpdateAttributes();
            return old;
        }

        /// <summary>
        /// Unequip armor from a specific slot. Returns the removed item or null.
        /// </summary>
        public Equipment UnequipArmor(EquipmentType slot)
        {
            Equipment removed = _equipmentSystem?.UnequipArmor(slot);
            UpdateAttributes();
            return removed;
        }

        /// <summary>
        /// Get the equipment in a specific armor slot.
        /// </summary>
        public Equipment GetEquippedArmor(EquipmentType slot)
        {
            return _equipmentSystem?.GetEquippedArmor(slot);
        }
    }
}