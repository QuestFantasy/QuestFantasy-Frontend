using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Base;
using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Systems.StatusEffects;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Magic slash skill for the Mage.
    /// Strikes with arcane energy, hitting nearby enemies.
    /// </summary>
    public class MagicSlashSkill : Attributes.Skills
    {
        private const float DAMAGE_VARIANCE_MIN = 0.9f;
        private const float DAMAGE_VARIANCE_MAX = 1.1f;
        private const float DAMAGE_REDUCTION_FACTOR = 0.5f;

        public MagicSlashSkill()
        {
            Name = "Magic Slash";
            Description = "Strike with arcane energy, cutting through magic.";
        }

        public override float MaxRange => 100f;

        public override float GetCooldownDuration() => 1.0f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            // Determine aim direction (mouse position or target position)
            Vector2 targetPos = target != null ? target.GlobalPosition : player.GetGlobalMousePosition();
            Vector2 towardTarget = targetPos - player.GlobalPosition;
            Vector2 dir = towardTarget.LengthSquared() > 0.0001f ? towardTarget.Normalized() : Vector2.Right;

            // Spawn visual slash effect 36 pixels in front of the player (offset vertically slightly)
            Vector2 effectPos = player.GlobalPosition + dir * 36f;
            effectPos.y -= 8f;

            // Spawn visual slash effect, rotated to face aim direction
            SkillProjectileSpawner.SpawnTemporaryVisualEffect(
                player,
                "res://Assets/SkillAnimation/magic_slash-2.png",
                effectPos,
                0.3f,
                0.3f,
                0.35f,
                0f,
                false,
                dir.Angle()
            );

            // Get attacker stats
            int attackerAtk = player.Attributes?.EffectiveAtk ?? 1;
            int defenderDef = 0;

            // Damage to each enemy
            float variance = GD.Randf() * (DAMAGE_VARIANCE_MAX - DAMAGE_VARIANCE_MIN) + DAMAGE_VARIANCE_MIN;

            // If we have a specific target, damage them more
            if (target != null)
            {
                defenderDef = target.Attributes?.EffectiveDef ?? 0;
                int baseDamage = Mathf.Max(1, attackerAtk - Mathf.FloorToInt(defenderDef * DAMAGE_REDUCTION_FACTOR));
                int finalDamage = Mathf.RoundToInt(baseDamage * variance);

                target.TakeDamage(finalDamage);
                GD.Print($"[COMBAT] {player.EntityName} casts Magic Slash on {target.EntityName} for {finalDamage} damage!");
            }
            else
            {
                // Empty cast
                GD.Print("[MagicSlashSkill] Empty cast - no targets in range");
            }
        }
    }
}