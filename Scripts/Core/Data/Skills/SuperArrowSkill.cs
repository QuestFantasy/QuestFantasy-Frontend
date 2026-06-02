using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Systems.StatusEffects;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Super arrow skill for the Archer.
    /// A powerful arrow that paralyzes enemies for 5 seconds on hit.
    /// Long cooldown skill.
    /// </summary>
    public class SuperArrowSkill : Attributes.Skills
    {
        public SuperArrowSkill()
        {
            Name = "Super Arrow";
            Description = "Fire a powerful arrow that paralyzes enemies for 5 seconds.";
        }

        public override float MaxRange => 350f;

        public override float GetCooldownDuration() => 10.0f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            // Spawn an arrow that applies stun effect on hit
            SkillProjectileSpawner.SpawnSuperArrow(
                player,
                target,
                MaxRange,
                onHitEffect: () => new StunEffect(5.0f), // 5 seconds stun (paralysis)
                onHitChance: 1.0f); // Always apply
        }
    }
}