using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Systems.StatusEffects;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Advanced triple fireball projectile for the Mage.
    /// Fires 3 fireballs simultaneously in a spread pattern.
    /// Explodes on wall or monster hit and damages nearby enemies.
    /// Has a 25% chance to apply Burn on each enemy hit (configurable via GameConstants).
    /// </summary>
    public class TripleFireballSkill : Attributes.Skills
    {
        public TripleFireballSkill()
        {
            Name = "Triple Fireball";
            Description = "Launch 3 fireballs in a spread that explode on impact.";
        }

        public override float MaxRange => 300f;

        public override float GetCooldownDuration() => 2.0f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            SkillProjectileSpawner.SpawnTripleFireball(
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