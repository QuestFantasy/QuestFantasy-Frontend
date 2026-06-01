using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Systems.StatusEffects;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// A massive fireball projectile exclusive to the Mage.
    /// Explodes on impact with a large AOE and high damage.
    /// Has a higher chance to apply Burn on each enemy hit.
    /// </summary>
    public class GiantFireballSkill : Attributes.Skills
    {
        public GiantFireballSkill()
        {
            Name = "Giant Fireball";
            Description = "Launch a massive fireball that explodes on impact.";
        }

        public override float MaxRange => 400f;

        public override float GetCooldownDuration() => 3.0f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            SkillProjectileSpawner.SpawnGiantFireball(
                player,
                target,
                MaxRange,
                onHitEffect: () => new BurnEffect(
                    GameConstants.FIREBALL_BURN_DURATION * 1.5f,
                    GameConstants.FIREBALL_BURN_DPS * 2),
                onHitChance: GameConstants.FIREBALL_BURN_CHANCE * 1.5f);
        }
    }
}