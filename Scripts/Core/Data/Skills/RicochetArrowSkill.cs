using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Advanced ricochet arrow projectile for the Archer.
    /// Fires a single arrow that bounces between walls and enemies up to 4 times.
    /// </summary>
    public class RicochetArrowSkill : Attributes.Skills
    {
        public RicochetArrowSkill()
        {
            Name = "Ricochet Arrow";
            Description = "Fire an arrow that bounces between walls and enemies.";
        }

        public override float MaxRange => 300f;

        public override float GetCooldownDuration() => 2.5f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            // Spawn an arrow with 4 bounces
            SkillProjectileSpawner.SpawnRicochetArrow(
                player,
                target,
                MaxRange,
                bounces: 4);
        }
    }
}