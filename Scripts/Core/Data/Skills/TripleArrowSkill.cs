using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Advanced triple arrow projectile for the Archer.
    /// Fires 3 arrows simultaneously in the same direction, slightly offset side-by-side.
    /// </summary>
    public class TripleArrowSkill : Attributes.Skills
    {
        public TripleArrowSkill()
        {
            Name = "Triple Arrow";
            Description = "Fire 3 arrows at once that pierce enemies at range.";
        }

        public override float MaxRange => 300f;

        public override float GetCooldownDuration() => 1.5f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            SkillProjectileSpawner.SpawnTripleArrow(
                player,
                target,
                MaxRange);
        }
    }
}