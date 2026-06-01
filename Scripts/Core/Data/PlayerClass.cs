using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QuestFantasy.Core.Data
{
    /// <summary>
    /// Available player classes in QuestFantasy.
    /// Adventurer is the default class assigned to all new and pre-existing accounts.
    /// </summary>
    public enum PlayerClass
    {
        Adventurer,
        Mage,
        Archer,
        Warrior
    }

    /// <summary>
    /// Sprite frame paths for a given class.
    /// Any path that is null/empty or whose file doesn't exist falls back to
    /// the shared Adventurer asset in <c>Player.ApplyClassSprites</c>.
    /// </summary>
    public class ClassSpritePaths
    {
        // Stand / idle frames
        public string StandFrame1 { get; set; }
        public string StandFrame2 { get; set; }

        // Walk frames
        public string WalkFrame1 { get; set; }
        public string WalkFrame2 { get; set; }

        // Attack frames (used to re-initialise the idle/walk/base-attack sprite animation)
        public string AttackFrame1 { get; set; }
        public string AttackFrame2 { get; set; }
        public string AttackFrame3 { get; set; }

        // Per-skill-style attack overrides fed to PlayerAnimationController.
        // 3-element arrays [frame1, frame2, frame3], or null to keep default.
        public string[] SwordAttackPaths { get; set; }  // override for sword-style attacks
        public string[] BowAttackPaths { get; set; }  // override for bow-style attacks
        public string[] FireballAttackPaths { get; set; }  // override for fireball-style attacks

        // Hit / damage flash frame (optional)
        public string HitFrame { get; set; }

        // Death / knocked-down frame (optional)
        public string DeadFrame { get; set; }

        /// <summary>
        /// Uniform visual scale multiplier applied on top of the body-size calculation.
        /// 1.0 = normal size, 0.8 = 20 % smaller, etc.
        /// Does NOT affect the physics body / hitbox.
        /// </summary>
        public float ScaleMultiplier { get; set; } = 1.0f;
    }

    /// <summary>
    /// Immutable descriptor for a single learnable skill.
    /// Used by the skill-equip UI to build cards without coupling to concrete skill classes.
    /// </summary>
    public class SkillDefinition
    {
        public string Id          { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Emoji       { get; set; }
        public float  CooldownSec { get; set; }
    }

    /// <summary>
    /// Static registry of per-class data:
    ///  - Which skill IDs are allowed
    ///  - Which sprite asset paths to use (with shared-asset fallbacks)
    /// </summary>
    public static class PlayerClassData
    {
        // ── Shared (Adventurer) asset paths ────────────────────────────────
        private const string SharedBase = "res://Assets/Characters/adventurer/";
        private const string SharedStand1 = SharedBase + "stand.png";
        private const string SharedStand2 = SharedBase + "stand2.png";
        private const string SharedWalk1 = SharedBase + "walk.png";
        private const string SharedWalk2 = SharedBase + "walk1.png";
        private const string SharedAttack1 = SharedBase + "slash.png";
        private const string SharedAttack2 = SharedBase + "slash1.png";
        private const string SharedAttack3 = SharedBase + "slash2.png";
        private const string SharedHit = SharedBase + "hit.png";
        private const string SharedDead = SharedBase + "down.png";

        // Adventurer bow attack frames
        private const string SharedBow1 = SharedBase + "shot_prepare.png";
        private const string SharedBow2 = SharedBase + "shot.png";
        private const string SharedBow3 = SharedBase + "shoted.png";

        // Adventurer fireball attack frames
        private const string SharedMagic1 = SharedBase + "magic.png";
        private const string SharedMagic2 = SharedBase + "magic1.png";

        // ── Skill ID constants (must match ResolveSkillId in Player.cs) ────
        public const string SkillIdSword = "basic_attack";
        public const string SkillIdBow = "bow_attack";
        public const string SkillIdFireball = "fireball";
        public const string SkillIdTripleFireball = "triple_fireball";
        public const string SkillIdGiantFireball = "giant_fireball";
        public const string SkillIdTripleArrow = "triple_arrow";
        public const string SkillIdRicochetArrow = "ricochet_arrow";

        // ── Allowed skills per class ───────────────────────────────────────

        private static readonly HashSet<string> AdventurerSkills = new HashSet<string>
        {
            SkillIdSword, SkillIdBow, SkillIdFireball
        };

        private static readonly HashSet<string> MageSkills = new HashSet<string>
        {
            SkillIdFireball, SkillIdTripleFireball, SkillIdGiantFireball
        };

        private static readonly HashSet<string> ArcherSkills = new HashSet<string>
        {
            SkillIdBow, SkillIdTripleArrow, SkillIdRicochetArrow
        };

        private static readonly HashSet<string> WarriorSkills = new HashSet<string>
        {
            SkillIdSword
        };

        /// <summary>
        /// Returns the set of skill IDs that the given class is permitted to use.
        /// </summary>
        public static HashSet<string> GetAllowedSkillIds(PlayerClass cls)
        {
            switch (cls)
            {
                case PlayerClass.Mage: return MageSkills;
                case PlayerClass.Archer: return ArcherSkills;
                case PlayerClass.Warrior: return WarriorSkills;
                default: return AdventurerSkills;
            }
        }

        /// <summary>Returns the display name shown in the class selection UI.</summary>
        public static string GetDisplayName(PlayerClass cls)
        {
            switch (cls)
            {
                case PlayerClass.Mage: return "Mage";
                case PlayerClass.Archer: return "Archer";
                case PlayerClass.Warrior: return "Warrior";
                default: return "Adventurer";
            }
        }

        /// <summary>Returns a one-line flavour description shown in the class selection UI.</summary>
        public static string GetDescription(PlayerClass cls)
        {
            switch (cls)
            {
                case PlayerClass.Mage:
                    return "Wields the arcane arts.\nMasters the art of Fireball magic.";
                case PlayerClass.Archer:
                    return "Swift and precise.\nStrikes enemies from a distance with arrows.";
                case PlayerClass.Warrior:
                    return "Unyielding and fierce.\nCleaves foes with a powerful sword slash.";
                default:
                    return "The all-rounder.\nCommands sword, bow, and magic freely.";
            }
        }

        /// <summary>Returns the skill names shown in the class-select UI.</summary>
        public static string GetSkillListText(PlayerClass cls)
        {
            switch (cls)
            {
                case PlayerClass.Mage: return "Fireball";
                case PlayerClass.Archer: return "Arrow Shot";
                case PlayerClass.Warrior: return "Sword Slash";
                default: return "Sword Slash, Arrow Shot, Fireball";
            }
        }

        // ── Skill definition catalogue ─────────────────────────────────────

        private static readonly SkillDefinition DefSword = new SkillDefinition
        {
            Id = SkillIdSword,
            DisplayName = "Sword Slash",
            Description = "A powerful melee strike that hits nearby enemies.",
            Emoji = "⚔️",
            CooldownSec = 0.3f,
        };

        private static readonly SkillDefinition DefBow = new SkillDefinition
        {
            Id = SkillIdBow,
            DisplayName = "Arrow Shot",
            Description = "Fire an arrow that pierces enemies at range.",
            Emoji = "🏹",
            CooldownSec = 0.8f,
        };

        private static readonly SkillDefinition DefTripleArrow = new SkillDefinition
        {
            Id = SkillIdTripleArrow,
            DisplayName = "Triple Arrow",
            Description = "Fire 3 parallel arrows at once that pierce enemies.",
            Emoji = "🏹",
            CooldownSec = 1.5f,
        };

        private static readonly SkillDefinition DefRicochetArrow = new SkillDefinition
        {
            Id = SkillIdRicochetArrow,
            DisplayName = "Ricochet Arrow",
            Description = "Fire an arrow that bounces between enemies and walls.",
            Emoji = "↪️",
            CooldownSec = 2.5f,
        };

        private static readonly SkillDefinition DefFireball = new SkillDefinition
        {
            Id = SkillIdFireball,
            DisplayName = "Fireball",
            Description = "Launch a fireball that explodes on impact and may Burn.",
            Emoji = "🔥",
            CooldownSec = 1.2f,
        };

        private static readonly SkillDefinition DefTripleFireball = new SkillDefinition
        {
            Id = SkillIdTripleFireball,
            DisplayName = "Triple Fireball",
            Description = "Launch 3 fireballs in a spread that explode on impact.",
            Emoji = "☄️",
            CooldownSec = 2.0f,
        };

        private static readonly SkillDefinition DefGiantFireball = new SkillDefinition
        {
            Id = SkillIdGiantFireball,
            DisplayName = "Giant Fireball",
            Description = "Launch a massive fireball that explodes on impact.",
            Emoji = "🔥",
            CooldownSec = 3.0f,
        };

        /// <summary>
        /// Returns the ordered list of all <see cref="SkillDefinition"/>s that
        /// the given class is permitted to equip. Used to build the skill-equip UI.
        /// </summary>
        public static ReadOnlyCollection<SkillDefinition> GetAllSkillDefinitions(PlayerClass cls)
        {
            switch (cls)
            {
                case PlayerClass.Mage:
                    return new ReadOnlyCollection<SkillDefinition>(new[] { DefFireball, DefTripleFireball, DefGiantFireball });
                case PlayerClass.Archer:
                    return new ReadOnlyCollection<SkillDefinition>(new[] { DefBow, DefTripleArrow, DefRicochetArrow });
                case PlayerClass.Warrior:
                    return new ReadOnlyCollection<SkillDefinition>(new[] { DefSword });
                default: // Adventurer
                    return new ReadOnlyCollection<SkillDefinition>(new[] { DefSword, DefBow, DefFireball });
            }
        }

        /// <summary>
        /// Returns the default ordered skill-ID loadout for <paramref name="cls"/>.
        /// Used to reset the loadout when the player changes class.
        /// </summary>
        public static ReadOnlyCollection<string> GetDefaultSkillLoadout(PlayerClass cls)
        {
            switch (cls)
            {
                case PlayerClass.Mage:    return new ReadOnlyCollection<string>(new[] { SkillIdFireball, SkillIdTripleFireball, SkillIdGiantFireball });
                case PlayerClass.Archer:  return new ReadOnlyCollection<string>(new[] { SkillIdBow, SkillIdTripleArrow, SkillIdRicochetArrow });
                case PlayerClass.Warrior: return new ReadOnlyCollection<string>(new[] { SkillIdSword });
                default:                  return new ReadOnlyCollection<string>(new[] { SkillIdSword, SkillIdBow, SkillIdFireball });
            }
        }

        /// <summary>
        /// Returns the explicit sprite paths for each class.
        /// Paths that don't exist on disk are handled gracefully in
        /// <c>Player.ApplyClassSprites</c> (falls back to shared assets).
        /// </summary>
        public static ClassSpritePaths GetSpritePaths(PlayerClass cls)
        {
            switch (cls)
            {
                case PlayerClass.Archer:
                    {
                        const string A = "res://Assets/Characters/archer/";
                        return new ClassSpritePaths
                        {
                            StandFrame1 = A + "stand.png",
                            StandFrame2 = A + "stand.png",     // no stand2 — reuse stand
                            WalkFrame1 = A + "walk.png",
                            WalkFrame2 = A + "walk1.png",
                            AttackFrame1 = A + "bow.png",
                            AttackFrame2 = A + "bow1.png",
                            AttackFrame3 = A + "bow1.png",      // hold last bow-draw frame
                            BowAttackPaths = new[] { A + "bow.png", A + "bow1.png", A + "bow1.png" },
                            SwordAttackPaths = null,          // archer can't sword; won't be used
                            FireballAttackPaths = null,
                            HitFrame = A + "hit.png",
                            DeadFrame = A + "down.png",
                            ScaleMultiplier = 0.8f,             // archer sprites are 20 % smaller
                        };
                    }

                case PlayerClass.Mage:
                    {
                        const string M = "res://Assets/Characters/mage/";
                        return new ClassSpritePaths
                        {
                            StandFrame1 = M + "stand.png",
                            StandFrame2 = M + "stand.png",     // reuse stand (no stand2)
                            WalkFrame1 = M + "walk.png",
                            WalkFrame2 = M + "walk1.png",
                            // Use mage attack frames for the base attack animation slot
                            AttackFrame1 = M + "attack.png",
                            AttackFrame2 = M + "attack-1.png",
                            AttackFrame3 = M + "attack-1.png",  // hold last frame
                                                                // Fireball uses the mage attack art
                            FireballAttackPaths = new[] { M + "attack.png", M + "attack-1.png", M + "attack-1.png" },
                            SwordAttackPaths = null,
                            BowAttackPaths = null,
                            HitFrame = M + "hit.png",
                            DeadFrame = M + "down.png",
                        };
                    }

                case PlayerClass.Warrior:
                    {
                        const string W = "res://Assets/Characters/warrior/";
                        return new ClassSpritePaths
                        {
                            StandFrame1 = W + "stand.png",
                            StandFrame2 = W + "stand.png",     // reuse stand (no stand2)
                            WalkFrame1 = W + "walk.png",
                            WalkFrame2 = W + "walk1.png",
                            // Only one slash frame exists — reuse it across all 3 attack slots
                            AttackFrame1 = W + "slash1.png",
                            AttackFrame2 = W + "slash1.png",
                            AttackFrame3 = W + "slash1.png",
                            SwordAttackPaths = new[] { W + "slash1.png", W + "slash1.png", W + "slash1.png" },
                            BowAttackPaths = null,
                            FireballAttackPaths = null,
                            HitFrame = W + "hit.png",
                            DeadFrame = W + "skill_down.png",   // knocked-down sprite
                        };
                    }

                default:   // Adventurer
                    return GetSharedSpritePaths();
            }
        }

        /// <summary>
        /// Returns the shared (Adventurer) sprite paths that are always guaranteed to exist.
        /// </summary>
        public static ClassSpritePaths GetSharedSpritePaths()
        {
            return new ClassSpritePaths
            {
                StandFrame1 = SharedStand1,
                StandFrame2 = SharedStand2,
                WalkFrame1 = SharedWalk1,
                WalkFrame2 = SharedWalk2,
                AttackFrame1 = SharedAttack1,
                AttackFrame2 = SharedAttack2,
                AttackFrame3 = SharedAttack3,
                HitFrame = SharedHit,
                DeadFrame = SharedDead,
                // Adventurer bow and fireball frames (used as defaults for all classes)
                BowAttackPaths = new[] { SharedBow1, SharedBow2, SharedBow3 },
                FireballAttackPaths = new[] { SharedMagic1, SharedMagic2, SharedMagic2 },
                SwordAttackPaths = new[] { SharedAttack1, SharedAttack2, SharedAttack3 },
            };
        }

        /// <summary>Serialises PlayerClass to the snake_case string stored in the backend.</summary>
        public static string Serialize(PlayerClass cls)
        {
            switch (cls)
            {
                case PlayerClass.Mage: return "mage";
                case PlayerClass.Archer: return "archer";
                case PlayerClass.Warrior: return "warrior";
                default: return "adventurer";
            }
        }

        /// <summary>Deserialises a backend string to PlayerClass. Defaults to Adventurer.</summary>
        public static PlayerClass Deserialize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return PlayerClass.Adventurer;

            switch (value.Trim().ToLowerInvariant())
            {
                case "mage": return PlayerClass.Mage;
                case "archer": return PlayerClass.Archer;
                case "warrior": return PlayerClass.Warrior;
                default: return PlayerClass.Adventurer;
            }
        }
    }
}