using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Systems.StatusEffects;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Ice spear skill for the Mage.
    /// Launches a spear of ice that pierces and freezes enemies.
    /// </summary>
    public class IceSpearSkill : Attributes.Skills
    {
        public IceSpearSkill()
        {
            Name = "Ice Spear";
            Description = "Launch a spear of ice that pierces and freezes enemies.";
        }

        public override float MaxRange => 280f;

        public override float GetCooldownDuration() => 1.8f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            // Spawn an ice spear projectile
            SkillProjectileSpawner.SpawnIceSpear(
                player,
                target,
                MaxRange,
                onHitEffect: () => new FreezeEffect(1.0f), // 1 second freeze
                onHitChance: 1.0f); // Always freeze
        }
    }
}