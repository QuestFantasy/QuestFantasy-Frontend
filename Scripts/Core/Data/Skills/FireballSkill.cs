using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Systems.StatusEffects;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Basic fireball projectile for adventurer.
    /// Explodes on wall or monster hit and damages nearby enemies.
    /// Has a 25% chance to apply Burn on each enemy hit (configurable via GameConstants).
    /// </summary>
    public class FireballSkill : Attributes.Skills
    {
        public FireballSkill()
        {
            Name = "Fireball";
            Description = "Launch a fireball that explodes on impact.";
        }

        public override float MaxRange => 300f;

        public override float GetCooldownDuration() => 1.2f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            SkillProjectileSpawner.SpawnFireball(
                player,
                target,
                MaxRange,
                onHitEffect: () => new BurnEffect(
                    GameConstants.FIREBALL_BURN_DURATION,
                    GameConstants.FIREBALL_BURN_DPS),
                onHitChance: GameConstants.FIREBALL_BURN_CHANCE);
        }
    }
}